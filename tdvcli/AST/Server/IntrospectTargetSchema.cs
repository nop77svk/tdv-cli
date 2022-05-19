#pragma warning disable SA1313
namespace NoP77svk.TibcoDV.CLI.AST.Server
{
    using System.Collections.Generic;

    internal record IntrospectTargetSchema(string SchemaName, IList<IntrospectTargetTable> TableList)
    {
    }
}
