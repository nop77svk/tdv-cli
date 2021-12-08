namespace NoP77svk.TibcoDV.CLI.AST
{
    using NoP77svk.TibcoDV.API;
    using NoP77svk.TibcoDV.CLI.Commons;

    internal interface IStatement
    {
        void Execute(TdvWebServiceClient tdvClient, IInfoOutput output);
    }
}
