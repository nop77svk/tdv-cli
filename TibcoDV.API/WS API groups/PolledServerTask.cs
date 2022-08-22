namespace NoP77svk.TibcoDV.API
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public partial class TdvWebServiceClient
    {
        public async Task PolledServerTask<TResponse>(
            PolledServerTasks.IPolledServerTaskHandler<TResponse> taskHandler,
            Action<TResponse>? responseFeedback = null,
            CancellationToken? cancellationToken = null
        )
        {
            if (taskHandler.PollingInterval.CompareTo(TimeSpan.Zero) < 0)
                throw new ArgumentOutOfRangeException(nameof(taskHandler) + "." + nameof(taskHandler.PollingInterval), taskHandler.PollingInterval.ToString(), "Invalid polling interval");

            int taskId;
            using (Task<int> taskIdTask = taskHandler.StartTaskAsync())
                taskId = await taskIdTask;

            TResponse response;
            while (true)
            {
                using (Task<TResponse> responseTask = taskHandler.PollTaskResultAsync(taskId))
                    response = await responseTask;

                cancellationToken?.ThrowIfCancellationRequested();
                responseFeedback?.Invoke(response);

                taskHandler.HandleResponse(response);

                if (taskHandler.IsFinished(response)) break;
                cancellationToken?.ThrowIfCancellationRequested();

                if (taskHandler.ShouldWaitBeforeAnotherPolling(response))
                {
                    using (Task delayTask = cancellationToken != null
                        ? Task.Delay((int)taskHandler.PollingInterval.TotalMilliseconds, (CancellationToken)cancellationToken)
                        : Task.Delay((int)taskHandler.PollingInterval.TotalMilliseconds)
                    )
                        await delayTask;
                }
            }

            taskHandler.Finalize(response);
        }

        public async IAsyncEnumerable<TResult> PolledServerTaskEnumerable<TResponse, TResult>(
            PolledServerTasks.IPolledServerTaskEnumerableHandler<TResponse, TResult> taskHandler,
            Action<TResponse>? responseFeedback = null,
            CancellationToken? cancellationToken = null
        )
        {
            if (taskHandler.PollingInterval.CompareTo(TimeSpan.Zero) < 0)
                throw new ArgumentOutOfRangeException(nameof(taskHandler) + "." + nameof(taskHandler.PollingInterval), taskHandler.PollingInterval.ToString(), "Invalid polling interval");

            int taskId;
            using (Task<int> taskIdTask = taskHandler.StartTaskAsync())
                taskId = await taskIdTask;

            TResponse response;
            while (true)
            {
                // 2do! could be super-nice if polling results would run in a separate thread, producing data asynchronously for the consuming foreach+yield below
                using (Task<TResponse> responseTask = taskHandler.PollTaskResultAsync(taskId))
                    response = await responseTask;

                cancellationToken?.ThrowIfCancellationRequested();
                responseFeedback?.Invoke(response);

                IEnumerable<TResult>? resultsExtracted = taskHandler.ExtractResults(response);
                if (resultsExtracted != null)
                {
                    foreach (TResult result in resultsExtracted)
                        yield return result;
                }

                if (taskHandler.IsFinished(response)) break;
                cancellationToken?.ThrowIfCancellationRequested();

                if (taskHandler.ShouldWaitBeforeAnotherPolling(response))
                {
                    using (Task delayTask = cancellationToken != null
                        ? Task.Delay((int)taskHandler.PollingInterval.TotalMilliseconds, (CancellationToken)cancellationToken)
                        : Task.Delay((int)taskHandler.PollingInterval.TotalMilliseconds)
                    )
                        await delayTask;
                }
            }

            taskHandler.Finalize(response);
        }
    }
}
