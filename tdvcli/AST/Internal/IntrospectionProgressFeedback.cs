namespace NoP77svk.TibcoDV.CLI.AST.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using NoP77svk.TibcoDV.CLI.Commons;
    using WSDL = NoP77svk.TibcoDV.API.WSDL;

    internal class IntrospectionProgressFeedback : IDisposable
    {
        private static readonly char[] _hourglass = { ' ', '.', 'o', 'O', '\u0001', '\u0002', 'o', '.' };

        private readonly Dictionary<string, WSDL.Admin.introspectResourcesResultResponse> _introspectionProgress = new Dictionary<string, WSDL.Admin.introspectResourcesResultResponse>();
        private readonly IInfoOutput _output;

        private IntrospectionProgress? _previousProgressState = null;
        private int _hourglassState = 0;
        private int _jobsToBeSpawned;

        public IntrospectionProgressFeedback(IInfoOutput output, int jobsToBeSpawned)
        {
            _output = output;
            _jobsToBeSpawned = jobsToBeSpawned;
        }

        public void Dispose()
        {
            _output.EndCR();
        }

        public void Feedback(WSDL.Admin.introspectResourcesResultResponse response)
        {
            lock (_output)
            {
                if (_introspectionProgress.ContainsKey(response.taskId))
                    _introspectionProgress[response.taskId] = response;
                else
                    _introspectionProgress.Add(response.taskId, response);

                Internal.IntrospectionProgress overallProgress = _introspectionProgress
                    .Where(x => x.Value != null)
                    .Select(x => x.Value)
                    .Aggregate(
                        seed: new Internal.IntrospectionProgress(jobsTotalToBeSpawned: _jobsToBeSpawned, jobsSpawned: _introspectionProgress.Count),
                        func: (aggregate, element) => aggregate.Add(element)
                    );

                if (overallProgress.Equals(_previousProgressState))
                {
                    _output.InfoNoEoln((_hourglassState == 0 ? string.Empty : "\b\b") + _hourglass[_hourglassState % _hourglass.Length] + " ");
                    _hourglassState++;
                }
                else
                {
                    _output.InfoCR($"{overallProgress.ProgressPct:#####0%} done ("
                        + $"{overallProgress.JobsRunning}"
                        + (overallProgress.JobsWaiting > 0 ? $"({overallProgress.JobsWaiting} waiting)" : string.Empty)
                        + $"/{overallProgress.JobsTotalToBeSpawned}({overallProgress.JobsDone} done"
                        + (overallProgress.JobsCancelled > 0 ? $",{overallProgress.JobsCancelled} cancelled" : string.Empty)
                        + (overallProgress.JobsFailed > 0 ? $",{overallProgress.JobsFailed} failed" : string.Empty)
                        + ") jobs"
                        + (overallProgress.ToBeAdded > 0 ? $", add:{overallProgress.Added}/{overallProgress.ToBeAdded}" : string.Empty)
                        + (overallProgress.ToBeUpdated > 0 ? $", upd:{overallProgress.Updated}(+{overallProgress.Skipped})/{overallProgress.ToBeUpdated}" : string.Empty)
                        + (overallProgress.ToBeRemoved > 0 ? $", del:{overallProgress.Removed}/{overallProgress.ToBeRemoved}" : string.Empty)
                        + (overallProgress.Warnings > 0 ? $", warn:{overallProgress.Warnings}" : string.Empty)
                        + (overallProgress.Errors > 0 ? $", err:{overallProgress.Errors}" : string.Empty)
                        + ") "
                    );
                    _previousProgressState = overallProgress;
                    _hourglassState = 0;
                }
            }
        }
    }
}
