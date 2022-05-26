namespace NoP77svk.TibcoDV.API
{
    using System;
    using System.Threading;

    public record PolledServerTaskOptions<TResponse>
    {
        public Action<TResponse>? responseFeedback { get; init; } = null;
        public CancellationToken? cancellationToken { get; init; } = null;
    }
}
