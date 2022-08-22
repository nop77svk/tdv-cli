namespace NoP77svk.TibcoDV.API
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using NoP77svk.IO;

    public partial class TdvWebServiceClient
    {
        public async Task CreateDataViews(IEnumerable<TdvRest_CreateDataView> requestBody)
        {
            await _wsClient.EndpointCall(TdvRestWsEndpoint.DataViewApi(HttpMethod.Post)
                .WithContent(requestBody)
            );
        }

        public async Task CreateDataView(string parentPath, string name, string sql, string? annotation = null, bool ifNotExists = false)
        {
            await CreateDataViews(new TdvRest_CreateDataView[]
            {
                new TdvRest_CreateDataView
                {
                    ParentPath = parentPath,
                    Name = name,
                    SQL = sql,
                    IfNotExists = ifNotExists,
                    Annotation = annotation
                }
            });
        }

        public async Task DropDataView(string path, bool ifExists = true)
        {
            await DropDataViews(new[] { path }, ifExists: ifExists);
        }

        public async Task DropDataViews(IEnumerable<string> paths, bool ifExists = true)
        {
            IEnumerable<string> pathsSanitized = paths
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(x => PathExt.Sanitize(x, FolderDelimiter) ?? string.Empty);

            await _wsClient.EndpointCall(TdvRestWsEndpoint.DataViewApi(HttpMethod.Delete)
                .AddTdvQuery(TdvRestEndpointParameterConst.IfExists, ifExists)
                .WithContent(pathsSanitized)
            );
        }
    }
}
