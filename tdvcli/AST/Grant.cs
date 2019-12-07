namespace NoP77svk.TibcoDV.CLI.AST
{
    using System.Collections.Generic;
    using NoP77svk.TibcoDV.API;
    using NoP77svk.TibcoDV.API.WSDL.Admin;

    internal record Grant
    {
        public bool IsRecursive { get; init; } = false;
        public updatePrivilegesMode ModusOperandi { get; init; } = updatePrivilegesMode.OVERWRITE_APPEND;
        public IList<TdvPrivilegeEnum>? Privileges { get; init; }
        public IList<ResourceSpecifier>? Resources { get; init; }
        public IList<Principal>? Principals { get; init; }
    }
}
