namespace NoP77svk.TibcoDV.API
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using NoP77svk.Linq;

    public partial class TdvWebServiceClient
    {
        public async Task AssignUnassignRbsPolicy(WSDL.Admin.rbsAssignmentOperationType action, string? policyFunctionPath, string? tablePath)
        {
            if (string.IsNullOrEmpty(policyFunctionPath))
                throw new ArgumentNullException(nameof(policyFunctionPath));

            if (string.IsNullOrWhiteSpace(tablePath))
                throw new ArgumentNullException(nameof(tablePath));

            await AssignUnassignRbsPolicy(action, policyFunctionPath, new string[] { tablePath });
        }

        public async Task AssignUnassignRbsPolicy(WSDL.Admin.rbsAssignmentOperationType action, string? policyFunctionPath, IEnumerable<string>? tables)
        {
            if (string.IsNullOrEmpty(policyFunctionPath))
                throw new ArgumentNullException(nameof(policyFunctionPath));

            if (tables is null)
                throw new ArgumentNullException(nameof(tables));

            IEnumerable<Task> assignUnassignTasks = tables
                .Where(tablePath => tablePath != null)
                .Select(tablePath => Task.Factory.StartNew(async () =>
                {
                    using Stream x = await _wsClient.EndpointGetStream(
                        new TdvSoapWsEndpoint<WSDL.Admin.rbsAssignFilterPolicyRequest>(
                            "rbsAssignFilterPolicy",
                            new WSDL.Admin.rbsAssignFilterPolicyRequest()
                            {
                                name = policyFunctionPath,
                                operation = action,
                                operationSpecified = true,
                                target = tablePath
                            }
                        )
                    );
                }));

            await Task.WhenAll(assignUnassignTasks);
        }
    }
}
