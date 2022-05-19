#pragma warning disable SA1313
namespace NoP77svk.TibcoDV.CLI.AST.Server
{
    using WSDL = NoP77svk.TibcoDV.API.WSDL.Admin;

    internal record ResourceSpecifier(WSDL.resourceType Type, string Path)
    {
    }
}
