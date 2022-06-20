#pragma warning disable SA1401
namespace NoP77svk.TibcoDV.CLI.AST.Internal
{
    using System;
    using WSDL = NoP77svk.TibcoDV.API.WSDL;

    internal class IntrospectionProgress : IEquatable<IntrospectionProgress>
    {
        internal int JobsTotalToBeSpawned { get; }
        internal int JobsSpawned { get; }
        internal int JobsDone { get; private set; } = 0;
        internal int JobsWaiting { get; private set; } = 0;
        internal int JobsFailed { get; private set; } = 0;
        internal int JobsCancelled { get; private set; } = 0;
        internal int Added { get; private set; } = 0;
        internal int ToBeAdded { get; private set; } = 0;
        internal int Updated { get; private set; } = 0;
        internal int ToBeUpdated { get; private set; } = 0;
        internal int Removed { get; private set; } = 0;
        internal int ToBeRemoved { get; private set; } = 0;
        internal int Skipped { get; private set; } = 0;
        internal int Warnings { get; private set; } = 0;
        internal int Errors { get; private set; } = 0;

        public IntrospectionProgress(int jobsTotalToBeSpawned, int jobsSpawned)
        {
            JobsTotalToBeSpawned = jobsTotalToBeSpawned;
            JobsSpawned = jobsSpawned;
        }

        internal int JobsRunning { get => JobsSpawned - JobsDone; }
        internal int ObjectsProcessed { get => Added + Updated + Removed + Skipped + Errors + JobsDone; }
        internal int ObjectsTotal { get => ToBeAdded + ToBeUpdated + ToBeRemoved + JobsTotalToBeSpawned; }
        internal float ProgressPct { get => ObjectsTotal > 0 && JobsWaiting <= 0 && JobsSpawned == JobsTotalToBeSpawned ? (float)ObjectsProcessed / ObjectsTotal : 0.0f; }

        public bool Equals(IntrospectionProgress? other)
        {
            return JobsSpawned == other?.JobsSpawned
                && JobsDone == other?.JobsDone
                && JobsWaiting == other?.JobsWaiting
                && JobsFailed == other?.JobsFailed
                && JobsCancelled == other?.JobsCancelled
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

        internal IntrospectionProgress Add(WSDL.Admin.introspectResourcesResultResponse wsResponse)
        {
            JobsDone += wsResponse.completed ? 1 : 0;
            JobsWaiting += wsResponse.status.status == WSDL.Admin.operationStatus.WAITING ? 1 : 0;
            JobsFailed += wsResponse.status.status == WSDL.Admin.operationStatus.FAIL ? 1 : 0;
            JobsCancelled += wsResponse.status.status == WSDL.Admin.operationStatus.CANCELED ? 1 : 0;
            Added += wsResponse.status.addedCount;
            ToBeAdded += wsResponse.status.toBeAddedCount;
            Updated += wsResponse.status.updatedCount;
            ToBeUpdated += wsResponse.status.toBeUpdatedCount;
            Removed += wsResponse.status.removedCount;
            ToBeRemoved += wsResponse.status.toBeRemovedCount;
            Skipped += wsResponse.status.skippedCount;
            Warnings += wsResponse.status.warningCount;
            Errors += wsResponse.status.errorCount;
            return this;
        }
    }
}
