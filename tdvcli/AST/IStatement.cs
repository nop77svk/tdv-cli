namespace NoP77svk.TibcoDV.CLI.AST
{
    using System.Threading.Tasks;
    using NoP77svk.TibcoDV.API;
    using NoP77svk.TibcoDV.CLI.Commons;

    internal interface IStatement
    {
        Task Execute(TdvWebServiceClient tdvClient, IInfoOutput output);
    }
}
