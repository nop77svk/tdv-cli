#pragma warning disable SA1313
namespace NoP77svk.TibcoDV.CLI.AST
{
    using System.Collections.Generic;

    internal record IntrospectTargetDataSource(string SchemaName, IList<AST.IntrospectTargetCatalog> CatalogList)
    {
    }
}
