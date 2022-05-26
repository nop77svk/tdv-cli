namespace NoP77svk.TibcoDV.API.PolledServerTasks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public interface IPolledServerTaskHandler<TResponse>
    {
        TimeSpan PollingInterval { get; set; }
        Task<int> StartTaskAsync();
        Task<TResponse> PollTaskResultAsync(int taskId);
        void HandleResponse(TResponse response);
        bool IsFinished(TResponse response);
        bool ShouldWaitBeforeAnotherPolling(TResponse response);
        void Finalize(TResponse response);
    }
}
