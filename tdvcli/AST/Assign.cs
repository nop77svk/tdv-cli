namespace NoP77svk.TibcoDV.CLI.AST
{
    using WSDL = NoP77svk.TibcoDV.API.WSDL.Admin;

    internal record Assign
    {
        public WSDL.rbsAssignmentOperationType Action { get; init; }
        public AssignWhat? What { get; init; }
    }
}
