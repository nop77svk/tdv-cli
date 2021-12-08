namespace NoP77svk.TibcoDV.CLI.AST
{
    using NoP77svk.TibcoDV.API;

    internal interface IStatement
    {
        void Execute(TdvWebServiceClient tdvClient);
    }
}
