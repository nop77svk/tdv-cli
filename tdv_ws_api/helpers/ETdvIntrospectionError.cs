namespace NoP77svk.TibcoDV.API
{
    using System;

    public class ETdvIntrospectionError : Exception
    {
        public string DataSource { get; }
        public int IntrospectionTaskId { get; }
        public string? ErrorReason { get; }

        public ETdvIntrospectionError(string dataSource, int introspectionTaskId)
            : base($"Error introspecting data source {dataSource} (task {introspectionTaskId})")
        {
            DataSource = dataSource;
            IntrospectionTaskId = introspectionTaskId;
            ErrorReason = null;
        }

        public ETdvIntrospectionError(string dataSource, int introspectionTaskId, string reason)
            : base($"Error introspecting data source {dataSource} (task {introspectionTaskId}): {reason}")
        {
            DataSource = dataSource;
            IntrospectionTaskId = introspectionTaskId;
            ErrorReason = reason;
        }
    }
}
