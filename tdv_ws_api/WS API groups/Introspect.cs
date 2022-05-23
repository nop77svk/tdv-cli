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

        public async Task<WSDL.Admin.getIntrospectedResourceIdsResultResponse> GetIntrospectedResourceIds(string dataSourcePath, int pollingIntervalMS = 500, CancellationToken? cancellationToken = null)
        {
            if (pollingIntervalMS < 0)
                throw new ArgumentOutOfRangeException(nameof(pollingIntervalMS), pollingIntervalMS, "Invalid polling interval");

            int taskId = await GetIntrospectedResourceIdsTask(dataSourcePath);

            WSDL.Admin.getIntrospectedResourceIdsResultResponse result;
            while (true)
            {
                result = await GetIntrospectedResourceIdsResult(taskId);

                // 2do! yield the results as IAsyncEnumerable; result.completed marks the checkpoint of "no more data on output"

                if (result.completed)
                    break;

                cancellationToken?.ThrowIfCancellationRequested();

                if (pollingIntervalMS > 0)
                    await Task.Delay(pollingIntervalMS);
            }

            return result;
        }

        public async Task<WSDL.Admin.getIntrospectableResourceIdsResultResponse> GetIntrospectableResourceIds(string dataSourcePath, int pollingIntervalMS = 500, bool clearCachePriorToRefresh = true, CancellationToken? cancellationToken = null)
        {
            if (pollingIntervalMS < 0)
                throw new ArgumentOutOfRangeException(nameof(pollingIntervalMS), pollingIntervalMS, "Invalid polling interval");

            if (clearCachePriorToRefresh)
                await ClearIntrospectableResourceIdCache(dataSourcePath);

            int taskId = await GetIntrospectableResourceIdsTask(dataSourcePath);

            WSDL.Admin.getIntrospectableResourceIdsResultResponse result;
            while (true)
            {
                result = await GetIntrospectableResourceIdsResult(taskId);

                // 2do! yield the results as IAsyncEnumerable; result.completed marks the checkpoint of "no more data on output"

                if (result.completed)
                    break;

                cancellationToken?.ThrowIfCancellationRequested();

                if (pollingIntervalMS > 0)
                    await Task.Delay(pollingIntervalMS);
            }

            return result;
        }

        private async Task<int> GetIntrospectedResourceIdsTask(string dataSourcePath)
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

        private async Task<WSDL.Admin.getIntrospectedResourceIdsResultResponse> GetIntrospectedResourceIdsResult(int taskId)
        {
            IAsyncEnumerable<WSDL.Admin.getIntrospectedResourceIdsResultResponse> response = _wsClient.EndpointGetObject<WSDL.Admin.getIntrospectedResourceIdsResultResponse>(
                new TdvSoapWsEndpoint<WSDL.Admin.getIntrospectedResourceIdsResultRequest>("getIntrospectedResourceIdsResult", new WSDL.Admin.getIntrospectedResourceIdsResultRequest()
                {
                    taskId = taskId.ToString()
                }
            ));

            return await response.FirstAsync();
        }

        private async Task<int> GetIntrospectableResourceIdsTask(string dataSourcePath)
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

        private async Task<WSDL.Admin.getIntrospectableResourceIdsResultResponse> GetIntrospectableResourceIdsResult(int taskId)
        {
            IAsyncEnumerable<WSDL.Admin.getIntrospectableResourceIdsResultResponse> response = _wsClient.EndpointGetObject<WSDL.Admin.getIntrospectableResourceIdsResultResponse>(
                new TdvSoapWsEndpoint<WSDL.Admin.getIntrospectableResourceIdsResultRequest>("getIntrospectableResourceIdsResult", new WSDL.Admin.getIntrospectableResourceIdsResultRequest()
                {
                    taskId = taskId.ToString()
                }
            ));

            return await response.FirstAsync();
        }
    }
}
