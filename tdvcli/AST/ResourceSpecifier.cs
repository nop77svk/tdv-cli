namespace NoP77svk.TibcoDV.CLI.AST
{
    using WSDL = NoP77svk.TibcoDV.API.WSDL.Admin;

    internal record ResourceSpecifier
    {
        public WSDL.resourceType Type { get; init; } = WSDL.resourceType.NONE;
        public string? Path { get; init; }
    }
}
