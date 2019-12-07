namespace NoP77svk.TibcoDV.Commons
{
    using System;
    using System.Collections.Generic;
    using CommandLine;
    using log4net;
    using NoP77svk.IO;
    using NoP77svk.TibcoDV.API;

    public class InputOutputCLI
        : BaseCLI
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(InputOutputCLI));

        [Option('i', "input", Required = true, HelpText = "\n"
            + "Source TDV path to read table/view structures from")]
        public string? InputTdvPath { get; set; }

        [Option('o', "output", Required = true, HelpText = "\n"
            + "Target TDV path")]
        public string? OutputTdvPath { get; set; }

        [Option("purge-output-first", Required = false, Default = false, HelpText = "\n"
            + "Purge the target TDV path of all views prior to generating the L1 views")]
        public bool PurgeOutputPathFirst { get; set; }

        // ----------------------------------------------------------------------------------------
        // cleaned-up CLI arguments
        // ----------------------------------------------------------------------------------------
        public string InputTdvPathSanitized => PathExt.Sanitize(InputTdvPath, TdvWebServiceClient.FolderDelimiter) ?? string.Empty;
        public string OutputTdvPathSanitized => PathExt.Sanitize(OutputTdvPath, TdvWebServiceClient.FolderDelimiter) ?? string.Empty;

        // ----------------------------------------------------------------------------------------
        // validation and clean up routines
        // ----------------------------------------------------------------------------------------
        public override void ValidateAndCleanUp(IEnumerable<KeyValuePair<string, string>>? defaults = null)
        {
            using var log = new TraceLog(_log, nameof(ValidateAndCleanUp));

            base.ValidateAndCleanUp(defaults);

            if (string.IsNullOrWhiteSpace(InputTdvPath))
                throw new ArgumentNullException(null, "Input TDV path must not be empty");
            else if (!InputTdvPath.StartsWith('/'))
                throw new ArgumentException("Input TDV path must start with /");

            if (string.IsNullOrWhiteSpace(OutputTdvPath))
                throw new ArgumentNullException(null, "Output TDV path must not be empty");
            else if (!OutputTdvPath.StartsWith('/'))
                throw new ArgumentException("Output TDV path must start with /");
        }
    }
}
