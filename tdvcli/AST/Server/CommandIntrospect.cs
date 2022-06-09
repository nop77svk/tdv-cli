namespace NoP77svk.TibcoDV.CLI.AST.Server
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using NoP77svk.IO;
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
            output.Info($"Introspecting {DataSources.Count} data sources...");

            string[] uniqueDataSourcePaths = DataSources
                .Select(x => x.DataSourcePath)
                .Distinct()
                .ToArray();

            ValueTuple<string, Task<WSDL.Admin.linkableResourceId[]>>[] multiGetIntrospectableResources = uniqueDataSourcePaths
                .Select(dataSourcePath => new ValueTuple<string, Task<WSDL.Admin.linkableResourceId[]>>(
                    dataSourcePath,
                    tdvClient.PolledServerTaskEnumerable(new API.PolledServerTasks.GetIntrospectableResourceIdsPolledServerTaskHandler(tdvClient, dataSourcePath, true))
                        .ToArrayAsync().AsTask()
                ))
                .ToArray();
            await Task.WhenAll(multiGetIntrospectableResources.Select(x => x.Item2));
            output.Info("... introspectable resource list retrieved");

            FilterIntrospectablesByInput(multiGetIntrospectableResources);

            throw new NotImplementedException();
        }
        public override string? ToString()
        {
            return $"{base.ToString()}[{DataSources.Count}]";
        }


        private void FilterIntrospectablesByInput(IEnumerable<ValueTuple<string, Task<WSDL.Admin.linkableResourceId[]>>> multiGetIntrospectableResources)
        {
            Internal.IntrospectableDataSource[] multiGetIntrospectableResourcesGrouped = multiGetIntrospectableResources
                .Unnest(
                    retrieveNestedCollection: x => x.Item2.Result,
                    resultSelector: (outer, inner) => new ValueTuple<string, WSDL.Admin.linkableResourceId>(outer.Item1, inner)
                )
                .Select(x =>
                {
                    var splitPath = x.Item2.resourceId.path.Split('/', 3);
                    return new ValueTuple<string, string, string, string, TdvResourceType>(
                        x.Item1,
                        splitPath.Length >= 1 ? splitPath[0] : string.Empty,
                        splitPath.Length >= 2 ? splitPath[1] : string.Empty,
                        splitPath.Length >= 3 ? splitPath[2] : string.Empty,
                        new TdvResourceType(x.Item2.resourceId.type, x.Item2.resourceId.subtype)
                    );
                })
                .GroupBy(
                    keySelector: x => new ValueTuple<string, string, string>(x.Item1, x.Item2, x.Item3),
                    elementSelector: x => new Internal.IntrospectableObject(x.Item5, x.Item4)
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

            IEnumerable<Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableDataSource, IntrospectTargetDataSource>> dataSourcesToIntrospect = IdentifyDataSourcesToIntrospect(multiGetIntrospectableResourcesGrouped);
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

        private IEnumerable<Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableDataSource, IntrospectTargetDataSource>> IdentifyDataSourcesToIntrospect(IEnumerable<Internal.IntrospectableDataSource> multiGetIntrospectableResourcesGrouped)
        {
            return multiGetIntrospectableResourcesGrouped
                .Join(
                    inner: DataSources,
                    outerKeySelector: x => x.DataSource,
                    innerKeySelector: x => x.DataSourcePath,
                    resultSelector: (outer, inner) => new Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableDataSource, IntrospectTargetDataSource>(outer, inner)
                );
        }

        private IEnumerable<Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableObject, IntrospectTargetTable>> IdentifyObjectsToIntrospect(Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableSchema, IntrospectTargetSchema> schemaJoin)
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

        private IEnumerable<Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableSchema, IntrospectTargetSchema>> IdentifySchemasToIntrospect(Internal.IntrospectionInputsJoinMatch<Internal.IntrospectableCatalog, IntrospectTargetCatalog> catalogJoin)
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
    }
}
