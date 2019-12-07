namespace NoP77svk.TibcoDV.CLI.AST
{
    using System.Collections.Generic;

    internal record Describe
    {
        internal IList<AST.ResourceSpecifier>? Resources { get; init; }
    }
}
