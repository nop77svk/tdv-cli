namespace NoP77svk.TibcoDV.CLI.AST
{
    internal record CommandCreateResource
    {
        public bool IfNotExists { get; init; } = false;
        public object? ResourceDDL { get; init; }
    }
}
