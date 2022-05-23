namespace NoP77svk.TibcoDV.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public partial class TdvWebServiceClient
    {
        public async Task<WSDL.Admin.getIntrospectedResourceIdsResultResponse> GetIntrospectedResourceIds(string dataSourcePath, int pollingIntervalMS = 500, CancellationToken? cancellationToken = null)
        {
            if (pollingIntervalMS < 0)
                throw new ArgumentOutOfRangeException(nameof(pollingIntervalMS), pollingIntervalMS, "Invalid polling interval");

            int taskId = await GetIntrospectedResourceIdsTask(dataSourcePath);

            WSDL.Admin.getIntrospectedResourceIdsResultResponse result;
            while (true)
            {
                result = await GetIntrospectedResourceIdsResult(taskId);
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
    }
}
