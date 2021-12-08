namespace NoP77svk.TibcoDV.CLI.AST
{
    internal class CommandCreateResource
    {
        internal bool IfNotExists { get; }
        internal object ResourceDDL { get; }

        internal CommandCreateResource(bool ifNotExists, object resourceDDL)
        {
            IfNotExists = ifNotExists;
            ResourceDDL = resourceDDL;
        }
    }
}
