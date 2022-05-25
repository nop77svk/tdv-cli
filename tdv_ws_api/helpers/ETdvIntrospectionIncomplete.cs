namespace NoP77svk.TibcoDV.API
{
    public class ETdvIntrospectionIncomplete : ETdvIntrospectionError
    {
        public ETdvIntrospectionIncomplete(string dataSourcePath, int taskId)
            : base(dataSourcePath, taskId, "Incomplete")
        {
        }
    }
}