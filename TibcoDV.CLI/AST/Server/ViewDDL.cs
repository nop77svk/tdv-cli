namespace NoP77svk.TibcoDV.CLI.AST.Server
{
    internal record ViewDDL
    {
        public string? ResourcePath { get; init; }
        public string? ViewQuery { get; init; }
    }
}
