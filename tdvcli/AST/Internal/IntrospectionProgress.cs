#pragma warning disable SA1401
namespace NoP77svk.TibcoDV.CLI.AST.Internal
{
    using System;

    internal class IntrospectionProgress : IEquatable<IntrospectionProgress>
    {
        internal int JobsSpawned = 0;
        internal int JobsDone = 0;
        internal int JobsTotalToBeSpawned = 0;
        internal int Added = 0;
        internal int ToBeAdded = 0;
        internal int Updated = 0;
        internal int ToBeUpdated = 0;
        internal int Removed = 0;
        internal int ToBeRemoved = 0;
        internal int Skipped = 0;
        internal int Warnings = 0;
        internal int Errors = 0;

        internal int JobsRunning { get => JobsSpawned - JobsDone; }
        internal int ObjectsProcessed { get => Added + Updated + Removed + Skipped + Errors + JobsDone; }
        internal int ObjectsTotal { get => ToBeAdded + ToBeUpdated + ToBeRemoved + JobsTotalToBeSpawned; }
        internal float ProgressPct { get => ObjectsTotal > 0 && JobsSpawned == JobsTotalToBeSpawned ? (float)ObjectsProcessed / ObjectsTotal : 0.0f; }

        public bool Equals(IntrospectionProgress? other)
        {
            return JobsSpawned == other?.JobsSpawned
                && JobsDone == other?.JobsDone
                && JobsTotalToBeSpawned == other?.JobsTotalToBeSpawned
                && Added == other?.Added
                && ToBeAdded == other?.ToBeAdded
                && Updated == other?.Updated
                && ToBeUpdated == other?.ToBeUpdated
                && Removed == other?.Removed
                && ToBeRemoved == other?.ToBeRemoved
                && Skipped == other?.Skipped
                && Warnings == other?.Warnings
                && Errors == other?.Errors
            ;
        }
    }
}
