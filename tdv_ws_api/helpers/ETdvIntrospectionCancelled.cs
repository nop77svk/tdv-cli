namespace NoP77svk.TibcoDV.API
{
    public class ETdvIntrospectionCancelled : ETdvIntrospectionError
    {
        public ETdvIntrospectionCancelled(string dataSource, int introspectionTaskId)
            : base(dataSource, introspectionTaskId, "Cancelled")
        {
        }
    }
}
