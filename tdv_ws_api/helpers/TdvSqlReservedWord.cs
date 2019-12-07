namespace NoP77svk.TibcoDV.API
{
    public class TdvSqlReservedWord
    {
        public static bool IsReserved(string? value)
        {
            // 2do! rework to some nice list of reserved words
            return value is not null
                && value.ToLower() is "type" or "value" or "position" or "year" or "from" or "select" or "where" or "domain" or "int" or "action" or "zone";
        }
    }
}
