namespace NoP77svk.TibcoDV.CLI.AST
{
    using System.Collections.Generic;

    internal class CommandDescribe
    {
        internal IList<AST.ResourceSpecifier> Resources { get; }

        public CommandDescribe(IList<ResourceSpecifier> resources)
        {
            Resources = resources;
        }
    }
}
