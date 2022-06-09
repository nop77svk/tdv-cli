#pragma warning disable SA1401
namespace NoP77svk.TibcoDV.CLI.AST.Internal
{
    internal class IntrospectionProgress
    {
        internal int JobsRunning = 0;
        internal int JobsDone = 0;
        internal int JobsTotal = 0;
        internal int Added = 0;
        internal int ToBeAdded = 0;
        internal int Updated = 0;
        internal int ToBeUpdated = 0;
        internal int Removed = 0;
        internal int ToBeRemoved = 0;
        internal int Skipped = 0;
        internal int Warnings = 0;
        internal int Errors = 0;

        internal int ObjectsProcessed { get => Added + Updated + Removed + Skipped + Warnings + Errors + JobsDone; }
        internal int ObjectsTotal { get => ToBeAdded + ToBeUpdated + ToBeRemoved + JobsTotal; }
        internal float ProgressPct { get => ObjectsTotal > 0 && JobsRunning == JobsTotal ? (float)ObjectsProcessed / ObjectsTotal : 0.0f; }
    }
}
