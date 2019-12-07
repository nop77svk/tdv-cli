namespace NoP77svk.TibcoDV.CLI.AST
{
    using NoP77svk.TibcoDV.API.WSDL.Admin;

    internal record Assign
    {
        public rbsAssignmentOperationType Action { get; init; }
        public AssignWhat? What { get; init; }
    }
}
