namespace NoP77svk.TibcoDV.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;

    public partial class TdvWebServiceClient
    {
        public async Task<ICollection<TdvRest_TableColumns>> RetrieveTableColumns(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path));

            return await _wsClient.EndpointGetObject<List<TdvRest_TableColumns>>(TdvRestWsEndpoint.ResourceApi(HttpMethod.Get)
                .AddResourceFolder("table")
                .AddResourceFolder("columns")
                .AddQuery("path", path)
                .AddQuery("type", "table")
            ).FirstAsync();
        }
    }
}
