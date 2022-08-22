namespace NoP77svk.TibcoDV.API
{
    using System.Text.Json.Serialization;

    public record TdvRest_ContainerContents
    {
        public string? Name { get; set; }
        public string? Path { get; set; }
        public string? Type { get; set; }
        [JsonPropertyName("subtype")]
        public string? SubType { get; set; }
        public string? TargetType { get; set; }
        public object? ImpactMessage { get; set; }
        public int? ChildCount { get; set; }

        public TdvResourceTypeEnumAgr TdvResourceType => TdvWebServiceClient.CalcResourceType(this);
    }
}
