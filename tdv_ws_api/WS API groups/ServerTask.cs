namespace NoP77svk.TibcoDV.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public partial class TdvWebServiceClient
    {
        public async Task PolledServerTask<TResponse>(
            IPolledServerTaskHandler<TResponse> taskHandler,
            Action<TResponse>? responseFeedback = null,
            CancellationToken? cancellationToken = null
        )
        {
            if (taskHandler.PollingInterval.CompareTo(TimeSpan.Zero) < 0)
                throw new ArgumentOutOfRangeException(nameof(taskHandler) + "." + nameof(taskHandler.PollingInterval), taskHandler.PollingInterval.ToString(), "Invalid polling interval");

            int taskId = await taskHandler.StartTaskAsync();

            TResponse response;
            while (true)
            {
                response = await taskHandler.PollTaskResultAsync(taskId);

                cancellationToken?.ThrowIfCancellationRequested();
                responseFeedback?.Invoke(response);

                taskHandler.HandleResponse(response);

                if (taskHandler.IsFinished(response)) break;
                cancellationToken?.ThrowIfCancellationRequested();

                if (taskHandler.ShouldWaitBeforeAnotherPolling(response))
                {
                    if (cancellationToken != null)
                        await Task.Delay((int)taskHandler.PollingInterval.TotalMilliseconds, (CancellationToken)cancellationToken);
                    else
                        await Task.Delay((int)taskHandler.PollingInterval.TotalMilliseconds);
                }
            }

            taskHandler.Finalize(response);
        }

        public async IAsyncEnumerable<TResult> PolledServerTaskEnumerable<TResponse, TResult>(
            IPolledServerTaskEnumerableHandler<TResponse, TResult> taskHandler,
            Action<TResponse>? responseFeedback = null,
            CancellationToken? cancellationToken = null
        )
        {
            if (taskHandler.PollingInterval.CompareTo(TimeSpan.Zero) < 0)
                throw new ArgumentOutOfRangeException(nameof(taskHandler) + "." + nameof(taskHandler.PollingInterval), taskHandler.PollingInterval.ToString(), "Invalid polling interval");

            int taskId = await taskHandler.StartTaskAsync();

            TResponse response;
            while (true)
            {
                response = await taskHandler.PollTaskResultAsync(taskId);

                cancellationToken?.ThrowIfCancellationRequested();
                responseFeedback?.Invoke(response);

                foreach (TResult result in taskHandler.ExtractResults(response))
                    yield return result;

                if (taskHandler.IsFinished(response)) break;
                cancellationToken?.ThrowIfCancellationRequested();

                if (taskHandler.ShouldWaitBeforeAnotherPolling(response))
                {
                    if (cancellationToken != null)
                        await Task.Delay((int)taskHandler.PollingInterval.TotalMilliseconds, (CancellationToken)cancellationToken);
                    else
                        await Task.Delay((int)taskHandler.PollingInterval.TotalMilliseconds);
                }
            }

            taskHandler.Finalize(response);
        }
    }
}
