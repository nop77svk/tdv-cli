namespace NoP77svk.TibcoDV.API
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public partial class TdvWebServiceClient
    {
        public async Task<string> BeginSession()
        {
            IAsyncEnumerable<WSDL.Util.beginSessionResponse> response = _wsClient.EndpointGetObject<WSDL.Util.beginSessionResponse>(
                new TdvSoapWsEndpoint<WSDL.Util.beginSessionRequest>("beginSession", new WSDL.Util.beginSessionRequest())
            );

            WSDL.Util.beginSessionResponse result = await response.FirstAsync();
            return result.sessionToken;
        }

        public async Task CloseSession()
        {
            IAsyncEnumerable<WSDL.Util.closeSessionResponse> response = _wsClient.EndpointGetObject<WSDL.Util.closeSessionResponse>(
                new TdvSoapWsEndpoint<WSDL.Util.closeSessionRequest>("closeSession", new WSDL.Util.closeSessionRequest())
            );

            await response.LastAsync();
        }
    }
}
