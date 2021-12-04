namespace NoP77svk.TibcoDV.CLI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using CommandLine;
    using log4net;
#if DEBUG
    using Microsoft.Extensions.Configuration;
#endif
    using NoP77svk.IO;
    using NoP77svk.Linq;
    using NoP77svk.Text.RegularExpressions;
    using NoP77svk.TibcoDV.API;
    using NoP77svk.TibcoDV.CLI.AST;
    using NoP77svk.TibcoDV.Commons;
    using NoP77svk.Web.WS;
    using Pegasus.Common;
    using WSDL = NoP77svk.TibcoDV.API.WSDL;

    internal class Program
        : BaseProgram
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Program));

        internal static async Task<int> Main(string[] args)
        {
            InitLogging();
            using var traceLog = new TraceLog(_log, nameof(Main));

            if (_log.IsDebugEnabled)
                _log.Debug(string.Join(Environment.NewLine, args.Prepend($"{nameof(args)}:")));

            int returnCode = 127;

            try
            {
                await Parser
                    .Default
                    .ParseArguments<CommandLineOptions>(args)
                    .WithParsedAsync(async argsParsed => await MainWithParsedOptions(argsParsed));
                returnCode = 0;
            }
            catch (Exception e)
            {
                _log.Fatal("Generator failed", e);
#if DEBUG
                throw;
#else
                returnCode = 126;
#endif
            }

            _log.Debug($"{nameof(returnCode)} = {returnCode}");
            return returnCode;
        }

        internal static async Task MainWithParsedOptions(CommandLineOptions args)
        {
            using var traceLog = new TraceLog(_log, nameof(MainWithParsedOptions));

            _log.Debug($"{nameof(TdvWebServiceClient)}.{nameof(TdvWebServiceClient.FolderDelimiter)} = {TdvWebServiceClient.FolderDelimiter}");
            PathExt.FolderDelimiter = TdvWebServiceClient.FolderDelimiter;

#if DEBUG
            _log.Debug("Loading user secrets");
            IConfiguration config = new ConfigurationBuilder()
                .AddUserSecrets<Program>()
                .Build();

            _log.Debug("Validating config");
            args.ValidateAndCleanUp(config.AsEnumerable());
#else
            _log.Debug("Validating config");
            args.ValidateAndCleanUp(null);
#endif

            _log.Info($"Connecting as {args.TdvServerUserName} to {args.TdvServerWsScheme}://{args.TdvServerHost}:{args.TdvServerWsApiPort}");
            using HttpClient httpClient = InitHttpConnectionPool(args);

            TdvWebServiceClient tdvClient = InitTdvRestClient(
                httpClient,
                args,
                1,
                obj =>
                {
                    // 2do! do this in a more elegant way somehow!
                    _log.Debug("Injecting credentials into HTTP request");
                    obj.Headers.Authorization = HttpWebServiceClient.GetHeaderForBasicAuthentication(args.TdvServerUserName, args.TdvServerUserPassword);

                    DebugLogHttpRequest(obj);
                },
                DebugLogHttpResponse
            );

            ScriptFileParser fileParser = new ScriptFileParser();
            PierresTibcoSqlParser sqlParser = new PierresTibcoSqlParser();

            // do your stuff
            foreach (ScriptFileParserOutPOCO statement in fileParser.SplitScriptsToStatements(args.PrivilegeDefinitionFiles))
            {
                _log.Debug(statement);
                object commandAST;

                try
                {
                    commandAST = sqlParser.Parse(statement.Statement);
                }
                catch (FormatException e)
                {
                    Cursor? ec = null;
                    if (e.Data["cursor"] is Cursor)
                        ec = (Cursor?)e.Data["cursor"];

                    if (ec is not null)
                        _log.Error($"File {statement.FileName}, line {statement.FileLine}, statement line {ec.Line}, column {ec.Column}\":\n{statement.Statement}", e);
                    else
                        _log.Error($"File {statement.FileName}, line {statement.FileLine}, failed to parse:\n{statement}", e);

                    throw new StatementParseException(statement.FileName, statement.FileLine, statement.Statement, e.Message, e);
                }

                await ExecuteParsedStatement(tdvClient, commandAST);
            }

            _log.Info("All done");
        }

        private static async Task ExecuteParsedStatement(TdvWebServiceClient tdvClient, object commandAST)
        {
            using var log = new TraceLog(_log, nameof(ExecuteParsedStatement));
            _log.Debug(commandAST);

            if (commandAST is AST.Assign stmtAssign)
                await ExecuteAssign(tdvClient, stmtAssign);
            else if (commandAST is AST.CreateResource stmtCreateResource)
                await ExecuteCreateResource(tdvClient, stmtCreateResource);
            else if (commandAST is AST.Describe stmtDescribe)
                await ExecuteDescribe(tdvClient, stmtDescribe);
            else if (commandAST is AST.DropResource stmtDropResource)
                await ExecuteDropResource(tdvClient, stmtDropResource);
            else if (commandAST is AST.Grant stmtGrant)
                await ExecuteGrant(tdvClient, stmtGrant);
            else if (commandAST is AST.ClientPrompt stmtClientPrompt)
                ExecuteClientPrompt(stmtClientPrompt);
            else
                throw new ArgumentOutOfRangeException(nameof(commandAST), commandAST?.GetType() + " :: " + commandAST?.ToString(), "Unrecognized type of parsed statement");
        }

        private static async Task ExecuteAssign(TdvWebServiceClient tdvClient, AST.Assign stmt)
        {
            using var log = new TraceLog(_log, nameof(ExecuteAssign));

            if (stmt.What is null)
                throw new ArgumentNullException(nameof(stmt) + "." + nameof(stmt.What));

            if (stmt.What.Resources is null || !stmt.What.Resources.Any())
                throw new ArgumentNullException(nameof(stmt) + "." + nameof(stmt.What) + "." + nameof(stmt.What.Resources));

            if (stmt.What is AST.AssignRbsPolicy whatRbsPolicy)
                await ExecuteAssignRbsPolicy(tdvClient, stmt.Action, whatRbsPolicy);
            else
                throw new NotImplementedException($"Don't know how to assign/unassign {stmt.What.GetType()}");
        }

        private static async Task ExecuteAssignRbsPolicy(TdvWebServiceClient tdvClient, WSDL.Admin.rbsAssignmentOperationType action, AST.AssignRbsPolicy what)
        {
            using var log = new TraceLog(_log, nameof(ExecuteAssignRbsPolicy));

            if (what.Policy is null)
                throw new ArgumentNullException(nameof(what) + "." + nameof(what.Policy));

            if (string.IsNullOrEmpty(what.Policy))
                throw new ArgumentNullException(nameof(what) + "." + nameof(what.Policy));

            if (what.Resources is null)
                throw new ArgumentNullException(nameof(what) + "." + nameof(what.Resources));

            if (!what.Resources.Any())
                throw new ArgumentException("Empty list of resources", nameof(what) + "." + nameof(what.Resources));

            int problemResources = what.Resources
                .Where(res => res.Type is not WSDL.Admin.resourceType.TABLE and not WSDL.Admin.resourceType.CONTAINER
                    || res.Path is null)
                .Count();

            if (problemResources > 0)
                throw new ArgumentOutOfRangeException(nameof(what) + "." + nameof(what.Resources), problemResources, "Some non-table, non-container resources supplied");

            IEnumerable<string> inputTables = what.Resources
                .Where(res => res.Type == WSDL.Admin.resourceType.TABLE)
                .Select(res => res.Path ?? string.Empty)
                .Distinct();

            if (_log.IsDebugEnabled)
                _log.Debug($"#inputTables = {inputTables.Count()}");

            Task<int> inputTablesRbsPolicyTask = tdvClient.AssignUnassignRbsPolicy(action, what.Policy, inputTables);

            List<ValueTuple<string?, TdvResourceTypeEnumAgr>> inputContainers = what.Resources
                .Where(res => res.Type == WSDL.Admin.resourceType.CONTAINER)
                .Select(res => res.Path ?? string.Empty)
                .Distinct()
                .Select(container => new ValueTuple<string?, TdvResourceTypeEnumAgr>(container, TdvResourceTypeEnumAgr.Folder))
                .ToList();

            if (_log.IsDebugEnabled)
                _log.Debug($"#inputContainers = {inputContainers.Count()}");

            IEnumerable<string> containedTables = tdvClient.RetrieveContainerContentsRecursive(inputContainers)
                .Where(folderItem => folderItem.Type == TdvResourceTypeConst.Table)
                .Where(folderItem => !string.IsNullOrEmpty(folderItem.Path))
                .Select(folderItem => folderItem.Path ?? string.Empty)
                .ToEnumerable();

            Task<int> containedTablesRbsPolicyTask = tdvClient.AssignUnassignRbsPolicy(
                action,
                what.Policy,
                containedTables,
                10,
                chunksThusFar => { _log.Info($"RLS policy {action.ToString().ToLower()}ed to {chunksThusFar} views/tables thus far"); }
            );

            await Task.WhenAll(inputTablesRbsPolicyTask, containedTablesRbsPolicyTask);
            int result = inputTablesRbsPolicyTask.Result + containedTablesRbsPolicyTask.Result;

            _log.Info($"Total of {result} tables restricted with RBS policy {what.Policy}");
        }

        private static void ExecuteClientPrompt(AST.ClientPrompt stmtClientPrompt)
        {
            using var log = new TraceLog(_log, nameof(ExecuteClientPrompt));

            if (stmtClientPrompt.PromptText is not null)
                _log.Info(stmtClientPrompt.PromptText);
        }

        private static async Task ExecuteCreateResource(TdvWebServiceClient tdvClient, CreateResource stmt)
        {
            using var log = new TraceLog(_log, nameof(ExecuteCreateResource));
            _log.Debug(stmt);

            if (stmt.ResourceDDL is null)
                throw new ArgumentNullException(nameof(stmt) + "." + nameof(stmt.ResourceDDL));

            if (stmt.ResourceDDL is AST.FolderDDL folderDDL)
                await ExecuteCreateFolder(tdvClient, stmt.IfNotExists, folderDDL);
            else if (stmt.ResourceDDL is AST.SchemaDDL schemaDDL)
                await ExecuteCreateSchema(tdvClient, stmt.IfNotExists, schemaDDL);
            else if (stmt.ResourceDDL is AST.ViewDDL viewDDL)
                await ExecuteCreateView(tdvClient, stmt.IfNotExists, viewDDL);
            else
                throw new ArgumentOutOfRangeException(nameof(stmt) + "." + nameof(stmt.ResourceDDL), stmt.ResourceDDL.GetType() + " :: " + stmt.ResourceDDL.ToString(), "Unrecognized type of parsed DDL statement");
        }

        private static async Task ExecuteCreateFolder(TdvWebServiceClient tdvClient, bool ifNotExists, FolderDDL stmt)
        {
            using var log = new TraceLog(_log, nameof(ExecuteCreateFolder));

            if (string.IsNullOrEmpty(stmt.ResourcePath))
                throw new ArgumentNullException(nameof(stmt) + "." + nameof(stmt.ResourcePath));

            string folderParentPath = PathExt.TrimLastLevel(stmt.ResourcePath) ?? throw new ArgumentNullException(nameof(folderParentPath));
            _log.Debug($"{nameof(folderParentPath)} = {folderParentPath}");

            string folderName = PathExt.GetLastLevel(stmt.ResourcePath) ?? throw new ArgumentNullException(nameof(folderName));
            _log.Debug($"{nameof(folderName)} = {folderName}");

            string result = await tdvClient.CreateFolder(folderParentPath, folderName, ifNotExists: ifNotExists);
            _log.Debug($"{nameof(result)} = {result}");

            if (ifNotExists)
                _log.Info($"Folder {stmt.ResourcePath} created (or left intact if there already was one)");
            else
                _log.Info($"Folder {stmt.ResourcePath} created");
        }

        private static async Task ExecuteCreateSchema(TdvWebServiceClient tdvClient, bool ifNotExists, SchemaDDL stmt)
        {
            using var log = new TraceLog(_log, nameof(ExecuteCreateSchema));

            if (string.IsNullOrEmpty(stmt.ResourcePath))
                throw new ArgumentNullException(nameof(stmt) + "." + nameof(stmt.ResourcePath));

            string result = await tdvClient.CreateSchemas(new string[] { stmt.ResourcePath }, ifNotExists: ifNotExists);
            _log.Debug($"{nameof(result)} = {result}");

            if (ifNotExists)
                _log.Info($"Schema {stmt.ResourcePath} created (or left intact if there already was one)");
            else
                _log.Info($"Schema {stmt.ResourcePath} created");
        }

        private static async Task ExecuteCreateView(TdvWebServiceClient tdvClient, bool ifNotExists, ViewDDL stmt)
        {
            using var log = new TraceLog(_log, nameof(ExecuteCreateView));

            if (string.IsNullOrEmpty(stmt.ResourcePath))
                throw new ArgumentNullException(nameof(stmt) + "." + nameof(stmt.ResourcePath));

            string? parentPath = PathExt.TrimLastLevel(stmt.ResourcePath);
            if (string.IsNullOrEmpty(parentPath))
                throw new ArgumentOutOfRangeException(nameof(stmt) + "." + nameof(stmt.ResourcePath), stmt.ResourcePath, "Cannot determine view's parent path");

            string? viewName = PathExt.GetLastLevel(stmt.ResourcePath);
            if (string.IsNullOrEmpty(viewName))
                throw new ArgumentOutOfRangeException(nameof(stmt) + "." + nameof(stmt.ResourcePath), stmt.ResourcePath, "Cannot determine view's name");

            if (string.IsNullOrWhiteSpace(stmt.ViewQuery))
                throw new ArgumentNullException(nameof(stmt) + "." + nameof(stmt.ViewQuery), "Empty view body");

            await tdvClient.CreateDataView(parentPath, viewName, stmt.ViewQuery, ifNotExists: ifNotExists);

            if (ifNotExists)
                _log.Info($"View {stmt.ResourcePath} created (or left intact if there already was one)");
            else
                _log.Info($"View {stmt.ResourcePath} created");
        }

        private static async Task ExecuteDescribe(TdvWebServiceClient tdvClient, AST.Describe stmt)
        {
            using var log = new TraceLog(_log, nameof(ExecuteDescribe));

            if (stmt.Resources is null || !stmt.Resources.Any())
                throw new ArgumentNullException(nameof(stmt) + "." + nameof(stmt.Resources));

            IEnumerable<IAsyncEnumerable<WSDL.Admin.resource>> getResourceInfoTasks = stmt.Resources
                .Select(res => tdvClient.GetResourceInfo(res.Path, res.Type))
                .ToList();

            foreach (IAsyncEnumerable<WSDL.Admin.resource> resources in getResourceInfoTasks)
            {
                await foreach (WSDL.Admin.resource res in resources)
                {
                    _log.Info($"resource: {res.path}\n\ttype: {res.type}\n\tsubtype: {res.subtype}\n\towner: {res.ownerName}@{res.ownerDomain}\n\tversion: {res.version}\n\tannotation: {res.annotation}");
                    /* 2do! describe also the rest...
                    [System.Xml.Serialization.XmlIncludeAttribute(typeof(tableResource))]
                    [System.Xml.Serialization.XmlIncludeAttribute(typeof(definitionSetResource))]
                    [System.Xml.Serialization.XmlIncludeAttribute(typeof(procedureResource))]
                    [System.Xml.Serialization.XmlIncludeAttribute(typeof(containerResource))]
                    [System.Xml.Serialization.XmlIncludeAttribute(typeof(dataSourceResource))]
                    [System.Xml.Serialization.XmlIncludeAttribute(typeof(triggerResource))]
                    [System.Xml.Serialization.XmlIncludeAttribute(typeof(linkResource))]
                    [System.Xml.Serialization.XmlIncludeAttribute(typeof(treeResource))]
                    */
                }
            }
        }

        private static async Task ExecuteDropResource(TdvWebServiceClient tdvClient, DropResource stmt)
        {
            using var log = new TraceLog(_log, nameof(ExecuteDropResource));

            if (stmt.Resources is null || !stmt.Resources.Any())
                throw new ArgumentNullException(nameof(stmt) + "." + nameof(stmt.Resources));

            IEnumerable<ResourceSpecifier> nonemptyResourceSpecifiers = stmt.Resources
                .Where(resource => !string.IsNullOrWhiteSpace(resource.Path));

            if (stmt.AlsoDropRootResource)
            {
                IEnumerable<TdvResourceSpecifier> resources = nonemptyResourceSpecifiers
                    .Select(resource => new TdvResourceSpecifier(resource.Path ?? string.Empty, new TdvResourceType(resource.Type.ToString(), null)));

                await tdvClient.DropAnyResources(resources, stmt.IfExists);
            }
            else
            {
                IEnumerable<Task> purgeTasks = nonemptyResourceSpecifiers
                    .Select(resource => tdvClient.PurgeContainer(resource.Path, stmt.IfExists));

                await Task.WhenAll(purgeTasks);
            }

            _log.Info(nonemptyResourceSpecifiers.Count().ToString() + " resource(s) "
                + (stmt.AlsoDropRootResource ? "dropped" : "purged")
                + " OK"
            );
        }

        private static async Task ExecuteGrant(TdvWebServiceClient tdvClient, AST.Grant stmt)
        {
            using var log = new TraceLog(_log, nameof(ExecuteGrant));

            if (stmt.Principals is null || !stmt.Principals.Any())
                throw new ArgumentNullException(nameof(stmt) + "." + nameof(stmt.Principals));

            if (stmt.Privileges is null || !stmt.Privileges.Any())
                throw new ArgumentNullException(nameof(stmt) + "." + nameof(stmt.Privileges));

            if (stmt.Resources is null || !stmt.Resources.Any())
                throw new ArgumentNullException(nameof(stmt) + "." + nameof(stmt.Resources));

            string privilegesConcatenated = string.Join(
                ' ',
                stmt.Privileges.Select(x => x.ToString().ToUpper())
            );

            IEnumerable<string> domainsForWildcardMatching = stmt.Principals
                .Where(principal => principal.LookupOperator != AST.LookupOperatorEnum.EqualTo)
                .Where(principal => !string.IsNullOrEmpty(principal.Domain))
                .Select(principal => principal.Domain ?? string.Empty)
                .Distinct()
                .ToList();

            // retrieve domain->group, domain->user mappings from server, if needed
            Dictionary<string, List<string>>? allDomainGroups = new ();
            Dictionary<string, List<string>>? allDomainUsers = new ();
            if (domainsForWildcardMatching.Any())
            {
                _log.Debug($"{domainsForWildcardMatching.Count()} unique domains identified for \"wildcard\"-matching principals");

                IEnumerable<Tuple<string, WSDL.Admin.userNameType, Task<List<string>>>> domainGroupRetrievalTasks = domainsForWildcardMatching
                    .Select(domain => new Tuple<string, WSDL.Admin.userNameType, IAsyncEnumerable<string>>(domain, WSDL.Admin.userNameType.GROUP, tdvClient.GetDomainGroups(domain)))
                    .Select(task => new Tuple<string, WSDL.Admin.userNameType, Task<List<string>>>(task.Item1, task.Item2, task.Item3.ToListAsync().AsTask()));

                IEnumerable<Tuple<string, WSDL.Admin.userNameType, Task<List<string>>>> userGroupRetrievalTasks = domainsForWildcardMatching
                    .Select(domain => new Tuple<string, WSDL.Admin.userNameType, IAsyncEnumerable<string>>(domain, WSDL.Admin.userNameType.USER, tdvClient.GetDomainUsers(domain)))
                    .Select(task => new Tuple<string, WSDL.Admin.userNameType, Task<List<string>>>(task.Item1, task.Item2, task.Item3.ToListAsync().AsTask()));

                await Task.WhenAll(domainGroupRetrievalTasks
                    .Concat(userGroupRetrievalTasks)
                    .Select(task => task.Item3)
                );

                foreach (Tuple<string, WSDL.Admin.userNameType, Task<List<string>>> task in domainGroupRetrievalTasks)
                {
                    if (_log.IsDebugEnabled)
                        _log.Debug($"retrieved domain {task.Item1} groups: [\n" + string.Join(",\n\t", task.Item3.Result) + "\n]");

                    allDomainGroups.Add(task.Item1, task.Item3.Result);
                }

                foreach (Tuple<string, WSDL.Admin.userNameType, Task<List<string>>> task in userGroupRetrievalTasks)
                {
                    if (_log.IsDebugEnabled)
                        _log.Debug($"retrieved domain {task.Item1} users: [\n" + string.Join(",\n\t", task.Item3.Result) + "\n]");

                    allDomainUsers.Add(task.Item1, task.Item3.Result);
                }
            }

            // calculate the final set of grantees
            IEnumerable<WSDL.Admin.privilege> granteesMatchedByEquality = stmt.Principals
                .Where(principal => principal.LookupOperator == AST.LookupOperatorEnum.EqualTo)
                .Select(principal => new WSDL.Admin.privilege()
                {
                    domain = principal.Domain,
                    name = principal.Name,
                    nameType = principal.Type ?? throw new ArgumentNullException(nameof(principal) + "." + nameof(principal.Type)),
                    privs = privilegesConcatenated
                });

            IEnumerable<WSDL.Admin.privilege> granteeGroupsMatchedByRegexp = new List<WSDL.Admin.privilege>();
            if (allDomainGroups is not null)
            {
                granteeGroupsMatchedByRegexp = allDomainGroups
                    .Unnest(
                        domainGroups => domainGroups.Value,
                        (domainGroups, groupName) => new { ServerDomain = domainGroups.Key, ServerGroup = groupName }
                    )
                    .CrossProduct(stmt.Principals
                        .Where(principal => principal.Type == WSDL.Admin.userNameType.GROUP)
                        .Where(principal => principal.LookupOperator == AST.LookupOperatorEnum.RegexpLike)
                        .Select(principal => new { GranteePrincipal = principal, GranteeWildcard = RegexExt.ParseSlashedRegexp(principal.Name, RegexOptions.IgnoreCase) })
                    )
                    .Where(crossRecord => crossRecord.Item1.ServerDomain.Equals(crossRecord.Item2.GranteePrincipal.Domain)
                        && crossRecord.Item2.GranteeWildcard.IsMatch(crossRecord.Item1.ServerGroup)
                    )
                    .Select(match => new WSDL.Admin.privilege()
                    {
                        domain = match.Item1.ServerDomain,
                        name = match.Item1.ServerGroup,
                        nameType = WSDL.Admin.userNameType.GROUP,
                        privs = privilegesConcatenated
                    })
                    .ToList();
            }

            IEnumerable<WSDL.Admin.privilege> granteeUsersMatchedByRegexp = new List<WSDL.Admin.privilege>();
            if (allDomainUsers is not null)
            {
                granteeUsersMatchedByRegexp = allDomainUsers
                    .Unnest(
                        domainUsers => domainUsers.Value,
                        (domainUsers, userName) => new { ServerDomain = domainUsers.Key, ServerUser = userName }
                    )
                    .CrossProduct(stmt.Principals
                        .Where(principal => principal.Type == WSDL.Admin.userNameType.USER)
                        .Where(principal => principal.LookupOperator == AST.LookupOperatorEnum.RegexpLike)
                        .Select(principal => new { GranteePrincipal = principal, GranteeWildcard = RegexExt.ParseSlashedRegexp(principal.Name, RegexOptions.IgnoreCase) })
                    )
                    .Where(crossRecord => crossRecord.Item1.ServerDomain.Equals(crossRecord.Item2.GranteePrincipal.Domain)
                        && crossRecord.Item2.GranteeWildcard.IsMatch(crossRecord.Item1.ServerUser)
                    )
                    .Select(match => new WSDL.Admin.privilege()
                    {
                        domain = match.Item1.ServerDomain,
                        name = match.Item1.ServerUser,
                        nameType = WSDL.Admin.userNameType.USER,
                        privs = privilegesConcatenated
                    })
                    .ToList();
            }

            WSDL.Admin.privilege[] allGrantees = granteesMatchedByEquality
                .Concat(granteeGroupsMatchedByRegexp)
                .Concat(granteeUsersMatchedByRegexp)
                .ToArray();

            List<WSDL.Admin.privilegeEntry> privilegeEntries = stmt.Resources
                .Select(obj => new WSDL.Admin.privilegeEntry()
                {
                    type = Enum.Parse<WSDL.Admin.resourceOrColumnType>(obj.Type.ToString(), false),
                    path = obj.Path,
                    privileges = allGrantees
                })
                .ToList();

            await tdvClient.UpdateResourcePrivileges(privilegeEntries, stmt.IsRecursive, stmt.ModusOperandi);

            string msgModus = stmt.ModusOperandi switch
            {
                WSDL.Admin.updatePrivilegesMode.SET_EXACTLY => "Set",
                WSDL.Admin.updatePrivilegesMode.OVERWRITE_APPEND => "Appended",
                _ => "Granted"
            };
            string msgRecursive = stmt.IsRecursive ? " recursively" : string.Empty;
            _log.Info($"{msgModus} {stmt.Privileges.Count} privileges{msgRecursive} on {stmt.Resources.Count} resources to {allGrantees.Length} principals");
        }
    }
}