namespace NoP77svk.TibcoDV.CLI.AST
{
    using NoP77svk.TibcoDV.API;
    using NoP77svk.TibcoDV.CLI.Commons;

    internal interface ISyncExecutable
    {
        void Execute(TdvWebServiceClient tdvClient, IInfoOutput output, ParserState parserState);
    }
}
