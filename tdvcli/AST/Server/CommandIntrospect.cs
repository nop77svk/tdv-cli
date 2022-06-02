namespace NoP77svk.TibcoDV.CLI.AST.Server
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
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

            Task[] multiClearIntrospectableResourceIdCache = uniqueDataSourcePaths
                .Select(dataSourcePath => tdvClient.ClearIntrospectableResourceIdCache(dataSourcePath))
                .ToArray();
            await Task.WhenAll();
            output.Info("... introspectable resource cache cleared");

            Task<WSDL.Admin.linkableResourceId[]>[] multiGetIntrospectableResources = uniqueDataSourcePaths
                .Select(dataSourcePath => new API.PolledServerTasks.GetIntrospectableResourceIdsPolledServerTaskHandler(tdvClient, dataSourcePath, false))
                .Select(taskHandler => tdvClient.PolledServerTaskEnumerable(taskHandler))
                .Select(task => task.ToArrayAsync().AsTask())
                .ToArray();
            await Task.WhenAll(multiGetIntrospectableResources);
            output.Info("... introspectable resource list retrieved");

            throw new NotImplementedException();
        }

        public override string? ToString()
        {
            return $"{base.ToString()}[{DataSources.Count}]";
        }
    }
}
