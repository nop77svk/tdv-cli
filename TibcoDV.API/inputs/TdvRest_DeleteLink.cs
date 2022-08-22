namespace NoP77svk.TibcoDV.API
{
    public record TdvRest_DeleteLink
    {
        public string? Path { get; init; }

        public bool IsTable { get; init; } = true;
    }
}
