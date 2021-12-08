namespace NoP77svk.TibcoDV.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;

    public partial class TdvWebServiceClient
    {
        public async IAsyncEnumerable<WSDL.Admin.resource> GetResourceInfo(string? path, WSDL.Admin.resourceType? resourceType = null, WSDL.Admin.detailLevel detailLevel = WSDL.Admin.detailLevel.SIMPLE)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path));

            IAsyncEnumerable<WSDL.Admin.getResourceResponse> resInfoAll = _wsClient.EndpointGetObject<WSDL.Admin.getResourceResponse>(
                new TdvSoapWsEndpoint<WSDL.Admin.getResourceRequest>(
                    "getResource",
                    new WSDL.Admin.getResourceRequest()
                    {
                        path = path,
                        type = resourceType ?? WSDL.Admin.resourceType.NONE,
                        detail = detailLevel,
                        typeSpecified = resourceType is not null and not WSDL.Admin.resourceType.NONE
                    })
            );

            await foreach (WSDL.Admin.getResourceResponse resInfoBody in resInfoAll)
            {
                foreach (WSDL.Admin.resource res in resInfoBody.resources)
                    yield return res;
            }
        }

        public async IAsyncEnumerable<TdvRest_ContainerContents> RetrieveResourceChildren(string? path, string resourceType)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path));

            IAsyncEnumerable<List<TdvRest_ContainerContents>> resourceChildrenAll = _wsClient.EndpointGetObject<List<TdvRest_ContainerContents>>(TdvRestWsEndpoint.ResourceApi(HttpMethod.Get)
                .AddResourceFolder("children")
                .AddQuery("path", path)
                .AddQuery("type", resourceType.ToUpper())
            );

            await foreach (List<TdvRest_ContainerContents> resourceChildren in resourceChildrenAll)
            {
                foreach (TdvRest_ContainerContents resourceChild in resourceChildren)
                    yield return resourceChild;
            }
        }

        public async IAsyncEnumerable<TdvRest_ContainerContents> RetrieveResourceChildren(string? path, TdvResourceTypeEnumAgr resourceType)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path));

            (string resourceTypeWs, _, _) = TdvResourceType.CalcWsResourceTypes(resourceType);

            IAsyncEnumerable<TdvRest_ContainerContents> resourceChildrenAll = RetrieveResourceChildren(path, resourceTypeWs);

            await foreach (TdvRest_ContainerContents resourceChild in resourceChildrenAll)
                yield return resourceChild;
        }

        public async Task<List<TdvRest_ContainerContents>> RetrieveResourceChildrenList(string? path, TdvResourceTypeEnumAgr resourceType)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path));

            IAsyncEnumerable<TdvRest_ContainerContents> resourceChildrenAll = RetrieveResourceChildren(path, resourceType);
            return await resourceChildrenAll.ToListAsync();
        }

        public async Task DropAnyResources(IEnumerable<TdvResourceSpecifier> resourceList, bool ifExists = true)
        {
            IEnumerable<IGrouping<TdvResourceTypeEnumAgr, TdvResourceSpecifier>> resourcesByType = resourceList
                .Where(resource => !string.IsNullOrWhiteSpace(resource.Path))
                .GroupBy(resource => resource.ResourceType.Type);

            List<Task> dropTasks = new List<Task>();
            foreach (IGrouping<TdvResourceTypeEnumAgr, TdvResourceSpecifier> singleTypeResources in resourcesByType)
            {
                IEnumerable<string> pathOnlyResources = singleTypeResources.Select(folderItem => folderItem.Path ?? string.Empty);

                if (singleTypeResources.Key is TdvResourceTypeEnumAgr.Folder or TdvResourceTypeEnumAgr.UnknownContainer)
                {
                    dropTasks.Add(DropFolders(pathOnlyResources, ifExists: ifExists));
                }
                else if (singleTypeResources.Key == TdvResourceTypeEnumAgr.PublishedCatalog)
                {
                    dropTasks.Add(DropCatalogs(pathOnlyResources, ifExists: ifExists));
                }
                else if (singleTypeResources.Key == TdvResourceTypeEnumAgr.PublishedSchema)
                {
                    dropTasks.Add(DropSchemas(pathOnlyResources, ifExists: ifExists));
                }
                else if (singleTypeResources.Key == TdvResourceTypeEnumAgr.View)
                {
                    dropTasks.Add(DropDataViews(pathOnlyResources, ifExists: ifExists));
                }
                else if (singleTypeResources.Key is TdvResourceTypeEnumAgr.StoredProcedureSQL or TdvResourceTypeEnumAgr.StoredProcedureOther)
                {
                    dropTasks.Add(DropScripts(pathOnlyResources, ifExists: ifExists));
                }
                else if (singleTypeResources.Key is TdvResourceTypeEnumAgr.DataSourceRelational or TdvResourceTypeEnumAgr.DataSourceExcel or TdvResourceTypeEnumAgr.DataSourceFile or TdvResourceTypeEnumAgr.DataSourceWsWsdl or TdvResourceTypeEnumAgr.DataSourceXmlFile)
                {
                    dropTasks.Add(DropDataSources(pathOnlyResources, ifExists: ifExists));
                }
                else if (singleTypeResources.Key == TdvResourceTypeEnumAgr.PublishedTableOrView)
                {
                    IEnumerable<TdvRest_DeleteLink> massLinkDrop = singleTypeResources
                        .Select(resource => new TdvRest_DeleteLink()
                        {
                            IsTable = resource.ResourceType.WsTargetType == TdvResourceTypeConst.Table,
                            Path = resource.Path
                        });

                    dropTasks.Add(DropLinks(massLinkDrop, ifExists: ifExists));
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(singleTypeResources) + "." + nameof(singleTypeResources.Key), singleTypeResources.Key, "Unrecognized resource type");
                }
            }

            await Task.WhenAll(dropTasks);
        }
    }
}
