namespace NoP77svk.TibcoDV.CLI.AST
{
    using System;
    using log4net;
    using NoP77svk.TibcoDV.API;
    using NoP77svk.TibcoDV.CLI.Commons;
    using NoP77svk.TibcoDV.Commons;

    internal class ClientSet : IStatement
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Program));

        internal string VarName { get; }
        internal object VarValue { get; }

        internal ClientSet(string varName, object varValue)
        {
            using var log = new TraceLog(_log, nameof(ClientSet));

            VarName = varName;
            VarValue = varValue;
        }

        public void Execute(TdvWebServiceClient tdvClient, IInfoOutput output)
        {
            using var log = new TraceLog(_log, nameof(Execute));

            throw new NotImplementedException();
        }
    }
}
