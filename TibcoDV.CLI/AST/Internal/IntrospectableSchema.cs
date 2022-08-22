#pragma warning disable SA1313
namespace NoP77svk.TibcoDV.CLI.AST.Internal
{
    using System.Collections.Generic;

    internal record IntrospectableSchema(string SchemaName, IEnumerable<IntrospectableObject> Objects)
    {
    }
}
