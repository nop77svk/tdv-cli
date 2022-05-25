namespace NoP77svk.TibcoDV.API
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
        Task<TResponse> PollTaskResultAsync();
        void HandleResponse(TResponse response);
        bool IsFinished();
        bool ShouldWaitBeforeAnotherPolling();
        void Finalize(TResponse response);
    }
}
