namespace NoP77svk.TibcoDV.CLI.AST
{
    using System.Threading.Tasks;
    using NoP77svk.TibcoDV.API;
    using NoP77svk.TibcoDV.CLI.Commons;
    using NoP77svk.TibcoDV.CLI.Parser;

    internal interface IAsyncExecutable
    {
        Task Execute(TdvWebServiceClient tdvClient, IInfoOutput output, ParserState parserState);
    }
}
