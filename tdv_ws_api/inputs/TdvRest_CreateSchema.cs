namespace NoP77svk.TibcoDV.API
{
    public record TdvRest_CreateSchema
    {
        public string? Path { get; init; }
        public string? Annotation { get; init; }
        public bool IfNotExists { get; init; } = true;
    }
}
