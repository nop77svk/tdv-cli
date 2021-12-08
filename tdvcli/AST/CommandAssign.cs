#pragma warning disable SA1313
namespace NoP77svk.TibcoDV.CLI.AST
{
    using NoP77svk.TibcoDV.API.WSDL.Admin;
    using WSDL = NoP77svk.TibcoDV.API.WSDL.Admin;

    internal class CommandAssign
    {
        internal WSDL.rbsAssignmentOperationType Action { get; }
        internal AssignWhat What { get; }

        internal CommandAssign(rbsAssignmentOperationType action, AssignWhat what)
        {
            Action = action;
            What = what;
        }
    }
}
