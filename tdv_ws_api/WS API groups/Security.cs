namespace NoP77svk.TibcoDV.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;

    public partial class TdvWebServiceClient
    {
        public async IAsyncEnumerable<string> GetDomainGroups(string? domain)
        {
            if (string.IsNullOrEmpty(domain))
                throw new ArgumentNullException(nameof(domain));

            IAsyncEnumerable<List<string>> result = _wsClient.EndpointGetObject<List<string>>(
                TdvRestWsEndpoint.SecurityApi(HttpMethod.Get)
                    .AddResourceFolder("domains")
                    .AddResourceFolder(domain)
                    .AddResourceFolder("groups")
            );

            await foreach (List<string> batchOfGroups in result)
            {
                foreach (string group in batchOfGroups)
                    yield return group;
            }
        }

        public async IAsyncEnumerable<string> GetDomainUsers(string? domain)
        {
            if (string.IsNullOrEmpty(domain))
                throw new ArgumentNullException(nameof(domain));

            IAsyncEnumerable<List<string>> result = _wsClient.EndpointGetObject<List<string>>(
                TdvRestWsEndpoint.SecurityApi(HttpMethod.Get)
                    .AddResourceFolder("domains")
                    .AddResourceFolder(domain)
                    .AddResourceFolder("users")
            );

            await foreach (List<string> batchOfUsers in result)
            {
                foreach (string user in batchOfUsers)
                    yield return user;
            }
        }

        public async Task<string> UpdateResourcePrivileges(
            IEnumerable<WSDL.Admin.privilegeEntry> privEntries,
            bool recursiveUpdate = false,
            WSDL.Admin.updatePrivilegesMode updateMode = WSDL.Admin.updatePrivilegesMode.OVERWRITE_APPEND
        )
        {
            WSDL.Admin.updateResourcePrivilegesRequest input = new ()
            {
                mode = updateMode,
                modeSpecified = true,
                updateRecursively = recursiveUpdate,
                privilegeEntries = privEntries.ToArray()
            };

            IAsyncEnumerable<WSDL.Admin.updateResourcePrivilegesResponse> result = _wsClient.EndpointGetObject<WSDL.Admin.updateResourcePrivilegesResponse>(
                new TdvSoapWsEndpoint<WSDL.Admin.updateResourcePrivilegesRequest>("updateResourcePrivileges", input)
            );

            return await result.CountAsync() > 0 ? "OK" : "NOK";
        }
    }
}
