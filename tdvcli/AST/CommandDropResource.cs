namespace NoP77svk.TibcoDV.CLI.AST
{
    using System.Collections.Generic;

    internal class CommandDropResource
    {
        internal bool IfExists { get; }
        internal IList<ResourceSpecifier> Resources { get; }
        internal bool AlsoDropRootResource { get; }

        internal CommandDropResource(bool ifExists, IList<ResourceSpecifier> resources, bool alsoDropRootResource)
        {
            IfExists = ifExists;
            Resources = resources;
            AlsoDropRootResource = alsoDropRootResource;
        }
    }
}
