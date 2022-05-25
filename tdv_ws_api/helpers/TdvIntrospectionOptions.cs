namespace NoP77svk.TibcoDV.API
{
    public class TdvIntrospectionOptions
    {
        public bool ScanForNewResourcesToAutoAdd { get; init; } = false;
        public bool UpdateAllIntrospectedResources { get; init; } = false;
        public bool RunInBackgroundTransaction { get; init; } = true;
        public bool FailFast { get; init; } = true;
        public bool CommitOnFailure { get; init; } = false;
        public bool AutoRollback { get; init; } = false;
    }
}
