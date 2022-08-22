namespace NoP77svk.TibcoDV.API.PolledServerTasks
{
    using System.Collections.Generic;

    public interface IPolledServerTaskEnumerableHandler<TResponse, TResult>
        : IPolledServerTaskHandler<TResponse>
    {
        IEnumerable<TResult>? ExtractResults(TResponse response);
    }
}
