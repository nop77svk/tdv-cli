namespace NoP77svk.TibcoDV.API
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using NoP77svk.Text;

    public partial class TdvWebServiceClient
    {
        public async Task<string> CreateCatalogs(IEnumerable<TdvRest_CreateCatalog> catalogs, bool? ifNotExistsOverride = null)
        {
            IEnumerable<TdvRest_CreateCatalog> catalogsSanitized = catalogs
                .Where(catalog => !string.IsNullOrWhiteSpace(catalog.Path))
                .Select(catalog => catalog with
                {
                    Path = PathExt.Sanitize(catalog.Path, FolderDelimiter),
                    NewPath = PathExt.Sanitize(catalog.Path, FolderDelimiter),
                    IfNotExists = ifNotExistsOverride ?? catalog.IfNotExists
                });

            return await _wsClient.EndpointGetString(TdvRestWsEndpoint.CatalogApi(HttpMethod.Post)
                .AddResourceFolder("virtual")
                .WithContent(catalogsSanitized)
            );
        }

        public async Task<string> DropCatalogs(IEnumerable<string> catalogs, bool ifExists = true)
        {
            IEnumerable<string> catalogsSanitized = catalogs
                .Where(catalog => !string.IsNullOrWhiteSpace(catalog))
                .Select(catalog => PathExt.Sanitize(catalog, FolderDelimiter) ?? string.Empty);

            return await _wsClient.EndpointGetString(TdvRestWsEndpoint.CatalogApi(HttpMethod.Delete)
                .AddResourceFolder("virtual")
                .AddTdvQuery(TdvRestEndpointParameterConst.IfExists, ifExists)
                .WithContent(catalogsSanitized)
            );
        }
    }
}
