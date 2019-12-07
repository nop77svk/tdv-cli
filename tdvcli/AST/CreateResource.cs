namespace NoP77svk.TibcoDV.CLI.AST
{
    internal record CreateResource
    {
        public bool IfNotExists { get; init; } = false;
        public object? ResourceDDL { get; init; }
    }
}
