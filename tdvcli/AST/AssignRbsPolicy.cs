namespace NoP77svk.TibcoDV.CLI.AST
{
    internal record AssignRbsPolicy
        : AssignWhat
    {
        internal string? Policy { get; init; }
    }
}
