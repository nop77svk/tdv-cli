namespace NoP77svk.TibcoDV.CLI.AST
{
    using System;
    using log4net;
    using NoP77svk.TibcoDV.API;
    using NoP77svk.TibcoDV.CLI.Commons;

    internal class ClientConnectionTimeout : IStatement
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Program));

        internal TimeSpan TimeSpan { get; }

        internal ClientConnectionTimeout(TimeSpan timeSpan)
        {
            TimeSpan = timeSpan;
        }

        public void Execute(TdvWebServiceClient tdvClient, IInfoOutput output)
        {
            throw new NotImplementedException();
        }
    }
}
