namespace NoP77svk.TibcoDV.CLI.AST.Server
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NoP77svk.IO;
    using NoP77svk.Linq;
    using NoP77svk.TibcoDV.API;
    using NoP77svk.TibcoDV.CLI.Commons;
    using NoP77svk.TibcoDV.CLI.Parser;
    using WSDL = NoP77svk.TibcoDV.API.WSDL.Admin;

    internal class CommandPublish : IAsyncExecutable
    {
        public string Source { get; }
        public string Target { get; }
        public string? FlattenString { get; }
        public bool IfNotExists { get; }

        public CommandPublish(string source, string target, string? flattenString = null, bool ifNotExists = false)
        {
            Source = PathExt.Sanitize(source) ?? throw new ArgumentNullException(nameof(source));
            Target = PathExt.Sanitize(target) ?? throw new ArgumentNullException(nameof(target));
            FlattenString = flattenString;
            IfNotExists = ifNotExists;
        }

        public async Task Execute(TdvWebServiceClient tdvClient, IInfoOutput output, ParserState parserState)
        {
            using Task<WSDL.resource> sourceInfoTask = tdvClient.GetResourceInfo(Source).FirstAsync().AsTask();
            using Task<WSDL.resource> targetInfoTask = tdvClient.GetResourceInfo(Target).FirstAsync().AsTask();
            await Task.WhenAll(sourceInfoTask, targetInfoTask);

            TdvResourceType sourceType = new TdvResourceType(sourceInfoTask.Result.type, sourceInfoTask.Result.subtype);
            TdvResourceType targetType = new TdvResourceType(targetInfoTask.Result.type, targetInfoTask.Result.subtype);
            output.InfoNoEoln($"Publishing {Source} -> {Target} ");

            if (sourceType.Type == TdvResourceTypeEnumAgr.Folder)
            {
                bool flattenFolderTreesToSchemas;
                if (targetType.Type == TdvResourceTypeEnumAgr.PublishedSchema)
                    flattenFolderTreesToSchemas = false;
                else if (targetType.Type is TdvResourceTypeEnumAgr.PublishedCatalog or TdvResourceTypeEnumAgr.DataSourceRelational)
                    flattenFolderTreesToSchemas = true;
                else
                    throw new CannotHandleResourceType(targetType);

                List<TdvRest_ContainerContents> subtreeContents = await tdvClient.RetrieveContainerContentsRecursive(Source, sourceType.Type).ToListAsync();
                output.InfoNoEoln(".");

                IEnumerable<TdvRest_ContainerContents> subtreeContentsSanitized = subtreeContents
                    .Where(folderItem => !string.IsNullOrEmpty(folderItem.Name))
                    .Where(folderItem => IsResourceTypeEligibleForPublishing(folderItem.TdvResourceType))
                    .Select(folderItem => folderItem with
                    {
                        Path = PathExt.Sanitize(folderItem.Path)
                    });
                IEnumerable<TdvRest_CreateLink> linkCreateRequests;

                if (flattenFolderTreesToSchemas)
                {
                    Dictionary<string, string> folderToSchemaMap = CalculateFoldersToSchemasMap(subtreeContents, FlattenString);
                    await PrecreateSchemas(tdvClient, folderToSchemaMap);
                    output.InfoNoEoln(".");

                    linkCreateRequests = subtreeContentsSanitized
                        .Select(folderItem => new TdvRest_CreateLink()
                        {
                            IsTable = IsTableLikeResourceType(folderItem.TdvResourceType),
                            IfNotExists = IfNotExists,
                            SourceObjectPath = folderItem.Path,
                            PublishedLinkPath = Target
                                + "/"
                                + folderToSchemaMap[PathExt.TrimLastLevel(folderItem.Path) ?? string.Empty]
                                + "/"
                                + PathExt.GetLastLevel(folderItem.Path)
                        });
                }
                else
                {
                    linkCreateRequests = subtreeContentsSanitized
                        .Select(folderItem => new TdvRest_CreateLink()
                        {
                            IsTable = IsTableLikeResourceType(folderItem.TdvResourceType),
                            IfNotExists = IfNotExists,
                            SourceObjectPath = PathExt.Sanitize(folderItem.Path),
                            PublishedLinkPath = PathExt.Sanitize(Target)
                                + "/"
                                + PathExt.TrimLeadingPath(folderItem.Path, Source)?.Replace("/", FlattenString)
                        });
                }

                int totalLinksCreated = await MassCreateLinksChunked(tdvClient, linkCreateRequests, _ => { output.InfoNoEoln("."); });
                output.InfoNoEoln($". {totalLinksCreated}");
            }
            else
            {
                if (targetType.Type is not TdvResourceTypeEnumAgr.PublishedSchema and not TdvResourceTypeEnumAgr.PublishedCatalog and not TdvResourceTypeEnumAgr.DataSourceRelational)
                    throw new CannotHandleResourceType(targetType);

                if (FlattenString != null)
                    throw new ArgumentOutOfRangeException(nameof(FlattenString), FlattenString, "Hierarchy flattening string is invalid in single-resource publish mode");

                TdvRest_CreateLink createLinkRequest = new TdvRest_CreateLink()
                {
                    SourceObjectPath = Source,
                    PublishedLinkPath = Target + "/" + PathExt.GetLastLevel(Source),
                    IfNotExists = IfNotExists,
                    IsTable = targetType.WsType == WSDL.resourceType.TABLE
                };

                await tdvClient.CreateLinks(new[] { createLinkRequest });
                output.InfoNoEoln("...");
            }

            output.Info(" Done");
        }

        private static bool IsResourceTypeEligibleForPublishing(TdvResourceTypeEnumAgr tdvResourceType)
        {
            return tdvResourceType
                is TdvResourceTypeEnumAgr.Table
                or TdvResourceTypeEnumAgr.View
                or TdvResourceTypeEnumAgr.StoredProcedureSQL;
        }

        private static bool IsTableLikeResourceType(TdvResourceTypeEnumAgr tdvResourceType)
        {
            return tdvResourceType is TdvResourceTypeEnumAgr.Table or TdvResourceTypeEnumAgr.View;
        }

        private static async Task<int> MassCreateLinksChunked(TdvWebServiceClient tdvClient, IEnumerable<TdvRest_CreateLink> linkCreateRequests, Action<int> visualFeedback)
        {
            int totalLinksCreated = 0;

            IEnumerable<ChunkOf<TdvRest_CreateLink>> linkCreateRequestsChunked = linkCreateRequests
                .ChunkByMeasure(
                    req => (req.SourceObjectPath?.Length ?? 0) + (req.PublishedLinkPath?.Length ?? 0) + (req.Annotation?.Length ?? 0),
                    31 * 1024
                );

            foreach (ChunkOf<TdvRest_CreateLink> chunk in linkCreateRequestsChunked)
            {
                if (chunk.Chunk == null)
                    throw new NullReferenceException("NULL chunk of link creation requests found");

                await tdvClient.CreateLinks(chunk.Chunk);
                totalLinksCreated += chunk.Chunk.Count;
                visualFeedback(totalLinksCreated);
            }

            return totalLinksCreated;
        }

        private Dictionary<string, string> CalculateFoldersToSchemasMap(List<TdvRest_ContainerContents> subtreeContents, string? flattenString)
        {
            return subtreeContents
                .Where(folderItem => folderItem.TdvResourceType is TdvResourceTypeEnumAgr.Folder or TdvResourceTypeEnumAgr.UnknownContainer)
                .Select(folderItem => folderItem with
                {
                    Path = PathExt.Sanitize(folderItem.Path)
                })
                .ToDictionary(
                    folderItem => folderItem.Path ?? string.Empty,
                    folderItem => PathExt.TrimLeadingPath(folderItem.Path ?? string.Empty, Source)
                        ?.TrimStart('/')
                        ?.Replace("/", flattenString ?? string.Empty)
                        ?? string.Empty
                );
        }

        private async Task PrecreateSchemas(TdvWebServiceClient tdvClient, Dictionary<string, string> folderToSchemaMap)
        {
            IEnumerable<TdvRest_CreateSchema> schemaCreateRequests = folderToSchemaMap
                .Select(folderMap => folderMap.Value)
                .Distinct()
                .Select(schemaName => new TdvRest_CreateSchema()
                {
                    Path = Target + "/" + schemaName,
                    IfNotExists = true,
                    Annotation = string.Empty
                });

            await tdvClient.CreateSchemas(schemaCreateRequests);
        }
    }
}
