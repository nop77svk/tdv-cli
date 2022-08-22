namespace NoP77svk.TibcoDV.API
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using NoP77svk.Text;

    public partial class TdvWebServiceClient
    {
        public async Task<string> DropDataSources(IEnumerable<string?> paths, bool ifExists = true)
        {
            IEnumerable<string> pathsSanitized = paths
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => PathExt.Sanitize(path) ?? string.Empty);

            return await _wsClient.EndpointGetString(TdvRestWsEndpoint.DataSourceApi(HttpMethod.Delete)
                .AddTdvQuery(TdvRestEndpointParameterConst.IfExists, ifExists)
                .WithContent(pathsSanitized)
            );
        }
    }
}
