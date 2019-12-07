namespace NoP77svk.TibcoDV.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using NoP77svk.IO;

    public partial class TdvWebServiceClient
    {
        public async Task<string> CreateFolder(string parentPath, string name, string? annotation = null, bool ifNotExists = true)
        {
            return await CreateFolders(new TdvRest_CreateFolder[]
            {
                new TdvRest_CreateFolder()
                {
                    ParentPath = parentPath,
                    Name = name,
                    Annotation = annotation,
                    IfNotExists = ifNotExists
                }
            });
        }

        public async Task<string> CreateFolders(IEnumerable<TdvRest_CreateFolder> folders)
        {
            IEnumerable<TdvRest_CreateFolder> foldersSanitized = folders
                .Select(x => x with
                {
                    Name = x.Name?.Trim('/'),
                    ParentPath = PathExt.Sanitize(x.ParentPath, FolderDelimiter) ?? string.Empty,
                    Annotation = x.Annotation ?? string.Empty
                });

            return await _wsClient.EndpointGetString(TdvRestWsEndpoint.FoldersApi(HttpMethod.Post)
                .WithContent(foldersSanitized)
            );
        }

        public async Task DropAnyContainers(IEnumerable<TdvRest_ContainerContents> paths, bool ifExists = true)
        {
            List<Task<string>> dropContainerTasks = new ();

            IEnumerable<TdvRest_ContainerContents> pathsSanitized = paths
                .Where(folderItem => !string.IsNullOrWhiteSpace(folderItem.Path))
                .Select(folderItem => folderItem with
                {
                    Path = PathExt.Sanitize(folderItem.Path, FolderDelimiter)
                });

            if (pathsSanitized.Where(folderItem => folderItem.TdvResourceType == TdvResourceTypeEnumAgr.Folder).Any())
            {
                dropContainerTasks.Add(DropFolders(
                    pathsSanitized
                        .Where(folderItem => folderItem.TdvResourceType == TdvResourceTypeEnumAgr.Folder)
                        .Select(folderItem => folderItem.Path ?? "???"),
                    ifExists: ifExists
                ));
            }

            if (pathsSanitized.Where(folderItem => folderItem.TdvResourceType == TdvResourceTypeEnumAgr.PublishedSchema).Any())
            {
                dropContainerTasks.Add(DropSchemas(
                    pathsSanitized
                        .Where(folderItem => folderItem.TdvResourceType == TdvResourceTypeEnumAgr.PublishedSchema)
                        .Select(folderItem => folderItem.Path ?? "???"),
                    ifExists: ifExists
                ));
            }

            if (pathsSanitized.Where(folderItem => folderItem.TdvResourceType == TdvResourceTypeEnumAgr.PublishedCatalog).Any())
            {
                dropContainerTasks.Add(DropCatalogs(
                    pathsSanitized
                        .Where(folderItem => folderItem.TdvResourceType == TdvResourceTypeEnumAgr.PublishedCatalog)
                        .Select(folderItem => folderItem.Path ?? "???"),
                    ifExists: ifExists
                ));
            }

            await Task.WhenAll(dropContainerTasks);
        }

        public async Task<string> DropAnyContainers(IEnumerable<string> paths, TdvResourceTypeEnumAgr folderType, bool ifExists = true)
        {
            if (folderType == TdvResourceTypeEnumAgr.Folder)
                return await DropFolders(paths, ifExists: ifExists);
            else if (folderType == TdvResourceTypeEnumAgr.PublishedCatalog)
                return await DropCatalogs(paths, ifExists: ifExists);
            else if (folderType == TdvResourceTypeEnumAgr.PublishedSchema)
                return await DropSchemas(paths, ifExists: ifExists);
            else
                throw new ArgumentOutOfRangeException(nameof(folderType), folderType.ToString());
        }

        public async Task<string> DropFolder(string folder, bool ifExists = true)
        {
            return await DropFolders(new string[] { folder }, ifExists);
        }

        public async Task<string> DropFolders(IEnumerable<string> folders, bool ifExists = true)
        {
            IEnumerable<string> foldersSanitized = folders
                .Where(folder => !string.IsNullOrWhiteSpace(folder))
                .Select(x => PathExt.Sanitize(x, FolderDelimiter) ?? string.Empty);

            return await _wsClient.EndpointGetString(TdvRestWsEndpoint.FoldersApi(HttpMethod.Delete)
                .AddTdvQuery(TdvRestEndpointParameterConst.IfExists, ifExists)
                .WithContent(foldersSanitized)
            );
        }

        public async Task DropFolderRecursive(
            string? rootNodePath,
            bool dropSelf,
            TdvResourceTypeEnumAgr resourceType = TdvResourceTypeEnumAgr.Folder,
            Action<string>? doneFeedbackCallback = null
        )
        {
            if (string.IsNullOrWhiteSpace(rootNodePath))
                throw new ArgumentNullException(nameof(rootNodePath));

            IEnumerable<TdvRest_ContainerContents> folderContents = await RetrieveContainerContents(rootNodePath, resourceType);
            IEnumerable<TdvRest_ContainerContents> nonemptyFolderContents = folderContents
                .Where(folderItem => !string.IsNullOrWhiteSpace(folderItem.Path));

            IEnumerable<string> viewsToDrop = nonemptyFolderContents
                .Where(folderItem => folderItem.TdvResourceType == TdvResourceTypeEnumAgr.View)
                .Select(folderItem => PathExt.Sanitize(folderItem.Path, FolderDelimiter) ?? string.Empty);
            IEnumerable<TdvRest_DeleteLink> linksToDrop = nonemptyFolderContents
                .Where(folderItem => folderItem.TdvResourceType == TdvResourceTypeEnumAgr.PublishedTableOrView)
                .Select(folderItem => new TdvRest_DeleteLink()
                {
                    Path = PathExt.Sanitize(folderItem.Path, FolderDelimiter),
                    IsTable = true
                });
            IEnumerable<TdvRest_ContainerContents> containersToDrop = nonemptyFolderContents
                .Where(folderItem => folderItem.TdvResourceType is TdvResourceTypeEnumAgr.Folder or TdvResourceTypeEnumAgr.PublishedCatalog or TdvResourceTypeEnumAgr.PublishedSchema);

            IEnumerable<Task> dropTasks = containersToDrop
                .Select(folder => DropFolderRecursive(folder.Path, false))
                // add the single-call mass-deletions
                .Append(DropDataViews(viewsToDrop))
                .Append(DropLinks(linksToDrop));

            await Task.WhenAll(dropTasks);

            await DropAnyContainers(containersToDrop);

            if (dropSelf)
                await DropAnyContainers(new[] { rootNodePath }, resourceType);

            doneFeedbackCallback?.Invoke(rootNodePath);
        }

        public async Task<List<TdvRest_ContainerContents>> RetrieveContainerContents(string? path, TdvResourceTypeEnumAgr resourceType)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path));

            (string jsonResourceType, _) = TdvResourceType.CalcWsResourceTypes(resourceType);

            return await _wsClient.EndpointGetObject<List<TdvRest_ContainerContents>>(TdvRestWsEndpoint.ResourceApi(HttpMethod.Get)
                .AddResourceFolder("children")
                .AddQuery("path", path)
                .AddQuery("type", jsonResourceType.ToLower())
            ).FirstAsync();
        }

        public async IAsyncEnumerable<TdvRest_ContainerContents> RetrieveContainerContentsRecursive(IEnumerable<ValueTuple<string?, TdvResourceTypeEnumAgr>>? containerPaths)
        {
            if (containerPaths is null || !containerPaths.Any())
                yield break;

            List<Task<List<TdvRest_ContainerContents>>> subfolderReaders = new ();
            foreach (ValueTuple<string?, TdvResourceTypeEnumAgr> resourceSpec in containerPaths.Distinct())
            {
                if (string.IsNullOrWhiteSpace(resourceSpec.Item1))
                    throw new ArgumentNullException(nameof(resourceSpec));

                subfolderReaders.Add(RetrieveContainerContents(resourceSpec.Item1, resourceSpec.Item2));

                while (subfolderReaders.Any())
                {
                    Task<List<TdvRest_ContainerContents>> finishedSubfolderReader = await Task.WhenAny(subfolderReaders);
                    subfolderReaders.Remove(finishedSubfolderReader);

                    foreach (TdvRest_ContainerContents folderItem in finishedSubfolderReader.Result)
                    {
                        if (folderItem.TdvResourceType is TdvResourceTypeEnumAgr.Folder
                            or TdvResourceTypeEnumAgr.PublishedCatalog
                            or TdvResourceTypeEnumAgr.PublishedSchema
                            or TdvResourceTypeEnumAgr.DataSourceCompositeWebService
                            or TdvResourceTypeEnumAgr.DataSourceRelational)
                        {
                            subfolderReaders.Add(RetrieveContainerContents(folderItem.Path, folderItem.TdvResourceType));
                        }

                        yield return folderItem;
                    }
                }
            }
        }

        public async IAsyncEnumerable<TdvRest_ContainerContents> RetrieveContainerContentsRecursive(string? containerPath, TdvResourceTypeEnumAgr resourceType)
        {
            if (string.IsNullOrWhiteSpace(containerPath))
                throw new ArgumentNullException(nameof(containerPath));

            ValueTuple<string?, TdvResourceTypeEnumAgr>[] input = { new ValueTuple<string?, TdvResourceTypeEnumAgr>(containerPath, resourceType) };
            IAsyncEnumerable<TdvRest_ContainerContents> retrievalTask = RetrieveContainerContentsRecursive(input);
            await foreach (TdvRest_ContainerContents folderItem in retrievalTask)
                yield return folderItem;
        }
    }
}
