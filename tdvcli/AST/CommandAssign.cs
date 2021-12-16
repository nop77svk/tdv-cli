namespace NoP77svk.TibcoDV.CLI.AST
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using log4net;
    using NoP77svk.TibcoDV.API;
    using NoP77svk.TibcoDV.API.WSDL.Admin;
    using NoP77svk.TibcoDV.CLI.Commons;
    using NoP77svk.TibcoDV.Commons;
    using WSDL = NoP77svk.TibcoDV.API.WSDL.Admin;

    internal class CommandAssign : IAsyncStatement
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Program));

        internal WSDL.rbsAssignmentOperationType Action { get; }
        internal FilterPolicy What { get; }
        internal IList<ResourceSpecifier> Resources { get; }

        internal CommandAssign(rbsAssignmentOperationType action, FilterPolicy what, IList<ResourceSpecifier> resources)
        {
            Action = action;
            What = what;
            Resources = resources;
        }

        public async Task Execute(TdvWebServiceClient tdvClient, IInfoOutput output)
        {
            using var log = new TraceLog(_log, nameof(Execute));

            if (What is null)
                throw new ArgumentNullException(nameof(What));

            if (Resources is null)
                throw new ArgumentNullException(nameof(Resources));

            if (What is AST.RbsFilterPolicy rbsPolicy)
                await ExecuteAssignRbsPolicy(tdvClient, Action, rbsPolicy.PolicyPath, Resources, output);
            else
                throw new NotImplementedException($"Don't know how to assign/unassign {What.GetType()}");
        }

        private static async Task ExecuteAssignRbsPolicy(TdvWebServiceClient tdvClient, WSDL.rbsAssignmentOperationType action, string policyFunction, IEnumerable<ResourceSpecifier> resources, IInfoOutput output)
        {
            using var log = new TraceLog(_log, nameof(ExecuteAssignRbsPolicy));

            (string actionDescPast, string actionDescPresentInitCaps, string actionDirectionDesc) = action switch
            {
                WSDL.rbsAssignmentOperationType.ASSIGN => ("restricted", "Assigning", "to"),
                WSDL.rbsAssignmentOperationType.REMOVE => ("unrestricted", "Removing", "from"),
                _ => throw new ArgumentOutOfRangeException(nameof(action), action.ToString())
            };

            if (string.IsNullOrEmpty(policyFunction))
                throw new ArgumentNullException(nameof(policyFunction));

            if (resources is null)
                throw new ArgumentNullException(nameof(resources));

            output.InfoNoEoln($"{actionDescPresentInitCaps} RLS policy {policyFunction} {actionDirectionDesc} {string.Join(',', resources.Select(x => x.Path))}...");

            int problemResources = resources
                .Where(res => res.Type is not WSDL.resourceType.TABLE and not WSDL.resourceType.CONTAINER
                    || res.Path is null)
                .Count();

            if (problemResources > 0)
                throw new ArgumentOutOfRangeException(nameof(resources), problemResources, "Some non-table, non-container resources supplied");

            Task<WSDL.rbsGetFilterPolicyResponse> policyInfoTask = tdvClient.GetRbsPolicyInfo(policyFunction).FirstAsync().AsTask();

            List<string> inputTables = resources
                .Where(res => res.Type == WSDL.resourceType.TABLE)
                .Where(res => !string.IsNullOrWhiteSpace(res.Path))
                .Select(res => res.Path ?? string.Empty)
                .Distinct()
                .ToList();

            if (_log.IsDebugEnabled)
                _log.Debug($"#inputTables = {inputTables.Count}");

            List<ValueTuple<string?, TdvResourceTypeEnumAgr>> inputContainers = resources
                .Where(res => res.Type == WSDL.resourceType.CONTAINER)
                .Where(res => !string.IsNullOrWhiteSpace(res.Path))
                .Select(res => res.Path ?? string.Empty)
                .Distinct()
                .Select(container => new ValueTuple<string?, TdvResourceTypeEnumAgr>(container, TdvResourceTypeEnumAgr.Folder))
                .ToList();

            if (_log.IsDebugEnabled)
                _log.Debug($"#inputContainers = {inputContainers.Count}");

            List<string> containedTables = await tdvClient.RetrieveContainerContentsRecursive(inputContainers)
                .Where(folderItem => folderItem.Type == TdvResourceTypeConst.Table)
                .Where(folderItem => !string.IsNullOrEmpty(folderItem.Path))
                .Select(folderItem => folderItem.Path ?? string.Empty)
                .ToListAsync();

            IEnumerable<string> allTablesFound = inputTables
                .Concat(containedTables);

            WSDL.rbsGetFilterPolicyResponse policyInfo = await policyInfoTask;
            int countAssignmentsBefore = policyInfo.policy.assignmentList.Length;

            if (action == rbsAssignmentOperationType.ASSIGN)
            {
                policyInfo.policy.assignmentList = policyInfo.policy.assignmentList
                    .Union(allTablesFound)
                    .ToArray();
            }
            else
            {
                policyInfo.policy.assignmentList = policyInfo.policy.assignmentList
                    .Except(allTablesFound)
                    .ToArray();
            }

            int countAssignmentsAfter = policyInfo.policy.assignmentList.Length;

            WSDL.rbsWriteFilterPolicyResponse result = await tdvClient.WriteRbsPolicy(new WSDL.rbsWriteFilterPolicyRequest()
            {
                policy = policyInfo.policy,
                originalPath = policyFunction
            });

            output.Info($" {Math.Abs(countAssignmentsAfter - countAssignmentsBefore)} ({countAssignmentsBefore}->{countAssignmentsAfter}) tables/views successfully {actionDescPast}");
        }
    }
}
