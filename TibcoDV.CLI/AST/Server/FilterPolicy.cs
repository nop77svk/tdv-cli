namespace NoP77svk.TibcoDV.CLI.AST.Server
{
    internal record FilterPolicy
    {
        public string PolicyPath { get; }

        internal FilterPolicy(string policyPath)
        {
            PolicyPath = policyPath;
        }
    }
}