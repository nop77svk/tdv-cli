namespace NoP77svk.TibcoDV.CLI.AST
{
    using System.Collections.Generic;
    using NoP77svk.TibcoDV.API;
    using WSDL = NoP77svk.TibcoDV.API.WSDL.Admin;

    internal class CommandGrant
    {
        internal bool IsRecursive { get; }
        internal WSDL.updatePrivilegesMode ModusOperandi { get; }
        internal IList<TdvPrivilegeEnum> Privileges { get; }
        internal IList<ResourceSpecifier> Resources { get; }
        internal IList<Principal> Principals { get; }

        internal CommandGrant(bool isRecursive, WSDL.updatePrivilegesMode modusOperandi, IList<TdvPrivilegeEnum> privileges, IList<ResourceSpecifier> resources, IList<Principal> principals)
        {
            IsRecursive = isRecursive;
            ModusOperandi = modusOperandi;
            Privileges = privileges;
            Resources = resources;
            Principals = principals;
        }
    }
}
