#pragma warning disable SA1313
namespace NoP77svk.TibcoDV.CLI.AST
{
    using WSDL = NoP77svk.TibcoDV.API.WSDL.Admin;

    internal record Principal(WSDL.userNameType Type, string Domain, AST.MatchBy MatchingPrincipal)
    {
    }
}
