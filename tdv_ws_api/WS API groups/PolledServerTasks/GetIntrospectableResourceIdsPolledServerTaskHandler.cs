namespace NoP77svk.TibcoDV.API.PolledServerTasks
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using WSDL = NoP77svk.TibcoDV.API.WSDL;

    public class GetIntrospectableResourceIdsPolledServerTaskHandler
        : IPolledServerTaskEnumerableHandler<WSDL.Admin.getIntrospectableResourceIdsResultResponse, WSDL.Admin.linkableResourceId>
    {
        public GetIntrospectableResourceIdsPolledServerTaskHandler(TdvWebServiceClient tdvClient, string dataSourcePath, bool clearCachePriorToRefresh = true)
        {
            TdvClient = tdvClient;
            DataSourcePath = dataSourcePath;
            ClearCachePriorToRefresh = clearCachePriorToRefresh;
        }

        public TdvWebServiceClient TdvClient { get; }
        public TimeSpan PollingInterval { get; set; } = TimeSpan.FromMilliseconds(500);
        public string DataSourcePath { get; }
        public bool ClearCachePriorToRefresh { get; }

        public IEnumerable<WSDL.Admin.linkableResourceId> ExtractResults(WSDL.Admin.getIntrospectableResourceIdsResultResponse response)
        {
            return response.resourceIdentifiers;
        }

        public void Finalize(WSDL.Admin.getIntrospectableResourceIdsResultResponse response)
        {
        }

        public void HandleResponse(WSDL.Admin.getIntrospectableResourceIdsResultResponse response)
        {
        }

        public bool IsFinished(WSDL.Admin.getIntrospectableResourceIdsResultResponse response)
        {
            return response.completed;
        }

        public async Task<WSDL.Admin.getIntrospectableResourceIdsResultResponse> PollTaskResultAsync(int taskId)
        {
            if (ClearCachePriorToRefresh)
                await TdvClient.ClearIntrospectableResourceIdCache(DataSourcePath);

            return await TdvClient.GetIntrospectableResourceIdsResult(taskId);
        }

        public bool ShouldWaitBeforeAnotherPolling(WSDL.Admin.getIntrospectableResourceIdsResultResponse response)
        {
            return PollingInterval.CompareTo(TimeSpan.Zero) > 0 && response.resourceIdentifiers.Length <= 0;
        }

        public async Task<int> StartTaskAsync()
        {
            return await TdvClient.GetIntrospectableResourceIdsTask(DataSourcePath);
        }
    }
}
