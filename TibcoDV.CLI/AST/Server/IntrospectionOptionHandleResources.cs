#pragma warning disable SA1313
namespace NoP77svk.TibcoDV.CLI.AST.Server
{
    internal record IntrospectionOptionHandleResources
    {
        internal bool UpdateExisting { get; init; } = false;
        internal bool DropUnmatched { get; init; } = false;
    }
}
