namespace NoP77svk.TibcoDV.CLI.AST
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using log4net;
    using NoP77svk.Linq;
    using NoP77svk.TibcoDV.API;
    using NoP77svk.TibcoDV.API.WSDL.Admin;
    using NoP77svk.TibcoDV.CLI.Commons;
    using NoP77svk.TibcoDV.Commons;
    using WSDL = NoP77svk.TibcoDV.API.WSDL.Admin;

    internal class CommandDropResource : IAsyncStatement
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Program));

        internal bool IfExists { get; }
        internal IList<ResourceSpecifier> Resources { get; }
        internal bool AlsoDropRootResource { get; }

        internal CommandDropResource(bool ifExists, IList<ResourceSpecifier> resources, bool alsoDropRootResource)
        {
            IfExists = ifExists;
            Resources = resources;
            AlsoDropRootResource = alsoDropRootResource;
        }

        public async Task Execute(TdvWebServiceClient tdvClient, IInfoOutput output)
        {
            using var log = new TraceLog(_log, nameof(Execute));

            IEnumerable<ResourceSpecifier> nonemptyResourceSpecifiers = Resources
                .Where(resource => !string.IsNullOrWhiteSpace(resource.Path));

            if (AlsoDropRootResource)
            {
                IEnumerable<TdvResourceSpecifier> resources = nonemptyResourceSpecifiers
                    .Select(resource => new TdvResourceSpecifier(resource.Path ?? string.Empty, new TdvResourceType(resource.Type.ToString(), null)));

                await tdvClient.DropAnyResources(resources, IfExists);
            }
            else
            {
                IEnumerable<Task> purgeTasks = nonemptyResourceSpecifiers
                    .Select(resource => tdvClient.PurgeContainer(resource.Path, IfExists));

                await Task.WhenAll(purgeTasks);
            }

            output.Info(nonemptyResourceSpecifiers.Count().ToString() + " resource(s) "
                + (AlsoDropRootResource ? "dropped" : "purged")
                + " OK"
            );
        }
    }
}
