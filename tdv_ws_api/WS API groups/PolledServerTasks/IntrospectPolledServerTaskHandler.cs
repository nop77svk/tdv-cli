namespace NoP77svk.TibcoDV.API.PolledServerTasks
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using WSDL = NoP77svk.TibcoDV.API.WSDL;

    public class IntrospectPolledServerTaskHandler
        : IPolledServerTaskHandler<WSDL.Admin.introspectResourcesResultResponse>
    {
        public IntrospectPolledServerTaskHandler(TdvWebServiceClient tdvClient, string dataSourcePath, IEnumerable<WSDL.Admin.introspectionPlanEntry> resources)
        {
            TdvClient = tdvClient;
            DataSourcePath = dataSourcePath;
            Resources = resources;
        }

        public TdvWebServiceClient TdvClient { get; }
        public TimeSpan PollingInterval { get; set; } = TimeSpan.FromMilliseconds(500);
        public string DataSourcePath { get; }
        public IEnumerable<WSDL.Admin.introspectionPlanEntry> Resources { get; }
        public TdvIntrospectionOptions IntrospectionOptions { get; init; } = new TdvIntrospectionOptions();

        internal bool RetrieveResultInBlockingFashion { get => PollingInterval.CompareTo(TimeSpan.Zero) <= 0 || !IntrospectionOptions.RunInBackgroundTransaction; }

        private int _taskId;

        public void Finalize(WSDL.Admin.introspectResourcesResultResponse response)
        {
            if (response.completed)
            {
                switch (response.status.status)
                {
                    case WSDL.Admin.operationStatus.SUCCESS: break;
                    case WSDL.Admin.operationStatus.CANCELED: throw new ETdvIntrospectionCancelled(DataSourcePath, _taskId);
                    case WSDL.Admin.operationStatus.WAITING: throw new ETdvIntrospectionPrematureEnd(DataSourcePath, _taskId);
                    case WSDL.Admin.operationStatus.INCOMPLETE: throw new ETdvIntrospectionIncomplete(DataSourcePath, _taskId);
                    case WSDL.Admin.operationStatus.FAIL: throw new ETdvIntrospectionFailed(DataSourcePath, _taskId);
                    default: throw new ETdvIntrospectionError(DataSourcePath, _taskId, $"Unknown status (\"{response.status.status}\" upon completion");
                }
            }
            else
            {
                throw new ETdvIntrospectionPrematureEnd(DataSourcePath, _taskId);
            }
        }

        public void HandleResponse(WSDL.Admin.introspectResourcesResultResponse response)
        {
        }

        public bool IsFinished(WSDL.Admin.introspectResourcesResultResponse response)
        {
            return response.completed || response.status.status is WSDL.Admin.operationStatus.SUCCESS or WSDL.Admin.operationStatus.FAIL or WSDL.Admin.operationStatus.CANCELED;
        }

        public async Task<WSDL.Admin.introspectResourcesResultResponse> PollTaskResultAsync(int taskId)
        {
            return await TdvClient.IntrospectResourcesResult(taskId, blocking: RetrieveResultInBlockingFashion, detailLevel: WSDL.Admin.detailLevel.SIMPLE);
        }

        public bool ShouldWaitBeforeAnotherPolling(WSDL.Admin.introspectResourcesResultResponse response)
        {
            return PollingInterval.CompareTo(TimeSpan.Zero) > 0;
        }

        public async Task<int> StartTaskAsync()
        {
            _taskId = await TdvClient.IntrospectResourcesTask(DataSourcePath, Resources, IntrospectionOptions);
            return _taskId;
        }
    }
}
