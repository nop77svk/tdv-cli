namespace NoP77svk.TibcoDV.API
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using NoP77svk.Text;

    public partial class TdvWebServiceClient
    {
        public async Task<string> CreateSchemas(IEnumerable<TdvRest_CreateSchema> schemas, bool? ifNotExistsOverride = null)
        {
            IEnumerable<TdvRest_CreateSchema> schemasSanitized = schemas
                .Where(schema => !string.IsNullOrWhiteSpace(schema.Path))
                .Select(schema => schema with
                {
                    Path = PathExt.Sanitize(schema.Path, FolderDelimiter),
                    IfNotExists = ifNotExistsOverride ?? schema.IfNotExists
                });

            return await _wsClient.EndpointGetString(TdvRestWsEndpoint.SchemaApi(HttpMethod.Post)
                .AddResourceFolder("virtual")
                .WithContent(schemasSanitized)
            );
        }

        public async Task<string> CreateSchemas(IEnumerable<string> schemas, bool ifNotExists = true, string? annotation = null)
        {
            IEnumerable<TdvRest_CreateSchema> schemaCreationRequests = schemas
                .Where(schema => !string.IsNullOrWhiteSpace(schema))
                .Select(schema => new TdvRest_CreateSchema()
                {
                    Path = PathExt.Sanitize(schema, FolderDelimiter),
                    IfNotExists = ifNotExists,
                    Annotation = annotation
                });

            return await CreateSchemas(schemaCreationRequests);
        }

        public async Task<string> DropSchemas(IEnumerable<string> schemas, bool ifExists = true)
        {
            IEnumerable<string> schemasSanitized = schemas
                .Where(schema => !string.IsNullOrWhiteSpace(schema))
                .Select(schema => PathExt.Sanitize(schema, FolderDelimiter) ?? string.Empty);

            return await _wsClient.EndpointGetString(TdvRestWsEndpoint.SchemaApi(HttpMethod.Delete)
                .AddResourceFolder("virtual")
                .AddTdvQuery(TdvRestEndpointParameterConst.IfExists, ifExists)
                .WithContent(schemasSanitized)
            );
        }
    }
}
