namespace NoP77svk.TibcoDV.API
{
    using NoP77svk.Web.WS;

    public static class RestWsEndpointExt4Tdv
    {
        public static JsonRestWsEndpoint AddTdvQuery(this JsonRestWsEndpoint self, string? key, bool? value)
        {
            return self.AddQuery(key, value?.ToString().ToLower());
        }
    }
}
