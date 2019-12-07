namespace NoP77svk.TibcoDV.API
{
    using System;
    using System.Collections.Generic;

    public partial class TdvWebServiceClient
    {
        public async IAsyncEnumerable<WSDL.Admin.resource> GetResourceInfo(string? path, WSDL.Admin.resourceType? resourceType = null, WSDL.Admin.detailLevel detailLevel = WSDL.Admin.detailLevel.SIMPLE)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path));

            IAsyncEnumerable<WSDL.Admin.getResourceResponse> resInfoAll = _wsClient.EndpointGetObject<WSDL.Admin.getResourceResponse>(
                new TdvSoapWsEndpoint<WSDL.Admin.getResourceRequest>(
                    "getResource",
                    new WSDL.Admin.getResourceRequest()
                    {
                        path = path,
                        type = resourceType ?? WSDL.Admin.resourceType.NONE,
                        detail = detailLevel,
                        typeSpecified = resourceType is not null and not WSDL.Admin.resourceType.NONE
                    })
            );

            await foreach (WSDL.Admin.getResourceResponse resInfoBody in resInfoAll)
            {
                foreach (WSDL.Admin.resource res in resInfoBody.resources)
                    yield return res;
            }
        }
    }
}
