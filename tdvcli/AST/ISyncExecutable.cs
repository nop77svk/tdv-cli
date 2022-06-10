namespace NoP77svk.TibcoDV.CLI.AST
{
    using NoP77svk.TibcoDV.API;
    using NoP77svk.TibcoDV.CLI.Commons;
    using NoP77svk.TibcoDV.CLI.Parser;

    internal interface ISyncExecutable
    {
        void Execute(TdvWebServiceClient tdvClient, IInfoOutput output, ParserState parserState);
    }
}
