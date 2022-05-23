namespace NoP77svk.TibcoDV.API
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public partial class TdvWebServiceClient
    {
        public async Task BeginTransaction(TdvTransactionFailureCompensationMode transactionFailureCompensationMode = TdvTransactionFailureCompensationMode.COMPENSATE, TdvTransactionServerInterruptCompensationMode serverInterruptCompensationMode = TdvTransactionServerInterruptCompensationMode.IGNORE_INTERRUPT)
        {
            IAsyncEnumerable<WSDL.Util.beginTransactionResponse> response = _wsClient.EndpointGetObject<WSDL.Util.beginTransactionResponse>(
                new TdvSoapWsEndpoint<WSDL.Util.beginTransactionRequest>("beginTransaction", new WSDL.Util.beginTransactionRequest()
                {
                    transactionMode = transactionFailureCompensationMode.ToString().ToUpper() + " " + serverInterruptCompensationMode.ToString().ToUpper()
                }
            ));

            await response.LastAsync();
        }

        public async Task CloseTransaction(WSDL.Util.commitAction transactionCloseAction)
        {
            IAsyncEnumerable<WSDL.Util.closeTransactionResponse> response = _wsClient.EndpointGetObject<WSDL.Util.closeTransactionResponse>(
                new TdvSoapWsEndpoint<WSDL.Util.closeTransactionRequest>("closeTransaction", new WSDL.Util.closeTransactionRequest()
                {
                    action = transactionCloseAction
                }
            ));

            await response.LastAsync();
        }
    }
}
