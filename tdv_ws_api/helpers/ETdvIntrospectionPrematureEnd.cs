namespace NoP77svk.TibcoDV.API
{
    public class ETdvIntrospectionPrematureEnd : ETdvIntrospectionError
    {
        public ETdvIntrospectionPrematureEnd(string dataSourcePath, int taskId)
            : base(dataSourcePath, taskId, "Results polling ended prematurely")
        {
        }
    }
}