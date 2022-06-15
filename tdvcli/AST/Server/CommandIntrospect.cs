namespace NoP77svk.TibcoDV.CLI.AST.Server
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NoP77svk.IO;
    using NoP77svk.Linq;
    using NoP77svk.Linq.Ext;
    using NoP77svk.Text.RegularExpressions;
    using NoP77svk.TibcoDV.API;
    using NoP77svk.TibcoDV.CLI.Commons;
    using NoP77svk.TibcoDV.CLI.Parser;
    using WSDL = NoP77svk.TibcoDV.API.WSDL;

    internal class CommandIntrospect : IAsyncExecutable
    {
        public IList<IntrospectTargetDataSource> ScriptInputs { get; }
        public IntrospectionOptionHandleResources OptionHandleResources { get; }

        public CommandIntrospect(IList<IntrospectTargetDataSource> scriptInputs, IntrospectionOptionHandleResources optionHandleResources)
        {
            ScriptInputs = scriptInputs;
            OptionHandleResources = optionHandleResources;
        }

        public async Task Execute(TdvWebServiceClient tdvClient, IInfoOutput output, ParserState parserState)
        {
            string[] uniqueDataSourcePaths = ScriptInputs
                .Select(x => x.DataSourcePath)
                .Distinct()
                .ToArray();

            output.Info($"Introspecting {uniqueDataSourcePaths.Length} data sources...");
            IEnumerable<Internal.IntrospectableDataSource> introspectables = await RetrieveIntrospectables(tdvClient, uniqueDataSourcePaths);
            IEnumerable<ValueTuple<string, string, string, TdvResourceType, string>> introspectedResources;
            if (OptionHandleResources.DropUnmatched)
                introspectedResources = await RetrieveIntrospectedResources(tdvClient, uniqueDataSourcePaths);
            else
                introspectedResources = new ValueTuple<string, string, string, TdvResourceType, string>[0];

            var filteredIntrospectables = FilterIntrospectablesByInput(introspectables, ScriptInputs).ToArray();
            var resourcesToDrop = FilterResourcesToDrop(introspectedResources, filteredIntrospectables);

            await RunTheIntrospection(tdvClient, output, filteredIntrospectables, resourcesToDrop);
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
                    yield return new ValueTuple<string, string, string, TdvResourceType, string>(dataSourceJoin.Introspectable.DataSource, catalogJoin.Introspectable.CatalogName, string.Empty, new TdvResourceType(TdvResourceTypeEnumAgr.PublishedCatalog), string.Empty);

                    IEnumerable<Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableSchema, IntrospectTargetSchema>> schemasToIntrospect = FilterSchemasToIntrospect(catalogJoin);
                    foreach (var schemaJoin in schemasToIntrospect)
                    {
                        yield return new ValueTuple<string, string, string, TdvResourceType, string>(dataSourceJoin.Introspectable.DataSource, catalogJoin.Introspectable.CatalogName, schemaJoin.Introspectable.SchemaName, new TdvResourceType(TdvResourceTypeEnumAgr.PublishedSchema), string.Empty);

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
                .RightAntiSemiJoin(
                    outerTable: filteredIntrospectables,
                    antiJoinedKeySelector: innerRow => new ValueTuple<string, string, string, string>(innerRow.Item1, innerRow.Item2, innerRow.Item3, innerRow.Item5),
                    outerKeySelector: outerRow => new ValueTuple<string, string, string, string>(outerRow.Item1, outerRow.Item2, outerRow.Item3, outerRow.Item5),
                    resultSelector: (innerRow, outerRow) => innerRow
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

        private static async Task<Internal.IntrospectableDataSource[]> RetrieveIntrospectables(TdvWebServiceClient tdvClient, IEnumerable<string> uniqueDataSourcePaths)
        {
            Internal.IntrospectableDataSource[] result;

            NamedTask<WSDL.Admin.linkableResourceId[]>[] multiGetIntrospectableResources = uniqueDataSourcePaths
                .Select(dataSourcePath => new NamedTask<WSDL.Admin.linkableResourceId[]>(
                    dataSourcePath,
                    tdvClient.PolledServerTaskEnumerable(new API.PolledServerTasks.GetIntrospectableResourceIdsPolledServerTaskHandler(tdvClient, dataSourcePath, true))
                        .ToArrayAsync().AsTask()
                ))
                .ToArray();

            try
            {
                await Task.WhenAll(multiGetIntrospectableResources.Select(x => x.Task));

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

        private static async Task<ValueTuple<string, string, string, TdvResourceType, string>[]> RetrieveIntrospectedResources(TdvWebServiceClient tdvClient, IEnumerable<string> uniqueDataSourcePaths)
        {
            ValueTuple<string, string, string, TdvResourceType, string>[] result;

            NamedTask<WSDL.Admin.resource>[] multiGetResourceInfo = uniqueDataSourcePaths
                .Select(dataSourcePath => new NamedTask<WSDL.Admin.resource>(
                    dataSourcePath,
                    tdvClient.GetResourceInfo(dataSourcePath).FirstAsync().AsTask()
                ))
                .ToArray();
            try
            {
                await Task.WhenAll(multiGetResourceInfo.Select(x => x.Task));

                NamedTask<TdvRest_ContainerContents[]>[] multiGetIntrospectedResources = multiGetResourceInfo
                    .Select(gri => new ValueTuple<string, TdvResourceType>(gri.Name, new TdvResourceType(gri.Task.Result.type, gri.Task.Result.subtype)))
                    .Select(x => new NamedTask<TdvRest_ContainerContents[]>(
                        x.Item1,
                        tdvClient.RetrieveContainerContentsRecursive(x.Item1, x.Item2.Type)
                            .ToArrayAsync().AsTask()
                    ))
                    .ToArray();
                await Task.WhenAll(multiGetIntrospectedResources.Select(x => x.Task));

                try
                {
                    await Task.WhenAll(multiGetIntrospectedResources.Select(x => x.Task));

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

        private async Task RunTheIntrospection(TdvWebServiceClient tdvClient, IInfoOutput output, IEnumerable<ValueTuple<string, string, string, TdvResourceType, string>> introspectables, IEnumerable<ValueTuple<string, string, string, TdvResourceType, string>> resourcesToDrop)
        {
            var filteredIntrospectablesByDataSource = introspectables
                .Select(x => new ValueTuple<string, WSDL.Admin.introspectionPlanEntry>(
                    x.Item1,
                    new WSDL.Admin.introspectionPlanEntry()
                    {
                        action = WSDL.Admin.introspectionPlanAction.ADD_OR_UPDATE,
                        resourceId = new WSDL.Admin.pathTypeSubtype()
                        {
                            path = x.Item4.Type switch
                            {
                                TdvResourceTypeEnumAgr.PublishedCatalog => x.Item2,
                                TdvResourceTypeEnumAgr.PublishedSchema => string.Join('/', x.Item2, x.Item3),
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
                                    TdvResourceTypeEnumAgr.PublishedCatalog => x.Item2,
                                    TdvResourceTypeEnumAgr.PublishedSchema => string.Join('/', x.Item2, x.Item3),
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
                )
                .ToArray();

            Dictionary<string, WSDL.Admin.introspectResourcesResultResponse> introspectionProgress = new Dictionary<string, WSDL.Admin.introspectResourcesResultResponse>();

            Internal.IntrospectionProgress? previousProgressState = null;
            // char[] hourglass = { '/', '-', '\\', '|' };
            char[] hourglass = { ' ', '.', 'o', 'O', '\u0002', 'O', 'o', '.' };
            int hourglassState = 0;

            var multiIntrospection = filteredIntrospectablesByDataSource
                .Select(x => tdvClient.PolledServerTask(
                    new API.PolledServerTasks.IntrospectPolledServerTaskHandler(tdvClient, x.Key, x.AsEnumerable()),
                    y =>
                    {
                        if (introspectionProgress.ContainsKey(y.taskId))
                            introspectionProgress[y.taskId] = y;
                        else
                            introspectionProgress.Add(y.taskId, y);

                        Internal.IntrospectionProgress overallProgress = introspectionProgress
                            .Where(x => x.Value != null)
                            .Select(x => x.Value)
                            .Aggregate(
                                seed: new Internal.IntrospectionProgress()
                                {
                                    JobsTotalToBeSpawned = filteredIntrospectablesByDataSource.Length,
                                    JobsSpawned = introspectionProgress.Count
                                },
                                func: (aggregate, element) =>
                                {
                                    aggregate.JobsDone += element.completed ? 1 : 0;
                                    aggregate.Added += element.status.addedCount;
                                    aggregate.ToBeAdded += element.status.toBeAddedCount;
                                    aggregate.Updated += element.status.updatedCount;
                                    aggregate.ToBeUpdated += element.status.toBeUpdatedCount;
                                    aggregate.Removed += element.status.removedCount;
                                    aggregate.ToBeRemoved += element.status.toBeRemovedCount;
                                    aggregate.Skipped += element.status.skippedCount;
                                    aggregate.Warnings += element.status.warningCount;
                                    aggregate.Errors += element.status.errorCount;
                                    return aggregate;
                                }
                            );

                        if (overallProgress.Equals(previousProgressState))
                        {
                            output.InfoNoEoln((hourglassState == 0 ? " " : "\b\b") + hourglass[hourglassState % hourglass.Length] + " ");
                            hourglassState++;
                        }
                        else
                        {
                            output.InfoCR($"{overallProgress.ProgressPct:#####0%} done ("
                                + $"{overallProgress.JobsRunning}/{overallProgress.JobsTotalToBeSpawned}(-{overallProgress.JobsDone}) jobs"
                                + (overallProgress.ToBeAdded > 0 ? $", add:{overallProgress.Added}/{overallProgress.ToBeAdded}" : string.Empty)
                                + (overallProgress.ToBeUpdated > 0 ? $", upd:{overallProgress.Updated}(+{overallProgress.Skipped})/{overallProgress.ToBeUpdated}" : string.Empty)
                                + (overallProgress.ToBeRemoved > 0 ? $", del:{overallProgress.Removed}/{overallProgress.ToBeRemoved}" : string.Empty)
                                + (overallProgress.Warnings > 0 ? $", warn:{overallProgress.Warnings}" : string.Empty)
                                + (overallProgress.Errors > 0 ? $", err:{overallProgress.Errors}" : string.Empty)
                                + ")"
                            );
                            hourglassState = 0;
                            previousProgressState = overallProgress;
                        }
                    }
                ))
                .ToArray();

            try
            {
                await Task.WhenAll(multiIntrospection);
                output.EndCR();
            }
            finally
            {
                foreach (var task in multiIntrospection)
                    task.Dispose();
            }
        }
    }
}
