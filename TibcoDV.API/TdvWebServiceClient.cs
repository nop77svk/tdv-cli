namespace NoP77svk.TibcoDV.API
{
    using System;
    using NoP77svk.Web.WS;

    /// <summary>
    /// Implementation of a few Tibco DV 8.4 server's REST/SOAP API calls as specified
    /// in <see cref="https://docs.tibco.com/pub/tdv/8.4.0/doc/html/StudioHelp/index.html#page/StudioHelp/Ch_7_REST-API.TDV%2520Server%2520REST%2520APIs.html#"/>Tibco DV 8.4 REST API Guide</see>
    /// and <see cref="https://docs.tibco.com/pub/tdv/8.4.0/doc/html/StudioHelp/index.html#page/StudioHelp/Ch_3_OperationsList.Operations%2520Reference.html#"/>Tibco DV 8.4 WS API Guide</see>.
    /// </summary>
    public partial class TdvWebServiceClient
    {
        public const char FolderDelimiter = '/';

        private readonly HttpWebServiceClient _wsClient;

        public TdvWebServiceClient(HttpWebServiceClient wsClient, int restApiVersion = 1)
        {
            _wsClient = wsClient;
            RestApiVersion = restApiVersion;
        }

        public int RestApiVersion { get; init; }

        public static TdvResourceTypeEnumAgr CalcResourceType(TdvRest_ContainerContents resource)
        {
            try
            {
                return new TdvResourceType(resource.Type ?? string.Empty, resource.SubType ?? string.Empty, resource.TargetType).Type;
            }
            catch (Exception e)
            {
                throw new ArgumentOutOfRangeException($"Error while determining type of resource \"{resource.Path}\"", e);
            }
        }
    }
}
