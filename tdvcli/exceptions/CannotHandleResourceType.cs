namespace NoP77svk.TibcoDV.CLI
{
    using System;
    using NoP77svk.TibcoDV.API;

    internal class CannotHandleResourceType : Exception
    {
        public API.WSDL.Admin.resourceType Type { get; }
        public API.WSDL.Admin.resourceSubType? SubType { get; } = null;
        public API.WSDL.Admin.resourceType? TargetType { get; } = null;

        public CannotHandleResourceType(TdvResourceType type)
            : base($"Don't know how to handle resource type \"{type.WsType}\", subtype \"{type.WsSubType}\", target type \"{type.WsTargetType}\"")
        {
            Type = type.WsType;
            SubType = type.WsSubType;
            TargetType = type.WsTargetType;
        }

        public CannotHandleResourceType(API.WSDL.Admin.resourceType type)
            : base($"Don't know how to handle resource type \"{type}\"")
        {
            Type = type;
        }

        public CannotHandleResourceType(API.WSDL.Admin.resourceType type, API.WSDL.Admin.resourceSubType subType)
            : base($"Don't know how to handle resource type \"{type}\", subtype \"{subType}\"")
        {
            Type = type;
            SubType = subType;
        }

        public CannotHandleResourceType(API.WSDL.Admin.resourceType type, API.WSDL.Admin.resourceSubType subType, API.WSDL.Admin.resourceType targetType)
            : base($"Don't know how to handle resource type \"{type}\", subtype \"{subType}\", target type \"{targetType}\"")
        {
            Type = type;
            SubType = subType;
            TargetType = targetType;
        }
    }
}
