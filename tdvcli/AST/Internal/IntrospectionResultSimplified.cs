#pragma warning disable SA1313
namespace NoP77svk.TibcoDV.CLI.AST.Internal
{
    using WSDL = NoP77svk.TibcoDV.API.WSDL;

    internal record IntrospectionResultSimplified(WSDL.Admin.introspectionAction Action, API.TdvResourceType ResourceType, string Path, WSDL.Admin.messageSeverity MessageSeverity)
    {
        internal int Warnings { get; init; } = 0;
        internal int Errors { get; init; } = 0;
        internal bool HasAddedColumns { get; init; } = false;
        internal bool HasDeletedColumns { get; init; } = false;
        internal bool HasFailedIntrospectables
        {
            get => ResourceType.WsType == WSDL.Admin.resourceType.TABLE
                && ((Action is WSDL.Admin.introspectionAction.ADD && !HasAddedColumns)
                    || (Action is WSDL.Admin.introspectionAction.UPDATE && !HasAddedColumns && HasDeletedColumns));
        }
    }
}
