namespace NoP77svk.TibcoDV.API
{
    public record TdvRest_CreateCatalog
    {
        public string? Path { get; init; }
        public string? Annotation { get; init; }
        public string? NewPath { get; init; }
        public bool IfNotExists { get; init; } = true;
    }
}
