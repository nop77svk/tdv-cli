namespace NoP77svk.TibcoDV.CLI.AST
{
    using System.Collections.Generic;

    internal record DropResource
    {
        public bool IfExists { get; init; }
        public bool Recursive { get; init; }
        public IList<ResourceSpecifier>? Resources { get; init; }
    }
}
