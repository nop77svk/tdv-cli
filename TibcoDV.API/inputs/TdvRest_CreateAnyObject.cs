namespace NoP77svk.TibcoDV.API
{
    public abstract record TdvRest_CreateAnyObject
    {
        public string ParentPath { get; init; } = "/";
        public string? Name { get; init; }
        public bool IfNotExists { get; init; } = true;
        public string? Annotation { get; init; }
    }
}
