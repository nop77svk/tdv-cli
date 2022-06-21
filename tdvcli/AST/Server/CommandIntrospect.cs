namespace NoP77svk.TibcoDV.CLI.AST.Server
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using log4net;
    using NoP77svk.IO;
    using NoP77svk.Linq;
    using NoP77svk.Linq.Ext;
    using NoP77svk.Text.RegularExpressions;
    using NoP77svk.TibcoDV.API;
    using NoP77svk.TibcoDV.CLI.Commons;
    using NoP77svk.TibcoDV.CLI.Parser;
    using NoP77svk.TibcoDV.Commons;
    using WSDL = NoP77svk.TibcoDV.API.WSDL;

    internal class CommandIntrospect : IAsyncExecutable
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(CommandIntrospect));

        private static readonly Regex AddedColumnRX = new Regex(@"^Added\s+column\s+#\d+\s+'");
        private static readonly Regex DeletedColumnRX = new Regex(@"^Deleted\s+column\s+'");
        private static readonly ValueTuple<string, string, string, TdvResourceType, string>[] EmptyListOfIntrospectables = new ValueTuple<string, string, string, TdvResourceType, string>[0];

        public IList<IntrospectTargetDataSource> ScriptInputs { get; }
        public IntrospectionOptionHandleResources OptionHandleResources { get; }

        public CommandIntrospect(IList<IntrospectTargetDataSource> scriptInputs, IntrospectionOptionHandleResources optionHandleResources)
        {
            ScriptInputs = scriptInputs;
            OptionHandleResources = optionHandleResources;
        }

        public async Task Execute(TdvWebServiceClient tdvClient, IInfoOutput output, ParserState parserState)
        {
            using var traceLog = new TraceLog(_log, nameof(Execute));

            string[] uniqueDataSourcePaths = ScriptInputs
                .Select(x => x.DataSourcePath)
                .Distinct()
                .ToArray();
            _log.Debug(string.Join('\n', uniqueDataSourcePaths.Prepend("Unique data source paths to introspect:\n")));

            output.InfoNoEoln($"Resolving introspection metadata for {uniqueDataSourcePaths.Length} data sources");

            Internal.IntrospectableDataSource[] introspectables = await RetrieveIntrospectables(tdvClient, uniqueDataSourcePaths, () => { output.InfoNoEoln("."); });
            if (_log.IsDebugEnabled)
                _log.Debug(string.Join('\n', introspectables.Select(x => x.ToString()).Prepend("Introspectable resources (prior to filter):")));

            ValueTuple<string, string, string, TdvResourceType, string>[] introspectedResources;
            if (OptionHandleResources.DropUnmatched || !OptionHandleResources.UpdateExisting)
            {
                introspectedResources = await RetrieveIntrospectedResources(tdvClient, uniqueDataSourcePaths, () => { output.InfoNoEoln("."); });
                if (_log.IsDebugEnabled)
                    _log.Debug(string.Join('\n', introspectedResources.Select(x => x.ToString()).Prepend("Introspected resources (prior to filters):")));
            }
            else
            {
                introspectedResources = EmptyListOfIntrospectables;
            }

            var filteredIntrospectablesEnumerable = FilterIntrospectablesByInput(introspectables, ScriptInputs);
            filteredIntrospectablesEnumerable = filteredIntrospectablesEnumerable.ToArray();
            if (_log.IsDebugEnabled)
                _log.Debug(string.Join('\n', filteredIntrospectablesEnumerable.Select(x => x.ToString()).Prepend("Introspectables (filtered by input):")));

            var resourcesToDrop = FilterResourcesToDrop(introspectedResources, filteredIntrospectablesEnumerable).ToArray();
            if (_log.IsDebugEnabled)
                _log.Debug(string.Join('\n', resourcesToDrop.Select(x => x.ToString()).Prepend("Resources to be dropped:")));

            if (!OptionHandleResources.UpdateExisting)
            {
                filteredIntrospectablesEnumerable = filteredIntrospectablesEnumerable
                    .AntiJoin(
                        antiJoinedTable: introspectedResources,
                        outerKeySelector: outer => (outer.Item1, outer.Item2, outer.Item3, outer.Item4.Type, outer.Item5),
                        antiJoinKeySelector: anti => (anti.Item1, anti.Item2, anti.Item3, anti.Item4.Type, anti.Item5)
                    );
            }

            var filteredIntrospectables = filteredIntrospectablesEnumerable.ToArray();
            if (_log.IsDebugEnabled)
                _log.Debug(string.Join('\n', filteredIntrospectables.Select(x => x.ToString()).Prepend("Resources to be added:")));

            output.Info(" done");

            if (filteredIntrospectables.Length > 0 || resourcesToDrop.Length > 0)
            {
                bool updateExistingResourcesOverride = OptionHandleResources.UpdateExisting;
                List<ValueTuple<string, Internal.IntrospectionResultSimplified>[]> failedIntrospectionIterations = new ();

                for (int iteration = 0; true; iteration++)
                {
                    int jobsToBeSpawned = filteredIntrospectables
                        .Select(x => x.Item1)
                        .Union(resourcesToDrop.Select(x => x.Item1))
                        .Count();

                    ValueTuple<string, Internal.IntrospectionResultSimplified[]>[] introspectionResult;
                    output.Info($"Introspecting {jobsToBeSpawned} data sources...");
                    using (var progressFeedback = new Internal.IntrospectionProgressFeedback(output, jobsToBeSpawned))
                        introspectionResult = await RunTheIntrospection(tdvClient, filteredIntrospectablesEnumerable, resourcesToDrop, updateExistingResourcesOverride, progressFeedback.Feedback);

                    var failedIntrospectionObjects = introspectionResult
                        .Unnest(
                            retrieveNestedCollection: x => x.Item2,
                            resultSelector: (outer, inner) => new ValueTuple<string, Internal.IntrospectionResultSimplified>(outer.Item1, inner)
                        )
                        .Where(x => x.Item2.HasFailedIntrospectables)
                        .ToArray();
                    failedIntrospectionIterations.Add(failedIntrospectionObjects);

                    if (failedIntrospectionObjects.Length > 0)
                    {
                        output.Info($"There are {failedIntrospectionObjects.Length} objects that failed being introspected; reintrospection necessary!");

                        // drop failed introspectables first
                        var failedIntrospectablesToBeDropped = failedIntrospectionObjects
                            .Select(x =>
                            {
                                var splitPath = x.Item2.Path.Split('/', 3);
                                return new ValueTuple<string, string, string, TdvResourceType, string>(
                                    x.Item1,
                                    splitPath.Length >= 1 ? splitPath[0] : string.Empty,
                                    splitPath.Length >= 2 ? splitPath[1] : string.Empty,
                                    x.Item2.ResourceType,
                                    splitPath.Length >= 3 ? splitPath[2] : string.Empty
                                );
                            })
                            .ToArray();
                        using (var progressFeedback = new Internal.IntrospectionProgressFeedback(output, jobsToBeSpawned))
                            await RunTheIntrospection(tdvClient, EmptyListOfIntrospectables, failedIntrospectablesToBeDropped, false, progressFeedback.Feedback);

                        // prepare for reintrospection of the failed ones as the next iteration
                        filteredIntrospectables = failedIntrospectablesToBeDropped;
                        resourcesToDrop = EmptyListOfIntrospectables;
                        updateExistingResourcesOverride = false;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else
            {
                output.Info("No introspectable/droppable resources identified");
            }

            output.Info("Introspection done");
        }

        public override string? ToString()
        {
            return $"{base.ToString()}[{ScriptInputs.Count}]";
        }

        private static IEnumerable<ValueTuple<string, string, string, TdvResourceType, string>> FilterIntrospectablesByInput(IEnumerable<Internal.IntrospectableDataSource> introspectablesGrouped, IEnumerable<Server.IntrospectTargetDataSource> commandInput)
        {
            IEnumerable<Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableDataSource, IntrospectTargetDataSource>> dataSourcesToIntrospect = FilterDataSourcesToIntrospect(commandInput, introspectablesGrouped);
            foreach (var dataSourceJoin in dataSourcesToIntrospect)
            {
                IEnumerable<Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableCatalog, IntrospectTargetCatalog>> catalogsToIntrospect = FilterCatalogsToIntrospect(dataSourceJoin);
                foreach (var catalogJoin in catalogsToIntrospect)
                {
                    yield return new ValueTuple<string, string, string, TdvResourceType, string>(dataSourceJoin.Introspectable.DataSource, catalogJoin.Introspectable.CatalogName, string.Empty, new TdvResourceType(TdvResourceTypeEnumAgr.Catalog), string.Empty);

                    IEnumerable<Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableSchema, IntrospectTargetSchema>> schemasToIntrospect = FilterSchemasToIntrospect(catalogJoin);
                    foreach (var schemaJoin in schemasToIntrospect)
                    {
                        yield return new ValueTuple<string, string, string, TdvResourceType, string>(dataSourceJoin.Introspectable.DataSource, catalogJoin.Introspectable.CatalogName, schemaJoin.Introspectable.SchemaName, new TdvResourceType(TdvResourceTypeEnumAgr.Schema), string.Empty);

                        IEnumerable<Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableObject, IntrospectTargetTable>> objectsToIntrospect = FilterObjectsToIntrospect(schemaJoin);
                        foreach (var objectJoin in objectsToIntrospect)
                        {
                            yield return new ValueTuple<string, string, string, TdvResourceType, string>(dataSourceJoin.Introspectable.DataSource, catalogJoin.Introspectable.CatalogName, schemaJoin.Introspectable.SchemaName, objectJoin.Introspectable.ObjectType, objectJoin.Introspectable.ObjectName);
                        }
                    }
                }
            }
        }

        private static IEnumerable<Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableCatalog, IntrospectTargetCatalog>> FilterCatalogsToIntrospect(Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableDataSource, IntrospectTargetDataSource> dataSourceJoin)
        {
            if (dataSourceJoin.Input != null && dataSourceJoin.Input.Catalogs.Count > 0)
            {
                IEnumerable<Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableCatalog, IntrospectTargetCatalog>> catalogJoinOnEquality = dataSourceJoin.Introspectable.Catalogs
                    .Join(
                        inner: dataSourceJoin.Input.Catalogs
                            .Where(x => x.CatalogName is Infra.MatchExactly),
                        outerKeySelector: x => x.CatalogName,
                        innerKeySelector: x => x.CatalogName.Value,
                        resultSelector: (outer, inner) => new Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableCatalog, IntrospectTargetCatalog>(outer, inner)
                    );

                IEnumerable<Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableCatalog, IntrospectTargetCatalog>> catalogJoinOnRegExp = dataSourceJoin.Introspectable.Catalogs
                    .JoinByRegexpMatch(
                        valueSelector: x => x.CatalogName,
                        regexps: dataSourceJoin.Input.Catalogs
                            .Where(x => x.CatalogName is Infra.MatchByRegExp),
                        regexpSelector: x => RegexExt.ParseSlashedRegexp(x.CatalogName.Value)
                    )
                    .Select(x => new Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableCatalog, IntrospectTargetCatalog>(x.Item1, x.Item2));

                return catalogJoinOnEquality
                    .Concat(catalogJoinOnRegExp)
                    .Where(x => x.Introspectable.CatalogName != string.Empty);
            }
            else
            {
                return dataSourceJoin.Introspectable.Catalogs
                    .Select(x => new Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableCatalog, IntrospectTargetCatalog>(x, null))
                    .Where(x => x.Introspectable.CatalogName != string.Empty);
            }
        }

        private static IEnumerable<Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableDataSource, IntrospectTargetDataSource>> FilterDataSourcesToIntrospect(IEnumerable<IntrospectTargetDataSource> dataSources, IEnumerable<Internal.IntrospectableDataSource> multiGetIntrospectableResourcesGrouped)
        {
            return multiGetIntrospectableResourcesGrouped
                .Join(
                    inner: dataSources,
                    outerKeySelector: x => x.DataSource,
                    innerKeySelector: x => x.DataSourcePath,
                    resultSelector: (outer, inner) => new Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableDataSource, IntrospectTargetDataSource>(outer, inner)
                );
        }

        private static IEnumerable<Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableObject, IntrospectTargetTable>> FilterObjectsToIntrospect(Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableSchema, IntrospectTargetSchema> schemaJoin)
        {
            if (schemaJoin.Input != null && schemaJoin.Input.Tables.Any())
            {
                var inputs = schemaJoin.Input.Tables.ToArray();

                foreach (var introspectable in schemaJoin.Introspectable.Objects)
                {
                    if (introspectable.ObjectName == string.Empty)
                        continue;

                    // if the inclusion/exclusion list starts with "include", then assume no objects are to be included automatically
                    // if the inclusion/exclusion list starts with "exclude", then assume all objects are to be included automatically
                    // if the inclusion/exclusion list is empty, then assume all objects are to be included automatically
                    bool spoolTheIntrospectable = inputs.Length <= 0 || inputs[0].ElementOperation == Infra.SetElementOperation.Exclude;

                    for (int i = 0; i < inputs.Length; i++)
                    {
                        bool objectNameMatch = inputs[i].TableName switch
                        {
                            Infra.MatchExactly exactInput => introspectable.ObjectName == exactInput.Value,
                            Infra.MatchByRegExp rxInput => rxInput.RegExp.IsMatch(introspectable.ObjectName),
                            _ => throw new NotImplementedException($"Don't know how to handle {inputs[i].TableName.GetType()} input")
                        };

                        if (objectNameMatch)
                        {
                            spoolTheIntrospectable = inputs[i].ElementOperation switch
                            {
                                Infra.SetElementOperation.Include => true,
                                Infra.SetElementOperation.Exclude => false,
                                _ => throw new NotImplementedException($"Don't know how to handle element set operation {inputs[i].ElementOperation}")
                            };
                        }
                    }

                    if (spoolTheIntrospectable)
                    {
                        yield return new Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableObject, IntrospectTargetTable>(introspectable, null);
                    }
                }
            }
            else
            {
                foreach (var row in schemaJoin.Introspectable.Objects
                    .Select(x => new Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableObject, IntrospectTargetTable>(x, null))
                    .Where(x => x.Introspectable.ObjectName != string.Empty)
                )
                    yield return row;
            }
        }

        private static IEnumerable<ValueTuple<string, string, string, TdvResourceType, string>> FilterResourcesToDrop(IEnumerable<ValueTuple<string, string, string, TdvResourceType, string>> introspectedResources, IEnumerable<ValueTuple<string, string, string, TdvResourceType, string>> filteredIntrospectables)
        {
            return introspectedResources
                .Where(x => !(x.Item4.Type == TdvResourceTypeEnumAgr.Trigger && x.Item2.Equals("ScheduledReintrospection", StringComparison.OrdinalIgnoreCase)))
                .AntiJoin(
                    antiJoinedTable: filteredIntrospectables,
                    outerKeySelector: outer => new ValueTuple<string, string, string, string>(outer.Item1, outer.Item2, outer.Item3, outer.Item5),
                    antiJoinKeySelector: inner => new ValueTuple<string, string, string, string>(inner.Item1, inner.Item2, inner.Item3, inner.Item5)
                );
        }

        private static IEnumerable<Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableSchema, IntrospectTargetSchema>> FilterSchemasToIntrospect(Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableCatalog, IntrospectTargetCatalog> catalogJoin)
        {
            if (catalogJoin.Input != null && catalogJoin.Input.Schemas.Any())
            {
                IEnumerable<Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableSchema, IntrospectTargetSchema>> schemaJoinOnEquality = catalogJoin.Introspectable.Schemas
                    .Join(
                        inner: catalogJoin.Input.Schemas
                            .Where(x => x.SchemaName is Infra.MatchExactly),
                        outerKeySelector: x => x.SchemaName,
                        innerKeySelector: x => x.SchemaName.Value,
                        resultSelector: (outer, inner) => new Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableSchema, IntrospectTargetSchema>(outer, inner)
                    );

                IEnumerable<Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableSchema, IntrospectTargetSchema>> schemaJoinOnRegExp = catalogJoin.Introspectable.Schemas
                    .JoinByRegexpMatch(
                        valueSelector: x => x.SchemaName,
                        regexps: catalogJoin.Input.Schemas
                            .Where(x => x.SchemaName is Infra.MatchByRegExp),
                        regexpSelector: x => RegexExt.ParseSlashedRegexp(x.SchemaName.Value)
                    )
                    .Select(x => new Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableSchema, IntrospectTargetSchema>(x.Item1, x.Item2));

                return schemaJoinOnEquality
                    .Concat(schemaJoinOnRegExp)
                    .Where(x => x.Introspectable.SchemaName != string.Empty);
            }
            else
            {
                return catalogJoin.Introspectable.Schemas
                    .Select(x => new Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableSchema, IntrospectTargetSchema>(x, null))
                    .Where(x => x.Introspectable.SchemaName != string.Empty);
            }
        }

        private static async Task<Internal.IntrospectableDataSource[]> RetrieveIntrospectables(TdvWebServiceClient tdvClient, IEnumerable<string> uniqueDataSourcePaths, Action? progressFeedback = null)
        {
            Internal.IntrospectableDataSource[] result;

            NamedTask<WSDL.Admin.linkableResourceId[]>[] multiGetIntrospectableResources = uniqueDataSourcePaths
                .Select(x =>
                {
                    progressFeedback?.Invoke();
                    return x;
                })
                .Select(dataSourcePath => new NamedTask<WSDL.Admin.linkableResourceId[]>(
                    dataSourcePath,
                    tdvClient.PolledServerTaskEnumerable(new API.PolledServerTasks.GetIntrospectableResourceIdsPolledServerTaskHandler(tdvClient, dataSourcePath, true), responseFeedback: x => { progressFeedback?.Invoke(); })
                        .ToArrayAsync().AsTask()
                ))
                .ToArray();

            try
            {
                progressFeedback?.Invoke();
                await Task.WhenAll(multiGetIntrospectableResources.Select(x => x.Task));
                progressFeedback?.Invoke();

                result = multiGetIntrospectableResources
                    .Unnest(
                        retrieveNestedCollection: x => x.Task.Result,
                        resultSelector: (outer, inner) => new ValueTuple<string, WSDL.Admin.linkableResourceId>(outer.Name, inner)
                    )
                    .Select(x =>
                    {
                        var splitPath = x.Item2.resourceId.path.Split('/', 3);
                        return new ValueTuple<string, string, string, TdvResourceType, string>(
                            x.Item1,
                            splitPath.Length >= 1 ? splitPath[0] : string.Empty,
                            splitPath.Length >= 2 ? splitPath[1] : string.Empty,
                            new TdvResourceType(x.Item2.resourceId.type, x.Item2.resourceId.subtype),
                            splitPath.Length >= 3 ? splitPath[2] : string.Empty
                        );
                    })
                    .Where(x => !string.IsNullOrEmpty(x.Item5) && !string.IsNullOrEmpty(x.Item3) && !string.IsNullOrEmpty(x.Item2) && !string.IsNullOrEmpty(x.Item1)) // 2do! should maybe throw an exception instead of silently out-filtering stuff
                    .Distinct()
                    .GroupBy(
                        keySelector: x => new ValueTuple<string, string, string>(x.Item1, x.Item2, x.Item3),
                        elementSelector: x => new Internal.IntrospectableObject(x.Item4, x.Item5)
                    )
                    .GroupBy(
                        keySelector: x => new ValueTuple<string, string>(x.Key.Item1, x.Key.Item2),
                        elementSelector: x => new Internal.IntrospectableSchema(x.Key.Item3, x.ToArray())
                    )
                    .GroupBy(
                        keySelector: x => x.Key.Item1,
                        elementSelector: x => new Internal.IntrospectableCatalog(x.Key.Item2, x.ToArray())
                    )
                    .Select(x => new Internal.IntrospectableDataSource(x.Key, x.ToArray()))
                    .ToArray();

                return result;
            }
            finally
            {
                foreach (var task in multiGetIntrospectableResources)
                    task.Dispose();
            }
        }

        private static async Task<ValueTuple<string, string, string, TdvResourceType, string>[]> RetrieveIntrospectedResources(TdvWebServiceClient tdvClient, IEnumerable<string> uniqueDataSourcePaths, Action? progressFeedback = null)
        {
            ValueTuple<string, string, string, TdvResourceType, string>[] result;

            NamedTask<WSDL.Admin.resource>[] multiGetResourceInfo = uniqueDataSourcePaths
                .Select(x =>
                {
                    progressFeedback?.Invoke();
                    return x;
                })
                .Select(dataSourcePath => new NamedTask<WSDL.Admin.resource>(
                    dataSourcePath,
                    tdvClient.GetResourceInfo(dataSourcePath)
                        .Select(x =>
                        {
                            progressFeedback?.Invoke();
                            return x;
                        })
                        .FirstAsync().AsTask()
                ))
                .ToArray();

            try
            {
                progressFeedback?.Invoke();
                await Task.WhenAll(multiGetResourceInfo.Select(x => x.Task));
                progressFeedback?.Invoke();

                NamedTask<TdvRest_ContainerContents[]>[] multiGetIntrospectedResources = multiGetResourceInfo
                    .Select(x =>
                    {
                        progressFeedback?.Invoke();
                        return x;
                    })
                    .Select(gri => new ValueTuple<string, TdvResourceType>(gri.Name, new TdvResourceType(gri.Task.Result.type, gri.Task.Result.subtype)))
                    .Select(x => new NamedTask<TdvRest_ContainerContents[]>(
                        x.Item1,
                        tdvClient.RetrieveContainerContentsRecursive(x.Item1, x.Item2.Type)
                            .ToArrayAsync().AsTask()
                    ))
                    .ToArray();

                try
                {
                    progressFeedback?.Invoke();
                    await Task.WhenAll(multiGetIntrospectedResources.Select(x => x.Task));
                    progressFeedback?.Invoke();

                    result = multiGetIntrospectedResources
                        .Unnest(
                            retrieveNestedCollection: x => x.Task.Result,
                            resultSelector: (outer, inner) => new ValueTuple<string, TdvRest_ContainerContents>(outer.Name, inner)
                        )
                        .Select(x =>
                        {
                            if (x.Item2.Path == null)
                                throw new NullReferenceException("NULL resource path detected");
                            if (x.Item2.Type == null)
                                throw new NullReferenceException($"NULL resource type detected on {x.Item2.Path}");
                            if (x.Item2.SubType == null)
                                throw new NullReferenceException($"NULL resource subtype detected on {x.Item2.Type.ToLower()} {x.Item2.Path}");

                            string[] splitPath = (PathExt.TrimLeadingPath(x.Item2.Path, x.Item1) ?? string.Empty).TrimStart('/').Split('/', 3);
                            return new ValueTuple<string, string, string, TdvResourceType, string>(
                                x.Item1,
                                splitPath.Length >= 1 ? splitPath[0] : string.Empty,
                                splitPath.Length >= 2 ? splitPath[1] : string.Empty,
                                new TdvResourceType(x.Item2.Type, x.Item2.SubType),
                                splitPath.Length >= 3 ? splitPath[2] : string.Empty
                            );
                        })
                        .Distinct()
                        .ToArray();

                    return result;
                }
                finally
                {
                    foreach (var task in multiGetIntrospectedResources)
                        task.Dispose();
                }
            }
            finally
            {
                foreach (var task in multiGetResourceInfo)
                    task.Dispose();
            }
        }

        private async Task<ValueTuple<string, Internal.IntrospectionResultSimplified[]>[]> RunTheIntrospection(
            TdvWebServiceClient tdvClient,
            IEnumerable<ValueTuple<string, string, string, TdvResourceType, string>> introspectables,
            IEnumerable<ValueTuple<string, string, string, TdvResourceType, string>> resourcesToDrop,
            bool updateExisting,
            Action<WSDL.Admin.introspectResourcesResultResponse>? progressIndicator = null
        )
        {
            var introspectionPlan = introspectables
                .Select(x => new ValueTuple<string, WSDL.Admin.introspectionPlanEntry>(
                    x.Item1,
                    new WSDL.Admin.introspectionPlanEntry()
                    {
                        action = WSDL.Admin.introspectionPlanAction.ADD_OR_UPDATE,
                        resourceId = new WSDL.Admin.pathTypeSubtype()
                        {
                            path = x.Item4.Type switch
                            {
                                TdvResourceTypeEnumAgr.Catalog => x.Item2,
                                TdvResourceTypeEnumAgr.Schema => string.Join('/', x.Item2, x.Item3),
                                _ => string.Join('/', x.Item2, x.Item3, x.Item5)
                            },
                            type = x.Item4.WsType,
                            subtype = x.Item4.WsSubType
                        }
                    }
                ))
                .Concat(resourcesToDrop
                    .Select(x => new ValueTuple<string, WSDL.Admin.introspectionPlanEntry>(
                        x.Item1,
                        new WSDL.Admin.introspectionPlanEntry()
                        {
                            action = WSDL.Admin.introspectionPlanAction.REMOVE,
                            resourceId = new WSDL.Admin.pathTypeSubtype()
                            {
                                path = x.Item4.Type switch
                                {
                                    TdvResourceTypeEnumAgr.Catalog => x.Item2,
                                    TdvResourceTypeEnumAgr.Schema => string.Join('/', x.Item2, x.Item3),
                                    _ => string.Join('/', x.Item2, x.Item3, x.Item5)
                                },
                                type = x.Item4.WsType,
                                subtype = x.Item4.WsSubType
                            }
                        }
                    ))
                )
                .GroupBy(
                    keySelector: x => x.Item1,
                    elementSelector: x => x.Item2
                );

            var multiIntrospection = introspectionPlan
                .Select(x => new NamedTask<Internal.IntrospectionResultSimplified[]>(
                    x.Key,
                    tdvClient.PolledServerTaskEnumerable(
                        new API.PolledServerTasks.IntrospectWithDetailsPolledServerTaskHandler(tdvClient, x.Key, x.AsEnumerable())
                        {
                            IntrospectionOptions = new TdvIntrospectionOptions()
                            {
                                UpdateAllIntrospectedResources = updateExisting
                            }
                        },
                        progressIndicator
                    )
                    .Select(res => new Internal.IntrospectionResultSimplified(res.action, new TdvResourceType(res.type, res.subtype), res.path, res.status)
                    {
                        HasAddedColumns = res.action is WSDL.Admin.introspectionAction.ADD or WSDL.Admin.introspectionAction.UPDATE
                            && res.type == WSDL.Admin.resourceType.TABLE
                            && res.messages
                                .Where(msg => msg.severity == WSDL.Admin.messageSeverity.INFO)
                                .Where(msg => AddedColumnRX.IsMatch(msg.message))
                                .Any(),
                        HasDeletedColumns = res.action == WSDL.Admin.introspectionAction.UPDATE
                            && res.type == WSDL.Admin.resourceType.TABLE
                            && res.messages
                                .Where(msg => msg.severity == WSDL.Admin.messageSeverity.INFO)
                                .Where(msg => DeletedColumnRX.IsMatch(msg.message))
                                .Any()
                    })
                    .ToArrayAsync().AsTask()
                ))
                .ToArray();

            try
            {
                await Task.WhenAll(multiIntrospection.Select(x => x.Task));

                return multiIntrospection
                    .Select(x => new ValueTuple<string, Internal.IntrospectionResultSimplified[]>(x.Name, x.Task.Result))
                    .ToArray();
            }
            finally
            {
                foreach (var task in multiIntrospection)
                    task.Dispose();
            }
        }
    }
}
