namespace NoP77svk.TibcoDV.Commons
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using CommandLine;
    using log4net;
    using NoP77svk.Text;

    public abstract class ColumnProcessingCLI
        : InputOutputCLI
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ColumnProcessingCLI));

        [Option('n', "naming", Required = false, Default = "original", HelpText = "\n"
            + "Case for the L1 views naming (upper|lower|original)")]

        public string? ViewNamingCaseDesc { get; set; }
        [Option('x', "exclude-columns", Separator = ',', Required = false, HelpText = "Comma-delimited list of regular expressions\n"
            + "Exclude source table columns matching any of the regexps.\n"
            + "Example: -x /^t_bita_/i,/^t_gdl_/i,/^dax_generation$/i,/^num_mandant$/i\n"
            + "Note: Quite logically, since comma is a list delimiter here, you cannot use comma anywhere in your regular expressions.")]

        public IEnumerable<string>? ExcludeColumnsRxStr { get; set; }

        [Option('c', "include-columns", Separator = ',', Required = false, HelpText = "Comma-delimited list of regular expressions\n"
            + "Include source table columns matching any of the regexps even when the columns were excluded by --exclude-columns.\n"
            + "Example: -c /^t_bita_.*_p/i\n"
            + "Note: Quite logically, since comma is a list delimiter here, you cannot use comma anywhere in your regular expressions.")]
        public IEnumerable<string>? IncludeColumnsRxStr { get; set; }

        // ----------------------------------------------------------------------------------------
        // cleaned-up CLI arguments
        // ----------------------------------------------------------------------------------------
        public IdentifierCaseEnum ViewNamingCase { get; set; }
        public ICollection<Regex> ExcludeColumnsRx { get; set; } = new List<Regex>();
        public ICollection<Regex> IncludeColumnsRx { get; set; } = new List<Regex>();

        // ----------------------------------------------------------------------------------------
        // validation and clean up routines
        // ----------------------------------------------------------------------------------------
        public override void ValidateAndCleanUp(IEnumerable<KeyValuePair<string, string>>? defaults = null)
        {
            using var log = new TraceLog(_log, nameof(ValidateAndCleanUp));

            base.ValidateAndCleanUp(defaults);

            ViewNamingCase = ViewNamingCaseDesc?.ToLower() switch
            {
                null or "" or "mixed" or "mix" or "auto" or "original" or "orig" => IdentifierCaseEnum.Original,
                "lower" or "lowercase" or "low" or "lc" => IdentifierCaseEnum.Lower,
                "upper" or "uppercase" or "up" or "uc" => IdentifierCaseEnum.Upper,
                _ => throw new ArgumentOutOfRangeException($"Unrecognized parameter \"output-case\" value {ViewNamingCaseDesc}")
            };

            // split and parse "exclude/include columns" regexps
            if (ExcludeColumnsRxStr != null)
            {
                ExcludeColumnsRx = ExcludeColumnsRxStr
                    .Select(rxStr => SlashedRegexpExt.ParseSlashedRegexp(rxStr, RegexOptions.Compiled))
                    .ToList();
            }

            if (IncludeColumnsRxStr != null)
            {
                IncludeColumnsRx = IncludeColumnsRxStr
                    .Select(rxStr => SlashedRegexpExt.ParseSlashedRegexp(rxStr, RegexOptions.Compiled))
                    .ToList();
            }
        }
    }
}
