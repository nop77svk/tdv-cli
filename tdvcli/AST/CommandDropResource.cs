namespace NoP77svk.TibcoDV.CLI.AST
{
    using System.Collections.Generic;

    internal record CommandDropResource
    {
        public bool IfExists { get; init; }
        public IList<ResourceSpecifier>? Resources { get; init; }
        public bool AlsoDropRootResource { get; init; } = true;
    }
}
