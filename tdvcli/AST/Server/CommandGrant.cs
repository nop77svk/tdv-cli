namespace NoP77svk.TibcoDV.CLI.AST.Server
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using log4net;
    using NoP77svk.Linq;
    using NoP77svk.Text.RegularExpressions;
    using NoP77svk.TibcoDV.API;
    using NoP77svk.TibcoDV.API.WSDL.Admin;
    using NoP77svk.TibcoDV.CLI.AST.Infra;
    using NoP77svk.TibcoDV.CLI.Commons;
    using NoP77svk.TibcoDV.Commons;
    using WSDL = NoP77svk.TibcoDV.API.WSDL.Admin;

    internal class CommandGrant : IAsyncExecutable
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Program));

        internal bool IsRecursive { get; }
        internal updatePrivilegesMode ModusOperandi { get; }
        internal IList<TdvPrivilegeEnum> Privileges { get; }
        internal IList<ResourceSpecifier> Resources { get; }
        internal IList<Principal> Principals { get; }
        internal GrantPropagationDirections Propagate { get; }

        internal CommandGrant(bool isRecursive, updatePrivilegesMode modusOperandi, IList<TdvPrivilegeEnum> privileges, IList<ResourceSpecifier> resources, IList<Principal> principals, GrantPropagationDirections propagate)
        {
            IsRecursive = isRecursive;
            ModusOperandi = modusOperandi;
            Privileges = privileges;
            Resources = resources;
            Principals = principals;
            Propagate = propagate;
        }

        public async Task Execute(TdvWebServiceClient tdvClient, IInfoOutput output, ParserState parserState)
        {
            using var log = new TraceLog(_log, nameof(Execute));

            string privilegesConcatenated = string.Join(
                ' ',
                Privileges.Select(x => x.ToString().ToUpper())
            );

            // retrieve domain->group, domain->user mappings from server, if needed
            Dictionary<string, List<string>> allDomainGroups, allDomainUsers;
            (allDomainGroups, allDomainUsers) = await RetrieveDomainGroupsAndUsers(tdvClient);

            // calculate the final set of grantees
            IEnumerable<privilege> granteesMatchedByEquality = Principals
                .Where(principal => principal.MatchingPrincipal is MatchExactly)
                .Select(principal => new privilege()
                {
                    domain = principal.Domain,
                    name = principal.MatchingPrincipal.Value,
                    nameType = principal.Type,
                    privs = privilegesConcatenated
                });

            privilege[] allGrantees = granteesMatchedByEquality
                .Concat(GranteeGroupsMatchedByRegexp(privilegesConcatenated, allDomainGroups))
                .Concat(GranteeUsersMatchedByRegexp(privilegesConcatenated, allDomainUsers))
                .ToArray();

            List<privilegeEntry> privilegeEntries = Resources
                .Select(obj => new privilegeEntry()
                {
                    type = Enum.Parse<resourceOrColumnType>(obj.Type.ToString(), false),
                    path = obj.Path,
                    privileges = allGrantees
                })
                .ToList();

            await tdvClient.UpdateResourcePrivileges(privilegeEntries, IsRecursive, ModusOperandi, propagateToProducers: Propagate.ToProducers, propagateToConsumers: Propagate.ToConsumers);

            string msgModus = ModusOperandi switch
            {
                updatePrivilegesMode.SET_EXACTLY => "Set",
                updatePrivilegesMode.OVERWRITE_APPEND => "Appended",
                _ => "Granted"
            };
            string msgRecursive = IsRecursive ? " recursively" : string.Empty;
            output.Info($"{msgModus} {Privileges.Count} privileges{msgRecursive} on {Resources.Count} resources to {allGrantees.Length} principals");
        }

        private IEnumerable<privilege> GranteeUsersMatchedByRegexp(string privilegesConcatenated, Dictionary<string, List<string>> allDomainUsers)
        {
            return allDomainUsers
                .Unnest(
                    domainUsers => domainUsers.Value,
                    (domainUsers, userName) => new { ServerDomain = domainUsers.Key, ServerUser = userName }
                )
                .CrossProduct(Principals
                    .Where(principal => principal.Type == userNameType.USER)
                    .Where(principal => principal.MatchingPrincipal is MatchByRegExp)
                    .Select(principal => new { GranteePrincipal = principal, GranteeWildcard = RegexExt.ParseSlashedRegexp(principal.MatchingPrincipal.Value, RegexOptions.IgnoreCase) })
                )
                .Where(crossRecord => crossRecord.Item1.ServerDomain.Equals(crossRecord.Item2.GranteePrincipal.Domain)
                    && crossRecord.Item2.GranteeWildcard.IsMatch(crossRecord.Item1.ServerUser)
                )
                .Select(match => new privilege()
                {
                    domain = match.Item1.ServerDomain,
                    name = match.Item1.ServerUser,
                    nameType = userNameType.USER,
                    privs = privilegesConcatenated
                });
        }

        private IEnumerable<privilege> GranteeGroupsMatchedByRegexp(string privilegesConcatenated, Dictionary<string, List<string>> allDomainGroups)
        {
            return allDomainGroups
                .Unnest(
                    domainGroups => domainGroups.Value,
                    (domainGroups, groupName) => new { ServerDomain = domainGroups.Key, ServerGroup = groupName }
                )
                .CrossProduct(Principals
                    .Where(principal => principal.Type == userNameType.GROUP)
                    .Where(principal => principal.MatchingPrincipal is MatchByRegExp)
                    .Select(principal => new { GranteePrincipal = principal, GranteeWildcard = RegexExt.ParseSlashedRegexp(principal.MatchingPrincipal.Value, RegexOptions.IgnoreCase) })
                )
                .Where(crossRecord => crossRecord.Item1.ServerDomain.Equals(crossRecord.Item2.GranteePrincipal.Domain)
                    && crossRecord.Item2.GranteeWildcard.IsMatch(crossRecord.Item1.ServerGroup)
                )
                .Select(match => new privilege()
                {
                    domain = match.Item1.ServerDomain,
                    name = match.Item1.ServerGroup,
                    nameType = userNameType.GROUP,
                    privs = privilegesConcatenated
                });
        }

        private async Task<ValueTuple<Dictionary<string, List<string>>, Dictionary<string, List<string>>>> RetrieveDomainGroupsAndUsers(TdvWebServiceClient tdvClient)
        {
            IEnumerable<Principal> domainsForWildcardMatching = Principals
                .Where(principal => principal.MatchingPrincipal is MatchExactly)
                .Where(principal => !string.IsNullOrEmpty(principal.Domain))
                .ToList();

            Dictionary<string, List<string>> allDomainGroups = new ();
            Dictionary<string, List<string>> allDomainUsers = new ();

            IEnumerable<Tuple<string, userNameType, Task<List<string>>>> domainGroupRetrievalTasks = domainsForWildcardMatching
                .Where(principal => principal.Type == userNameType.GROUP)
                .Select(principal => principal.Domain ?? string.Empty)
                .Distinct()
                .Select(domain => new Tuple<string, userNameType, IAsyncEnumerable<string>>(domain, userNameType.GROUP, tdvClient.GetDomainGroups(domain)))
                .Select(task => new Tuple<string, userNameType, Task<List<string>>>(task.Item1, task.Item2, task.Item3.ToListAsync().AsTask()));

            IEnumerable<Tuple<string, userNameType, Task<List<string>>>> userGroupRetrievalTasks = domainsForWildcardMatching
                .Where(principal => principal.Type == userNameType.USER)
                .Select(principal => principal.Domain ?? string.Empty)
                .Distinct()
                .Select(domain => new Tuple<string, userNameType, IAsyncEnumerable<string>>(domain, userNameType.USER, tdvClient.GetDomainUsers(domain)))
                .Select(task => new Tuple<string, userNameType, Task<List<string>>>(task.Item1, task.Item2, task.Item3.ToListAsync().AsTask()));

            await Task.WhenAll(domainGroupRetrievalTasks
                .Concat(userGroupRetrievalTasks)
                .Select(task => task.Item3)
            );

            foreach (Tuple<string, userNameType, Task<List<string>>> task in domainGroupRetrievalTasks)
            {
                if (_log.IsDebugEnabled)
                    _log.Debug($"retrieved domain {task.Item1} groups: [\n" + string.Join(",\n\t", task.Item3.Result) + "\n]");

                allDomainGroups.Add(task.Item1, task.Item3.Result);
            }

            foreach (Tuple<string, userNameType, Task<List<string>>> task in userGroupRetrievalTasks)
            {
                if (_log.IsDebugEnabled)
                    _log.Debug($"retrieved domain {task.Item1} users: [\n" + string.Join(",\n\t", task.Item3.Result) + "\n]");

                allDomainUsers.Add(task.Item1, task.Item3.Result);
            }

            return new (allDomainGroups, allDomainUsers);
        }
    }
}
