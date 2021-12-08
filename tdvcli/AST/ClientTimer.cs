namespace NoP77svk.TibcoDV.CLI.AST
{
    using System;
    using log4net;
    using NoP77svk.TibcoDV.API;
    using NoP77svk.TibcoDV.CLI.Commons;

    internal class ClientTimer : IStatement
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Program));

        internal bool State { get; }

        internal ClientTimer(bool state)
        {
            State = state;
        }

        public void Execute(TdvWebServiceClient tdvClient, IInfoOutput output)
        {
            throw new NotImplementedException();
        }
    }
}
