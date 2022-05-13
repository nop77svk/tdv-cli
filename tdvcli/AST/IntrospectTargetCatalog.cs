#pragma warning disable SA1313
namespace NoP77svk.TibcoDV.CLI.AST
{
    using System.Collections.Generic;

    internal record IntrospectTargetCatalog(string SchemaName, IList<AST.IntrospectTargetSchema> SchemaList)
    {
    }
}
