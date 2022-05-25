namespace NoP77svk.TibcoDV.API
{
    public class ETdvIntrospectionFailed : ETdvIntrospectionError
    {
        public ETdvIntrospectionFailed(string dataSource, int introspectionTaskId)
            : base(dataSource, introspectionTaskId, "Failed")
        {
        }
    }
}
