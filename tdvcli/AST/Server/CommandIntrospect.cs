﻿namespace NoP77svk.TibcoDV.CLI.AST.Server
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NoP77svk.Linq;
    using NoP77svk.Text.RegularExpressions;
    using NoP77svk.TibcoDV.API;
    using NoP77svk.TibcoDV.CLI.Commons;
    using WSDL = NoP77svk.TibcoDV.API.WSDL;

    internal class CommandIntrospect : IAsyncExecutable
    {
        public IList<IntrospectTargetDataSource> DataSources { get; }

        public CommandIntrospect(IList<IntrospectTargetDataSource> dataSources)
        {
            DataSources = dataSources;
        }

        public async Task Execute(TdvWebServiceClient tdvClient, IInfoOutput output, ParserState parserState)
        {
            string[] uniqueDataSourcePaths = DataSources
                .Select(x => x.DataSourcePath)
                .Distinct()
                .ToArray();

            output.Info($"Introspecting {uniqueDataSourcePaths.Length} data sources...");

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
            }
            finally
            {
                foreach (var task in multiGetIntrospectableResources)
                    task.Dispose();
            }

            await RunTheIntrospection(tdvClient, output, multiGetIntrospectableResources);
            output.Info("Introspection done");
        }

        public override string? ToString()
        {
            return $"{base.ToString()}[{DataSources.Count}]";
        }

        private static IEnumerable<ValueTuple<string, string, string, TdvResourceType, string>> FilterIntrospectablesByInput(IEnumerable<NamedTask<WSDL.Admin.linkableResourceId[]>> multiGetIntrospectableResources, IEnumerable<Server.IntrospectTargetDataSource> commandInput)
        {
            Internal.IntrospectableDataSource[] multiGetIntrospectableResourcesGrouped = multiGetIntrospectableResources
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
                .Where(x => !string.IsNullOrEmpty(x.Item5) && !string.IsNullOrEmpty(x.Item3) && !string.IsNullOrEmpty(x.Item2) && !string.IsNullOrEmpty(x.Item1))
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

            IEnumerable<Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableDataSource, IntrospectTargetDataSource>> dataSourcesToIntrospect = IdentifyDataSourcesToIntrospect(commandInput, multiGetIntrospectableResourcesGrouped);
            foreach (var dataSourceJoin in dataSourcesToIntrospect)
            {
                IEnumerable<Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableCatalog, IntrospectTargetCatalog>> catalogsToIntrospect = IdentifyCatalogsToIntrospect(dataSourceJoin);
                foreach (var catalogJoin in catalogsToIntrospect)
                {
                    IEnumerable<Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableSchema, IntrospectTargetSchema>> schemasToIntrospect = IdentifySchemasToIntrospect(catalogJoin);
                    foreach (var schemaJoin in schemasToIntrospect)
                    {
                        IEnumerable<Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableObject, IntrospectTargetTable>> objectsToIntrospect = IdentifyObjectsToIntrospect(schemaJoin);
                        foreach (var objectJoin in objectsToIntrospect)
                        {
                            yield return new ValueTuple<string, string, string, TdvResourceType, string>(dataSourceJoin.Introspectable.DataSource, catalogJoin.Introspectable.CatalogName, schemaJoin.Introspectable.SchemaName, objectJoin.Introspectable.ObjectType, objectJoin.Introspectable.ObjectName);
                        }
                    }
                }
            }
        }

        private static IEnumerable<Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableCatalog, IntrospectTargetCatalog>> IdentifyCatalogsToIntrospect(Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableDataSource, IntrospectTargetDataSource> dataSourceJoin)
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

        private static IEnumerable<Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableDataSource, IntrospectTargetDataSource>> IdentifyDataSourcesToIntrospect(IEnumerable<IntrospectTargetDataSource> dataSources, IEnumerable<Internal.IntrospectableDataSource> multiGetIntrospectableResourcesGrouped)
        {
            return multiGetIntrospectableResourcesGrouped
                .Join(
                    inner: dataSources,
                    outerKeySelector: x => x.DataSource,
                    innerKeySelector: x => x.DataSourcePath,
                    resultSelector: (outer, inner) => new Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableDataSource, IntrospectTargetDataSource>(outer, inner)
                );
        }

        private static IEnumerable<Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableObject, IntrospectTargetTable>> IdentifyObjectsToIntrospect(Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableSchema, IntrospectTargetSchema> schemaJoin)
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

        private static IEnumerable<Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableSchema, IntrospectTargetSchema>> IdentifySchemasToIntrospect(Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableCatalog, IntrospectTargetCatalog> catalogJoin)
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

        private async Task RunTheIntrospection(TdvWebServiceClient tdvClient, IInfoOutput output, NamedTask<WSDL.Admin.linkableResourceId[]>[] multiGetIntrospectableResources)
        {
            var filteredIntrospectablesByDataSource = FilterIntrospectablesByInput(multiGetIntrospectableResources, DataSources)
                .Select(x => new ValueTuple<string, WSDL.Admin.introspectionPlanEntry>(
                    x.Item1,
                    new WSDL.Admin.introspectionPlanEntry()
                    {
                        action = WSDL.Admin.introspectionPlanAction.ADD_OR_UPDATE,
                        resourceId = new WSDL.Admin.pathTypeSubtype()
                        {
                            path = $"{x.Item2}/{x.Item3}/{x.Item5}",
                            type = x.Item4.WsType,
                            subtype = x.Item4.WsSubType
                        }
                    }
                ))
                .GroupBy(
                    keySelector: x => x.Item1,
                    elementSelector: x => x.Item2
                )
                .ToArray();

            Dictionary<string, WSDL.Admin.introspectResourcesResultResponse> introspectionProgress = new Dictionary<string, WSDL.Admin.introspectResourcesResultResponse>();

            Internal.IntrospectionProgress? previousProgressState = null;

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
                            output.InfoNoEoln(". \b");
                        }
                        else
                        {
                            output.InfoCR($"{overallProgress.ProgressPct:#####0%} done ("
                                + $"{overallProgress.JobsSpawned - overallProgress.JobsDone}/{overallProgress.JobsTotalToBeSpawned} jobs"
                                + (overallProgress.ToBeAdded > 0 ? $", add:{overallProgress.Added}/{overallProgress.ToBeAdded}" : string.Empty)
                                + (overallProgress.ToBeUpdated > 0 ? $", upd:{overallProgress.Updated}(+{overallProgress.Skipped})/{overallProgress.ToBeUpdated}" : string.Empty)
                                + (overallProgress.ToBeRemoved > 0 ? $", del:{overallProgress.Removed}/{overallProgress.ToBeRemoved}" : string.Empty)
                                + (overallProgress.Warnings > 0 ? $", warn:{overallProgress.Warnings}" : string.Empty)
                                + (overallProgress.Errors > 0 ? $", err:{overallProgress.Errors}" : string.Empty)
                                + ")"
                            );
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
