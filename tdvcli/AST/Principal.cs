namespace NoP77svk.TibcoDV.CLI.AST
{
    using NoP77svk.TibcoDV.API.WSDL.Admin;

    internal record Principal
    {
        internal userNameType? Type { get; init; }
        internal string? Name { get; init; }
        internal string? Domain { get; init; }
        internal LookupOperatorEnum LookupOperator { get; init; } = LookupOperatorEnum.EqualTo;
        internal GrantMatchStrictnessEnum LookupStrictness { get; init; } = GrantMatchStrictnessEnum.Relaxed;
    }
}
