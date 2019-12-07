namespace NoP77svk.TibcoDV.CLI.AST
{
    using NoP77svk.TibcoDV.API.WSDL.Admin;

    internal record ResourceSpecifier
    {
        public resourceType Type { get; init; } = resourceType.NONE;
        public string? Path { get; init; }
    }
}
