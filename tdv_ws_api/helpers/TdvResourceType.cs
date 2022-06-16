namespace NoP77svk.TibcoDV.API
{
    using System;

    public record TdvResourceType
    {
        public WSDL.Admin.resourceType WsType { get; }
        public WSDL.Admin.resourceSubType WsSubType { get; }
        public WSDL.Admin.resourceType? WsTargetType { get; }
        public WSDL.Admin.resourceSubType? WsTargetSubType { get; }

        public TdvResourceTypeEnumAgr Type { get; }

        public TdvResourceType(WSDL.Admin.resourceType type, WSDL.Admin.resourceSubType subType, WSDL.Admin.resourceType? targetType = null, WSDL.Admin.resourceSubType? targetSubType = null)
        {
            WsType = type;
            WsSubType = subType;
            WsTargetType = targetType;
            WsTargetSubType = targetSubType;

            Type = CalcResourceType(type, subType, targetType, targetSubType);
        }

        public TdvResourceType(string type, string subType, string? targetType = null)
        {
            (WsType, WsSubType, WsTargetType, WsTargetSubType) = RemapStringsToEnums(type, subType, targetType);

            Type = CalcResourceType(WsType, WsSubType, WsTargetType, WsTargetSubType);
        }

        public TdvResourceType(TdvResourceTypeEnumAgr type)
        {
            Type = type;

            (WsType, WsSubType, WsTargetType, WsTargetSubType) = CalcWsResourceTypes(Type);
        }

        /// <see cref="https://docs.tibco.com/pub/tdv/8.4.0/doc/html/StudioHelp/index.html#page/StudioHelp/Ch_3_OperationsList.TDV%2520Resource%2520Types%2520and%2520Subtypes.html"/>
        public static TdvResourceTypeEnumAgr CalcResourceType(WSDL.Admin.resourceType type, WSDL.Admin.resourceSubType subType, WSDL.Admin.resourceType? targetType, WSDL.Admin.resourceSubType? targetSubType)
        {
            return (type, subType, targetType, targetSubType) switch
            {
                (WSDL.Admin.resourceType.CONTAINER, WSDL.Admin.resourceSubType.FOLDER_CONTAINER, _, _) => TdvResourceTypeEnumAgr.Folder,
                (WSDL.Admin.resourceType.CONTAINER, WSDL.Admin.resourceSubType.CATALOG_CONTAINER, _, _) => TdvResourceTypeEnumAgr.Catalog,
                (WSDL.Admin.resourceType.CONTAINER, WSDL.Admin.resourceSubType.SCHEMA_CONTAINER, _, _) => TdvResourceTypeEnumAgr.Schema,
                (WSDL.Admin.resourceType.CONTAINER, _, _, _) => TdvResourceTypeEnumAgr.UnknownContainer,
                (WSDL.Admin.resourceType.DATA_SOURCE, WSDL.Admin.resourceSubType.RELATIONAL_DATA_SOURCE, _, _) => TdvResourceTypeEnumAgr.DataSourceRelational,
                (WSDL.Admin.resourceType.DATA_SOURCE, WSDL.Admin.resourceSubType.COMPOSITE_WEB_SERVICE, _, _) => TdvResourceTypeEnumAgr.DataSourceCompositeWebService,
                (WSDL.Admin.resourceType.DATA_SOURCE, WSDL.Admin.resourceSubType.FILE_DATA_SOURCE, _, _) => TdvResourceTypeEnumAgr.DataSourceFile,
                (WSDL.Admin.resourceType.DATA_SOURCE, WSDL.Admin.resourceSubType.POI_EXCEL_DATA_SOURCE, _, _) => TdvResourceTypeEnumAgr.DataSourceExcel,
                (WSDL.Admin.resourceType.DATA_SOURCE, WSDL.Admin.resourceSubType.XML_FILE_DATA_SOURCE, _, _) => TdvResourceTypeEnumAgr.DataSourceXmlFile,
                (WSDL.Admin.resourceType.DATA_SOURCE, WSDL.Admin.resourceSubType.WSDL_DATA_SOURCE, _, _) => TdvResourceTypeEnumAgr.DataSourceWsWsdl,
                (WSDL.Admin.resourceType.DATA_SOURCE, WSDL.Admin.resourceSubType.NONE, _, _) => TdvResourceTypeEnumAgr.UnknownDataSource,
                (WSDL.Admin.resourceType.TABLE, WSDL.Admin.resourceSubType.DATABASE_TABLE, _, _) => TdvResourceTypeEnumAgr.Table,
                (WSDL.Admin.resourceType.TABLE, WSDL.Admin.resourceSubType.SQL_TABLE, _, _) => TdvResourceTypeEnumAgr.View,
                (WSDL.Admin.resourceType.TABLE, WSDL.Admin.resourceSubType.NONE, _, _) => TdvResourceTypeEnumAgr.UnknownTable,
                (WSDL.Admin.resourceType.PROCEDURE, WSDL.Admin.resourceSubType.SQL_SCRIPT_PROCEDURE, _, _) => TdvResourceTypeEnumAgr.StoredProcedureSQL,
                (WSDL.Admin.resourceType.PROCEDURE, _, _, _) => TdvResourceTypeEnumAgr.StoredProcedureOther,
                (WSDL.Admin.resourceType.LINK, WSDL.Admin.resourceSubType.NONE, WSDL.Admin.resourceType.TABLE, _) => TdvResourceTypeEnumAgr.PublishedTableOrView,
                (WSDL.Admin.resourceType.LINK, WSDL.Admin.resourceSubType.NONE, _, _) => TdvResourceTypeEnumAgr.PublishedResource,
                (WSDL.Admin.resourceType.DEFINITION_SET, _, _, _) => TdvResourceTypeEnumAgr.DefinitionSet,
                (WSDL.Admin.resourceType.MODEL, WSDL.Admin.resourceSubType.NONE, _, _) => TdvResourceTypeEnumAgr.Model,
                (WSDL.Admin.resourceType.TRIGGER, WSDL.Admin.resourceSubType.NONE, _, _) => TdvResourceTypeEnumAgr.Trigger,
                _ => throw new ArgumentOutOfRangeException(nameof(type) + ":" + nameof(subType) + ":" + nameof(targetType) + ":" + nameof(targetSubType), $"Unrecognized combination of resource type \"{type}\", subtype \"{subType}\" and target type \"{targetType}\"/\"{targetSubType}\"")
            };
        }

        public static TdvResourceTypeEnumAgr CalcResourceType(string wsType, string wsSubType, string? wsTargetType)
        {
            (WSDL.Admin.resourceType type, WSDL.Admin.resourceSubType subType, WSDL.Admin.resourceType? targetType, WSDL.Admin.resourceSubType? targetSubType) = RemapStringsToEnums(wsType, wsSubType, wsTargetType);
            return CalcResourceType(type, subType, targetType, targetSubType);
        }

        private static ValueTuple<WSDL.Admin.resourceType, WSDL.Admin.resourceSubType, WSDL.Admin.resourceType?, WSDL.Admin.resourceSubType?> RemapStringsToEnums(string wsType, string wsSubType, string? wsTargetType = null)
        {
            WSDL.Admin.resourceType type;
            WSDL.Admin.resourceSubType subType;
            WSDL.Admin.resourceType? targetType;
            WSDL.Admin.resourceSubType? targetSubType;

            type = StringToResourceType(wsType);
            subType = StringToResourceSubType(wsSubType);

            if (wsTargetType == null)
            {
                targetType = null;
                targetSubType = null;
            }
            else
            {
                try
                {
                    targetType = StringToResourceType(wsTargetType);
                    targetSubType = null;
                }
                catch (ArgumentOutOfRangeException)
                {
                    targetType = null;
                    targetSubType = StringToResourceSubType(wsTargetType);
                }
            }

            return new (type, subType, targetType, targetSubType);
        }

        private static WSDL.Admin.resourceSubType StringToResourceSubType(string wsSubType)
        {
            WSDL.Admin.resourceSubType subType;
            try
            {
                subType = (WSDL.Admin.resourceSubType)Enum.Parse(typeof(WSDL.Admin.resourceSubType), wsSubType);
            }
            catch (ArgumentException)
            {
                throw new ArgumentOutOfRangeException(nameof(wsSubType), wsSubType);
            }

            return subType;
        }

        private static WSDL.Admin.resourceType StringToResourceType(string wsType)
        {
            WSDL.Admin.resourceType type;
            try
            {
                type = (WSDL.Admin.resourceType)Enum.Parse(typeof(WSDL.Admin.resourceType), wsType);
            }
            catch (ArgumentException)
            {
                throw new ArgumentOutOfRangeException(nameof(wsType), wsType);
            }

            return type;
        }

        public static ValueTuple<WSDL.Admin.resourceType, WSDL.Admin.resourceSubType, WSDL.Admin.resourceType?, WSDL.Admin.resourceSubType?> CalcWsResourceTypes(TdvResourceTypeEnumAgr type)
        {
            return type switch
            {
                TdvResourceTypeEnumAgr.Folder => new (WSDL.Admin.resourceType.CONTAINER, WSDL.Admin.resourceSubType.FOLDER_CONTAINER, null, null),
                TdvResourceTypeEnumAgr.Catalog => new (WSDL.Admin.resourceType.CONTAINER, WSDL.Admin.resourceSubType.CATALOG_CONTAINER, null, WSDL.Admin.resourceSubType.CATALOG_CONTAINER),
                TdvResourceTypeEnumAgr.Schema => new (WSDL.Admin.resourceType.CONTAINER, WSDL.Admin.resourceSubType.SCHEMA_CONTAINER, null, WSDL.Admin.resourceSubType.SCHEMA_CONTAINER),
                TdvResourceTypeEnumAgr.DataSourceRelational => new (WSDL.Admin.resourceType.DATA_SOURCE, WSDL.Admin.resourceSubType.RELATIONAL_DATA_SOURCE, null, null),
                TdvResourceTypeEnumAgr.DataSourceCompositeWebService => new (WSDL.Admin.resourceType.DATA_SOURCE, WSDL.Admin.resourceSubType.COMPOSITE_WEB_SERVICE, null, null),
                TdvResourceTypeEnumAgr.DataSourceFile => new (WSDL.Admin.resourceType.DATA_SOURCE, WSDL.Admin.resourceSubType.FILE_DATA_SOURCE, null, null),
                TdvResourceTypeEnumAgr.DataSourceExcel => new (WSDL.Admin.resourceType.DATA_SOURCE, WSDL.Admin.resourceSubType.POI_EXCEL_DATA_SOURCE, null, null),
                TdvResourceTypeEnumAgr.DataSourceXmlFile => new (WSDL.Admin.resourceType.DATA_SOURCE, WSDL.Admin.resourceSubType.XML_FILE_DATA_SOURCE, null, null),
                TdvResourceTypeEnumAgr.DataSourceWsWsdl => new (WSDL.Admin.resourceType.DATA_SOURCE, WSDL.Admin.resourceSubType.WSDL_DATA_SOURCE, null, null),
                TdvResourceTypeEnumAgr.Table => new (WSDL.Admin.resourceType.TABLE, WSDL.Admin.resourceSubType.DATABASE_TABLE, null, null),
                TdvResourceTypeEnumAgr.View => new (WSDL.Admin.resourceType.TABLE, WSDL.Admin.resourceSubType.SQL_TABLE, null, null),
                TdvResourceTypeEnumAgr.StoredProcedureSQL => new (WSDL.Admin.resourceType.PROCEDURE, WSDL.Admin.resourceSubType.SQL_SCRIPT_PROCEDURE, null, null),
                TdvResourceTypeEnumAgr.PublishedTableOrView => new (WSDL.Admin.resourceType.LINK, WSDL.Admin.resourceSubType.NONE, WSDL.Admin.resourceType.TABLE, null),
                TdvResourceTypeEnumAgr.PublishedStoredProcedure => new (WSDL.Admin.resourceType.LINK, WSDL.Admin.resourceSubType.NONE, WSDL.Admin.resourceType.PROCEDURE, null),
                TdvResourceTypeEnumAgr.PublishedResource => new (WSDL.Admin.resourceType.LINK, WSDL.Admin.resourceSubType.NONE, null, null),
                TdvResourceTypeEnumAgr.Model => new (WSDL.Admin.resourceType.MODEL, WSDL.Admin.resourceSubType.NONE, null, null),
                _ => throw new ArgumentOutOfRangeException(nameof(type), $"Unrecognized resource type \"{type}\"")
            };
        }
    }
}
