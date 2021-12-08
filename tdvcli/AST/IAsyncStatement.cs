namespace NoP77svk.TibcoDV.CLI.AST
{
    using System.Threading.Tasks;
    using NoP77svk.TibcoDV.API;
    using NoP77svk.TibcoDV.CLI.Commons;

    internal interface IAsyncStatement
    {
        Task Execute(TdvWebServiceClient tdvClient, IInfoOutput output);
    }
}
