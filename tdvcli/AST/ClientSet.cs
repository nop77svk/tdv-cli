#pragma warning disable SA1313
namespace NoP77svk.TibcoDV.CLI.AST
{
    internal class ClientSet
    {
        internal string VarName { get; }
        internal object VarValue { get; }

        internal ClientSet(string varName, object varValue)
        {
            VarName = varName;
            VarValue = varValue;
        }
    }
}
