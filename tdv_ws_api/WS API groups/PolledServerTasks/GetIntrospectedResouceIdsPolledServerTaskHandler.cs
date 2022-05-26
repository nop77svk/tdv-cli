namespace NoP77svk.TibcoDV.API.PolledServerTasks
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using WSDL = NoP77svk.TibcoDV.API.WSDL;

    public class GetIntrospectedResouceIdsPolledServerTaskHandler
        : IPolledServerTaskEnumerableHandler<WSDL.Admin.getIntrospectedResourceIdsResultResponse, WSDL.Admin.pathTypePair>
    {
        public GetIntrospectedResouceIdsPolledServerTaskHandler(TdvWebServiceClient tdvClient, string dataSourcePath)
        {
            TdvClient = tdvClient;
            DataSourcePath = dataSourcePath;
        }

        public TdvWebServiceClient TdvClient { get; }
        public TimeSpan PollingInterval { get; set; } = TimeSpan.FromMilliseconds(500)
        public string DataSourcePath { get; }

        public IEnumerable<WSDL.Admin.pathTypePair> ExtractResults(WSDL.Admin.getIntrospectedResourceIdsResultResponse response)
        {
            return response.resourceIdentifiers;
        }

        public void Finalize(WSDL.Admin.getIntrospectedResourceIdsResultResponse response)
        {
        }

        public void HandleResponse(WSDL.Admin.getIntrospectedResourceIdsResultResponse response)
        {
        }

        public bool IsFinished(WSDL.Admin.getIntrospectedResourceIdsResultResponse response)
        {
            return response.completed;
        }

        public async Task<WSDL.Admin.getIntrospectedResourceIdsResultResponse> PollTaskResultAsync(int taskId)
        {
            return await TdvClient.GetIntrospectedResourceIdsResult(taskId);
        }

        public bool ShouldWaitBeforeAnotherPolling(WSDL.Admin.getIntrospectedResourceIdsResultResponse response)
        {
            return PollingInterval.CompareTo(TimeSpan.Zero) > 0 && response.resourceIdentifiers.Length <= 0;
        }

        public async Task<int> StartTaskAsync()
        {
            return await TdvClient.GetIntrospectedResourceIdsTask(DataSourcePath);
        }
    }
}
