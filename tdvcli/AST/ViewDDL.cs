namespace NoP77svk.TibcoDV.CLI.AST
{
    internal record ViewDDL
    {
        public string? ResourcePath { get; init; }
        public string? ViewQuery { get; init; }
    }
}
