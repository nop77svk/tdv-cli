namespace NoP77svk.TibcoDV.API
{
    internal class TdvIntrospectionOptions
    {
        internal bool ScanForNewResourcesToAutoAdd { get; init; } = false;
        internal bool UpdateAllIntrospectedResources { get; init; } = false;
        internal bool RunInBackgroundTransaction { get; init; } = true;
        internal bool FailFast { get; init; } = true;
        internal bool CommitOnFailure { get; init; } = false;
        internal bool AutoRollback { get; init; } = false;
    }
}
