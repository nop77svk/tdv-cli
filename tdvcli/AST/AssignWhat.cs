namespace NoP77svk.TibcoDV.CLI.AST
{
    using System.Collections.Generic;

    internal record AssignWhat
    {
        internal IList<ResourceSpecifier>? Resources { get; init; }
    }
}