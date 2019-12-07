namespace NoP77svk.TibcoDV.API
{
    public record TdvRest_CreateDataView
        : TdvRest_CreateAnyObject
    {
        public string SQL { get; init; } = string.Empty;
    }
}
