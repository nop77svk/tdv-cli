﻿namespace NoP77svk.TibcoDV.CLI.AST
{
    using log4net;
    using NoP77svk.TibcoDV.API;
    using NoP77svk.TibcoDV.CLI.Commons;
    using NoP77svk.TibcoDV.Commons;

    internal class ClientPrompt : IStatement
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Program));

        internal string PromptText { get; }

        internal ClientPrompt(string promptText)
        {
            PromptText = promptText;
        }

        public void Execute(TdvWebServiceClient tdvClient, IInfoOutput output)
        {
            using var log = new TraceLog(_log, nameof(Execute));

            output.Info(PromptText);
        }
    }
}
