namespace NoP77svk.TibcoDV.QueryBenchmark
{
    using System.Collections.Generic;
    using CommandLine;
    using NoP77svk.TibcoDV.Commons;

    internal class CommandLineOptions
        : BaseCLI
    {
        [Option('f', "script", Required = true, Separator = ',', HelpText = "\n"
            + "A comma-delimited list of files with Tibco SQL queries (one query per file)")]
        public IEnumerable<string?>? PrivilegeDefinitionFiles { get; set; }

        [Option("data-source", Required = true, HelpText = "\n"
            + "Published data source name")]
        public string? TdvPublishedDataSource { get; set; }

        [Option("catalog", Required = false, HelpText = "\n"
            + "Published catalog name")]
        public string TdvPublishedCatalog { get; set; } = string.Empty;

        [Option("fetch-rows", Required = false, HelpText = "Maximum number of rows to fetch in a single client-server roundtrip")]
        public int? MaxFetchRows { get; set; }

        [Option("fetch-bytes", Required = false, HelpText = "Maximum number of bytes to fetch in a single client-server roundtrip")]
        public int? MaxFetchBytes { get; set; }
    }
}
