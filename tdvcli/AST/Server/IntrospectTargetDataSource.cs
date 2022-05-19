#pragma warning disable SA1313
namespace NoP77svk.TibcoDV.CLI.AST.Server
{
    using System.Collections.Generic;

    internal record IntrospectTargetDataSource(string SchemaName, IList<IntrospectTargetCatalog> CatalogList)
    {
    }
}
