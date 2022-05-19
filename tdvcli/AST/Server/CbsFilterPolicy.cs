namespace NoP77svk.TibcoDV.CLI.AST.Server
{
    internal record CbsFilterPolicy : FilterPolicy
    {
        internal CbsFilterPolicy(string policyPath)
            : base(policyPath)
        {
        }
    }
}