namespace NoP77svk.TibcoDV.CLI.AST.Client
{
    using System;
    using log4net;
    using NoP77svk.TibcoDV.API;
    using NoP77svk.TibcoDV.CLI;
    using NoP77svk.TibcoDV.CLI.AST;
    using NoP77svk.TibcoDV.CLI.Commons;
    using NoP77svk.TibcoDV.CLI.Parser;
    using NoP77svk.TibcoDV.Commons;

    internal class CommandTimer : ISyncExecutable
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Program));

        internal bool State { get; }

        internal CommandTimer(bool state)
        {
            using var log = new TraceLog(_log, nameof(CommandTimer));

            State = state;
        }

        public void Execute(TdvWebServiceClient tdvClient, IInfoOutput output, ParserState parserState)
        {
            using var log = new TraceLog(_log, nameof(Execute));

            throw new NotImplementedException();
        }
    }
}
