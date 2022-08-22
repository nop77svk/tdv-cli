#pragma warning disable SA1313
namespace NoP77svk.TibcoDV.CLI.AST.Internal
{
    using System.Collections.Generic;

    internal record IntrospectableDataSource(string DataSource, IEnumerable<IntrospectableCatalog> Catalogs)
    {
    }
}
