namespace NoP77svk.TibcoDV.CLI.AST.Client
{
    using System;
    using log4net;
    using NoP77svk.TibcoDV.API;
    using NoP77svk.TibcoDV.CLI;
    using NoP77svk.TibcoDV.CLI.AST;
    using NoP77svk.TibcoDV.CLI.Commons;
    using NoP77svk.TibcoDV.Commons;

    internal class CommandConnectionTimeout : ISyncExecutable
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Program));

        internal Infra.TimeSpan TimeSpan { get; }

        internal CommandConnectionTimeout(Infra.TimeSpan timeSpan)
        {
            using var log = new TraceLog(_log, nameof(CommandConnectionTimeout));

            TimeSpan = timeSpan;
        }

        public void Execute(TdvWebServiceClient tdvClient, IInfoOutput output)
        {
            using var log = new TraceLog(_log, nameof(Execute));

            throw new NotImplementedException();
        }
    }
}
