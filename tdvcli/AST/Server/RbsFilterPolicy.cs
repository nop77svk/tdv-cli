namespace NoP77svk.TibcoDV.CLI.AST.Server
{
    internal record RbsFilterPolicy : FilterPolicy
    {
        internal RbsFilterPolicy(string policyPath)
            : base(policyPath)
        {
        }
    }
}