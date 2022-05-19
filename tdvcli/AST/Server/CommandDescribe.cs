namespace NoP77svk.TibcoDV.CLI.AST.Server
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using log4net;
    using NoP77svk.TibcoDV.API;
    using NoP77svk.TibcoDV.CLI.Commons;
    using NoP77svk.TibcoDV.Commons;
    using WSDL = NoP77svk.TibcoDV.API.WSDL.Admin;

    internal class CommandDescribe : IAsyncExecutable
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Program));

        internal IList<ResourceSpecifier> Resources { get; }

        public CommandDescribe(IList<ResourceSpecifier> resources)
        {
            Resources = resources;
        }

        public async Task Execute(TdvWebServiceClient tdvClient, IInfoOutput output)
        {
            using var log = new TraceLog(_log, nameof(Execute));

            IEnumerable<IAsyncEnumerable<WSDL.resource>> getResourceInfoTasks = Resources
                .Select(res => tdvClient.GetResourceInfo(res.Path, res.Type))
                .ToList();

            foreach (IAsyncEnumerable<WSDL.resource> resources in getResourceInfoTasks)
            {
                await foreach (WSDL.resource res in resources)
                {
                    output.Info($"resource: {res.path}\n\ttype: {res.type}\n\tsubtype: {res.subtype}\n\towner: {res.ownerName}@{res.ownerDomain}\n\tversion: {res.version}\n\tannotation: {res.annotation}");
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
    }
}
