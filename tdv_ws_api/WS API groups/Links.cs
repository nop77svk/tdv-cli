namespace NoP77svk.TibcoDV.API
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;

    public partial class TdvWebServiceClient
    {
        public async Task<string> CreateLinks(IEnumerable<TdvRest_CreateLink> links, bool ifNotExists = true, bool? isTableOverride = null, bool? ifNotExistsOverride = null)
        {
            IEnumerable<TdvRest_CreateLink> linksSanitized = links
                .Select(link => link with
                {
                    IfNotExists = ifNotExistsOverride ?? link.IfNotExists,
                    IsTable = isTableOverride ?? link.IsTable
                });

            return await _wsClient.EndpointGetString(TdvRestWsEndpoint.LinkApi(HttpMethod.Post)
                .AddTdvQuery(TdvRestEndpointParameterConst.IfNotExists, ifNotExists)
                .WithContent(linksSanitized)
            );
        }

        public async Task<string> DropLinks(IEnumerable<TdvRest_DeleteLink> links, bool ifExists = true, bool? isTableOverride = null)
        {
            IEnumerable<TdvRest_DeleteLink> linksSanitized = links
                .Select(link => link with
                {
                    IsTable = isTableOverride ?? link.IsTable
                });

            return await _wsClient.EndpointGetString(TdvRestWsEndpoint.LinkApi(HttpMethod.Delete)
                .AddTdvQuery(TdvRestEndpointParameterConst.IfExists, ifExists)
                .WithContent(linksSanitized)
            );
        }
    }
}
