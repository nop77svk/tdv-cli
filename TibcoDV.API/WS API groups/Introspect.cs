namespace NoP77svk.TibcoDV.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public partial class TdvWebServiceClient
    {
        public async Task ClearIntrospectableResourceIdCache(string dataSourcePath)
        {
            IAsyncEnumerable<WSDL.Admin.clearIntrospectableResourceIdCacheResponse> response = _wsClient.EndpointGetObject<WSDL.Admin.clearIntrospectableResourceIdCacheResponse>(
                new TdvSoapWsEndpoint<WSDL.Admin.clearIntrospectableResourceIdCacheRequest>("clearIntrospectableResourceIdCache", new WSDL.Admin.clearIntrospectableResourceIdCacheRequest()
                {
                    path = dataSourcePath
                }
            ));

            await response.LastAsync();
        }

        internal async Task<int> GetIntrospectedResourceIdsTask(string dataSourcePath)
        {
            IAsyncEnumerable<WSDL.Admin.getIntrospectedResourceIdsTaskResponse> response = _wsClient.EndpointGetObject<WSDL.Admin.getIntrospectedResourceIdsTaskResponse>(
                new TdvSoapWsEndpoint<WSDL.Admin.getIntrospectedResourceIdsTaskRequest>("getIntrospectedResourceIdsTask", new WSDL.Admin.getIntrospectedResourceIdsTaskRequest()
                {
                    path = dataSourcePath
                }
            ));

            WSDL.Admin.getIntrospectedResourceIdsTaskResponse result = await response.FirstAsync();
            return int.Parse(result.taskId);
        }

        internal async Task<WSDL.Admin.getIntrospectedResourceIdsResultResponse> GetIntrospectedResourceIdsResult(int taskId)
        {
            IAsyncEnumerable<WSDL.Admin.getIntrospectedResourceIdsResultResponse> response = _wsClient.EndpointGetObject<WSDL.Admin.getIntrospectedResourceIdsResultResponse>(
                new TdvSoapWsEndpoint<WSDL.Admin.getIntrospectedResourceIdsResultRequest>("getIntrospectedResourceIdsResult", new WSDL.Admin.getIntrospectedResourceIdsResultRequest()
                {
                    taskId = taskId.ToString()
                }
            ));

            return await response.FirstAsync();
        }

        internal async Task<int> GetIntrospectableResourceIdsTask(string dataSourcePath)
        {
            IAsyncEnumerable<WSDL.Admin.getIntrospectableResourceIdsTaskResponse> response = _wsClient.EndpointGetObject<WSDL.Admin.getIntrospectableResourceIdsTaskResponse>(
                new TdvSoapWsEndpoint<WSDL.Admin.getIntrospectableResourceIdsTaskRequest>("getIntrospectableResourceIdsTask", new WSDL.Admin.getIntrospectableResourceIdsTaskRequest()
                {
                    path = dataSourcePath
                }
            ));

            WSDL.Admin.getIntrospectableResourceIdsTaskResponse result = await response.FirstAsync();
            return int.Parse(result.taskId);
        }

        internal async Task<WSDL.Admin.getIntrospectableResourceIdsResultResponse> GetIntrospectableResourceIdsResult(int taskId)
        {
            IAsyncEnumerable<WSDL.Admin.getIntrospectableResourceIdsResultResponse> response = _wsClient.EndpointGetObject<WSDL.Admin.getIntrospectableResourceIdsResultResponse>(
                new TdvSoapWsEndpoint<WSDL.Admin.getIntrospectableResourceIdsResultRequest>("getIntrospectableResourceIdsResult", new WSDL.Admin.getIntrospectableResourceIdsResultRequest()
                {
                    taskId = taskId.ToString()
                }
            ));

            return await response.FirstAsync();
        }

        internal async Task<int> IntrospectResourcesTask(string dataSourcePath, IEnumerable<WSDL.Admin.introspectionPlanEntry> resources, TdvIntrospectionOptions options)
        {
            IAsyncEnumerable<WSDL.Admin.introspectResourcesTaskResponse> response = _wsClient.EndpointGetObject<WSDL.Admin.introspectResourcesTaskResponse>(
                new TdvSoapWsEndpoint<WSDL.Admin.introspectResourcesTaskRequest>("introspectResourcesTask", new WSDL.Admin.introspectResourcesTaskRequest()
                {
                    path = dataSourcePath,
                    runInBackgroundTransaction = options.RunInBackgroundTransaction,
                    plan = new WSDL.Admin.introspectionPlan()
                    {
                        autoRollback = options.AutoRollback,
                        failFast = options.FailFast,
                        commitOnFailure = options.CommitOnFailure,
                        scanForNewResourcesToAutoAdd = options.ScanForNewResourcesToAutoAdd,
                        updateAllIntrospectedResources = options.UpdateAllIntrospectedResources,
                        entries = resources.ToArray()
                    }
                }
            ));

            WSDL.Admin.introspectResourcesTaskResponse result = await response.FirstAsync();
            return int.Parse(result.taskId);
        }

        internal async Task<WSDL.Admin.introspectResourcesResultResponse> IntrospectResourcesResult(int taskId, bool blocking = false, WSDL.Admin.detailLevel detailLevel = WSDL.Admin.detailLevel.SIMPLE)
        {
            IAsyncEnumerable<WSDL.Admin.introspectResourcesResultResponse> response = _wsClient.EndpointGetObject<WSDL.Admin.introspectResourcesResultResponse>(
                new TdvSoapWsEndpoint<WSDL.Admin.introspectResourcesResultRequest>("introspectResourcesResult", new WSDL.Admin.introspectResourcesResultRequest()
                {
                    taskId = taskId.ToString(),
                    detail = detailLevel,
                    block = blocking,
                    blockSpecified = true
                }
            ));

            return await response.FirstAsync();
        }
    }
}
