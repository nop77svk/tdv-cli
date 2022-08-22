namespace NoP77svk.TibcoDV.API
{
    using System.Text.Json.Serialization;

    public record TdvRest_CreateLink
    {
        [JsonPropertyName("path")]
        public string? PublishedLinkPath { get; init; }

        public bool IsTable { get; init; } = true;

        [JsonPropertyName("targetPath")]
        public string? SourceObjectPath { get; init; }

        public string? Annotation { get; init; }

        public bool IfNotExists { get; init; } = false;
    }
}
