#pragma warning disable SA1313
namespace NoP77svk.TibcoDV.CLI.AST
{
    using WSDL = NoP77svk.TibcoDV.API.WSDL.Admin;

    internal record CommandAssign(WSDL.rbsAssignmentOperationType Action, AssignWhat What);
}
