namespace NoP77svk.TibcoDV.Commons
{
    using System;
    using log4net;

    public class TraceLog : IDisposable
    {
        private const string DunnoWhoAmI = "???";

        private readonly ILog? _log;
        private readonly string? _whoAmI;

        public TraceLog(ILog log, string whoAmI)
        {
            _log = log;

            if (string.IsNullOrEmpty(whoAmI))
                _whoAmI = DunnoWhoAmI;
            else
                _whoAmI = whoAmI;

            LogEnter();
        }

        public void Dispose()
        {
            LogExit();
        }

        private void LogEnter()
        {
            _log?.Debug($"Entering {_whoAmI}");
        }

        private void LogExit()
        {
            _log?.Debug($"Exiting {_whoAmI}");
        }
    }
}
