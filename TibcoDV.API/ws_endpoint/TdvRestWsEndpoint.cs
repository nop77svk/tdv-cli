namespace NoP77svk.TibcoDV.API
{
    using System.Collections.Generic;
    using System.Net.Http;
    using NoP77svk.Web.WS;

    public class TdvRestWsEndpoint : JsonRestWsEndpoint
    {
        public int ApiVersion { get; init; }

        public string ApiFeature { get; init; }

        public TdvRestWsEndpoint(HttpMethod httpMethod, string apiFeature, int apiVersion)
            : base(httpMethod)
        {
            ApiVersion = apiVersion;
            ApiFeature = apiFeature;
        }

        internal static TdvRestWsEndpoint CatalogApi(HttpMethod httpMethod, int apiVersion = 1)
        {
            return new TdvRestWsEndpoint(httpMethod, "catalog", apiVersion);
        }

        internal static TdvRestWsEndpoint ColumnBasedSecurityApi(HttpMethod httpMethod, int apiVersion = 1)
        {
            return new TdvRestWsEndpoint(httpMethod, "cbs", apiVersion);
        }

        internal static TdvRestWsEndpoint DataSourceApi(HttpMethod httpMethod, int apiVersion = 1)
        {
            return new TdvRestWsEndpoint(httpMethod, "datasource", apiVersion);
        }

        internal static TdvRestWsEndpoint DataViewApi(HttpMethod httpMethod, int apiVersion = 1)
        {
            return new TdvRestWsEndpoint(httpMethod, "dataview", apiVersion);
        }

        internal static TdvRestWsEndpoint DeploymentManagerApi(HttpMethod httpMethod, int apiVersion = 1)
        {
            return new TdvRestWsEndpoint(httpMethod, "deploy", apiVersion);
        }

        internal static TdvRestWsEndpoint ExecuteApi(HttpMethod httpMethod, int apiVersion = 1)
        {
            return new TdvRestWsEndpoint(httpMethod, "execute", apiVersion);
        }

        internal static TdvRestWsEndpoint FoldersApi(HttpMethod httpMethod, int apiVersion = 1)
        {
            return new TdvRestWsEndpoint(httpMethod, "folder", apiVersion);
        }

        internal static TdvRestWsEndpoint LinkApi(HttpMethod httpMethod, int apiVersion = 1)
        {
            return new TdvRestWsEndpoint(httpMethod, "link", apiVersion);
        }

        internal static TdvRestWsEndpoint ResourceApi(HttpMethod httpMethod, int apiVersion = 1)
        {
            return new TdvRestWsEndpoint(httpMethod, "resource", apiVersion);
        }

        internal static TdvRestWsEndpoint SchemaApi(HttpMethod httpMethod, int apiVersion = 1)
        {
            return new TdvRestWsEndpoint(httpMethod, "schema", apiVersion);
        }

        internal static TdvRestWsEndpoint ScriptApi(HttpMethod httpMethod, int apiVersion = 1)
        {
            return new TdvRestWsEndpoint(httpMethod, "script", apiVersion);
        }

        internal static TdvRestWsEndpoint SecurityApi(HttpMethod httpMethod, int apiVersion = 1)
        {
            return new TdvRestWsEndpoint(httpMethod, "security", apiVersion);
        }

        internal static TdvRestWsEndpoint SessionApi(HttpMethod httpMethod, int apiVersion = 1)
        {
            return new TdvRestWsEndpoint(httpMethod, "session", apiVersion);
        }

        internal static TdvRestWsEndpoint VersionControlSystemApi(HttpMethod httpMethod, int apiVersion = 1)
        {
            return new TdvRestWsEndpoint(httpMethod, "vcs", apiVersion);
        }

        internal static TdvRestWsEndpoint WorkloadManagementApi(HttpMethod httpMethod, int apiVersion = 1)
        {
            return new TdvRestWsEndpoint(httpMethod, "workload", apiVersion);
        }

        private IEnumerable<string> OverrideResource()
        {
            yield return "rest";
            yield return ApiFeature;
            yield return $"v{ApiVersion}";

            if (Resource is not null)
            {
                foreach (string? resFolder in Resource)
                    yield return resFolder;
            }
        }

        public override string UriResource
        {
            get => IWebServiceEndpoint.ResourceToString(OverrideResource());
        }
    }
}
