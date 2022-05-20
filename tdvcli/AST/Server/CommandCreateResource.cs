namespace NoP77svk.TibcoDV.CLI.AST.Server
{
    using System;
    using System.Threading.Tasks;
    using log4net;
    using NoP77svk.IO;
    using NoP77svk.TibcoDV.API;
    using NoP77svk.TibcoDV.CLI.Commons;
    using NoP77svk.TibcoDV.Commons;

    internal class CommandCreateResource : IAsyncExecutable
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Program));

        internal bool IfNotExists { get; }
        internal object ResourceDDL { get; }

        internal CommandCreateResource(bool ifNotExists, object resourceDDL)
        {
            IfNotExists = ifNotExists;
            ResourceDDL = resourceDDL;
        }

        public async Task Execute(TdvWebServiceClient tdvClient, IInfoOutput output, ParserState parserState)
        {
            using var log = new TraceLog(_log, nameof(Execute));

            if (ResourceDDL is null)
                throw new ArgumentNullException(nameof(ResourceDDL));

            if (ResourceDDL is FolderDDL folderDDL)
                await ExecuteCreateFolder(tdvClient, IfNotExists, folderDDL, output);
            else if (ResourceDDL is SchemaDDL schemaDDL)
                await ExecuteCreateSchema(tdvClient, IfNotExists, schemaDDL, output);
            else if (ResourceDDL is ViewDDL viewDDL)
                await ExecuteCreateView(tdvClient, IfNotExists, viewDDL, output);
            else
                throw new ArgumentOutOfRangeException(nameof(ResourceDDL), ResourceDDL.GetType() + " :: " + ResourceDDL.ToString(), "Unrecognized type of parsed DDL statement");
        }

        private async Task ExecuteCreateFolder(TdvWebServiceClient tdvClient, bool ifNotExists, FolderDDL stmt, IInfoOutput output)
        {
            using var log = new TraceLog(_log, nameof(ExecuteCreateFolder));

            if (string.IsNullOrEmpty(stmt.ResourcePath))
                throw new ArgumentNullException(nameof(stmt) + "." + nameof(stmt.ResourcePath));

            string folderParentPath = PathExt.TrimLastLevel(stmt.ResourcePath) ?? throw new ArgumentNullException(nameof(folderParentPath));
            _log.Debug($"{nameof(folderParentPath)} = {folderParentPath}");

            string folderName = PathExt.GetLastLevel(stmt.ResourcePath) ?? throw new ArgumentNullException(nameof(folderName));
            _log.Debug($"{nameof(folderName)} = {folderName}");

            string result = await tdvClient.CreateFolder(folderParentPath, folderName, ifNotExists: ifNotExists);
            _log.Debug($"{nameof(result)} = {result}");

            if (ifNotExists)
                output.Info($"Folder {stmt.ResourcePath} created (or left intact if there already was one)");
            else
                output.Info($"Folder {stmt.ResourcePath} created");
        }

        private async Task ExecuteCreateSchema(TdvWebServiceClient tdvClient, bool ifNotExists, SchemaDDL stmt, IInfoOutput output)
        {
            using var log = new TraceLog(_log, nameof(ExecuteCreateSchema));

            if (string.IsNullOrEmpty(stmt.ResourcePath))
                throw new ArgumentNullException(nameof(stmt) + "." + nameof(stmt.ResourcePath));

            string result = await tdvClient.CreateSchemas(new string[] { stmt.ResourcePath }, ifNotExists: ifNotExists);
            _log.Debug($"{nameof(result)} = {result}");

            if (ifNotExists)
                output.Info($"Schema {stmt.ResourcePath} created (or left intact if there already was one)");
            else
                output.Info($"Schema {stmt.ResourcePath} created");
        }

        private async Task ExecuteCreateView(TdvWebServiceClient tdvClient, bool ifNotExists, ViewDDL stmt, IInfoOutput output)
        {
            using var log = new TraceLog(_log, nameof(ExecuteCreateView));

            if (string.IsNullOrEmpty(stmt.ResourcePath))
                throw new ArgumentNullException(nameof(stmt) + "." + nameof(stmt.ResourcePath));

            string? parentPath = PathExt.TrimLastLevel(stmt.ResourcePath);
            if (string.IsNullOrEmpty(parentPath))
                throw new ArgumentOutOfRangeException(nameof(stmt) + "." + nameof(stmt.ResourcePath), stmt.ResourcePath, "Cannot determine view's parent path");

            string? viewName = PathExt.GetLastLevel(stmt.ResourcePath);
            if (string.IsNullOrEmpty(viewName))
                throw new ArgumentOutOfRangeException(nameof(stmt) + "." + nameof(stmt.ResourcePath), stmt.ResourcePath, "Cannot determine view's name");

            if (string.IsNullOrWhiteSpace(stmt.ViewQuery))
                throw new ArgumentNullException(nameof(stmt) + "." + nameof(stmt.ViewQuery), "Empty view body");

            await tdvClient.CreateDataView(parentPath, viewName, stmt.ViewQuery, ifNotExists: ifNotExists);

            if (ifNotExists)
                output.Info($"View {stmt.ResourcePath} created (or left intact if there already was one)");
            else
                output.Info($"View {stmt.ResourcePath} created");
        }
    }
}
