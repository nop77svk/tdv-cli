#pragma warning disable SA1313
namespace NoP77svk.TibcoDV.API
{
    using System;

    public record TdvResourceType
    {
        public string WsType { get; }
        public string? WsSubType { get; }
        public string? WsTargetType { get; }

        public TdvResourceTypeEnumAgr Type { get; }

        public TdvResourceType(string wsType, string? wsSubType, string? wsTargetType = null)
        {
            WsType = wsType;
            WsSubType = wsSubType;
            WsTargetType = wsTargetType;

            Type = CalcResourceType(WsType, WsSubType, WsTargetType);
        }

        public TdvResourceType(TdvResourceTypeEnumAgr type)
        {
            Type = type;

            (WsType, WsSubType, WsTargetType) = CalcWsResourceTypes(Type);
        }

        /// <see cref="https://docs.tibco.com/pub/tdv/8.4.0/doc/html/StudioHelp/index.html#page/StudioHelp/Ch_3_OperationsList.TDV%2520Resource%2520Types%2520and%2520Subtypes.html"/>
        public static TdvResourceTypeEnumAgr CalcResourceType(string? wsType, string? wsSubType, string? wsTargetType)
        {
            // 2do! cache the mapping via Dictionary<Tuple<string?, string?, string?>, TdvResourceTypeEnum>
            return (wsType, wsSubType, wsTargetType) switch
            {
                (TdvResourceTypeConst.Container, TdvResourceSubtypeConst.FolderContainer, _) => TdvResourceTypeEnumAgr.Folder,
                (TdvResourceTypeConst.Container, TdvResourceSubtypeConst.CatalogContainer, _) => TdvResourceTypeEnumAgr.PublishedCatalog,
                (TdvResourceTypeConst.Container, TdvResourceSubtypeConst.SchemaContainer, _) => TdvResourceTypeEnumAgr.PublishedSchema,
                (TdvResourceTypeConst.Container, _, _) => TdvResourceTypeEnumAgr.UnknownContainer,
                (TdvResourceTypeConst.DataSource, TdvResourceSubtypeConst.RelationalDataSource, _) => TdvResourceTypeEnumAgr.DataSourceRelational,
                (TdvResourceTypeConst.DataSource, TdvResourceSubtypeConst.CompositeWebService, _) => TdvResourceTypeEnumAgr.DataSourceCompositeWebService,
                (TdvResourceTypeConst.DataSource, TdvResourceSubtypeConst.FileDataSource, _) => TdvResourceTypeEnumAgr.DataSourceFile,
                (TdvResourceTypeConst.DataSource, TdvResourceSubtypeConst.PoiExcelDataSource, _) => TdvResourceTypeEnumAgr.DataSourceExcel,
                (TdvResourceTypeConst.DataSource, TdvResourceSubtypeConst.XmlFileDataSource, _) => TdvResourceTypeEnumAgr.DataSourceXmlFile,
                (TdvResourceTypeConst.DataSource, TdvResourceSubtypeConst.WsdlDataSource, _) => TdvResourceTypeEnumAgr.DataSourceWsWsdl,
                (TdvResourceTypeConst.Table, TdvResourceSubtypeConst.DatabaseTable, _) => TdvResourceTypeEnumAgr.Table,
                (TdvResourceTypeConst.Table, TdvResourceSubtypeConst.SqlTable, _) => TdvResourceTypeEnumAgr.View,
                (TdvResourceTypeConst.Procedure, TdvResourceSubtypeConst.SqlScriptProcedure, _) => TdvResourceTypeEnumAgr.StoredProcedureSQL,
                (TdvResourceTypeConst.Procedure, _, _) => TdvResourceTypeEnumAgr.StoredProcedureOther,
                (TdvResourceTypeConst.Link, TdvResourceSubtypeConst.None, TdvResourceTypeConst.Table) => TdvResourceTypeEnumAgr.PublishedTableOrView,
                (TdvResourceTypeConst.DefinitionSet, _, _) => TdvResourceTypeEnumAgr.DefinitionSet,
                (TdvResourceTypeConst.Model, TdvResourceSubtypeConst.None, _) => TdvResourceTypeEnumAgr.Model,
                _ => throw new ArgumentOutOfRangeException(nameof(wsType) + ":" + nameof(wsSubType) + ":" + nameof(wsTargetType), $"Unrecognized combination of resource type \"{wsType}\", subtype \"{wsSubType}\" and target type \"{wsTargetType}\"")
            };
        }

        public static ValueTuple<string, string?, string?> CalcWsResourceTypes(TdvResourceTypeEnumAgr type)
        {
            return type switch
            {
                TdvResourceTypeEnumAgr.Folder => new (TdvResourceTypeConst.Container, TdvResourceSubtypeConst.FolderContainer, null),
                TdvResourceTypeEnumAgr.PublishedCatalog => new (TdvResourceTypeConst.Container, TdvResourceSubtypeConst.CatalogContainer, null),
                TdvResourceTypeEnumAgr.PublishedSchema => new (TdvResourceTypeConst.Container, TdvResourceSubtypeConst.SchemaContainer, null),
                TdvResourceTypeEnumAgr.DataSourceRelational => new (TdvResourceTypeConst.DataSource, TdvResourceSubtypeConst.RelationalDataSource, null),
                TdvResourceTypeEnumAgr.DataSourceCompositeWebService => new (TdvResourceTypeConst.DataSource, TdvResourceSubtypeConst.CompositeWebService, null),
                TdvResourceTypeEnumAgr.DataSourceFile => new (TdvResourceTypeConst.DataSource, TdvResourceSubtypeConst.FileDataSource, null),
                TdvResourceTypeEnumAgr.DataSourceExcel => new (TdvResourceTypeConst.DataSource, TdvResourceSubtypeConst.PoiExcelDataSource, null),
                TdvResourceTypeEnumAgr.DataSourceXmlFile => new (TdvResourceTypeConst.DataSource, TdvResourceSubtypeConst.XmlFileDataSource, null),
                TdvResourceTypeEnumAgr.DataSourceWsWsdl => new (TdvResourceTypeConst.DataSource, TdvResourceSubtypeConst.WsdlDataSource, null),
                TdvResourceTypeEnumAgr.Table => new (TdvResourceTypeConst.Table, TdvResourceSubtypeConst.DatabaseTable, null),
                TdvResourceTypeEnumAgr.View => new (TdvResourceTypeConst.Table, TdvResourceSubtypeConst.SqlTable, null),
                TdvResourceTypeEnumAgr.StoredProcedureSQL => new (TdvResourceTypeConst.Procedure, TdvResourceSubtypeConst.SqlScriptProcedure, null),
                TdvResourceTypeEnumAgr.PublishedTableOrView => new (TdvResourceTypeConst.Link, TdvResourceSubtypeConst.None, null),
                TdvResourceTypeEnumAgr.Model => new (TdvResourceTypeConst.Model, TdvResourceSubtypeConst.None, null),
                _ => throw new ArgumentOutOfRangeException(nameof(type), $"Unrecognized resource type \"{type}\"")
            };
        }
    }
}
