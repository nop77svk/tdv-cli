#pragma warning disable SA1313
namespace NoP77svk.TibcoDV.CLI.AST.Infra
{
    internal record MatchExactly(string Value) : MatchBy(Value)
    {
    }
}
