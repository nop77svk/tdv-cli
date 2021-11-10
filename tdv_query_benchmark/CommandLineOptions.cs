namespace NoP77svk.TibcoDV.QueryBenchmark
{
    using System.Collections.Generic;
    using CommandLine;
    using NoP77svk.TibcoDV.Commons;

    internal class CommandLineOptions
        : BaseCLI
    {
        [Option('f', "script", Required = true, Separator = ',', HelpText = "\n"
            + "A comma-delimited list of \"SQL\" scripts")]
        public IEnumerable<string?>? PrivilegeDefinitionFiles { get; set; }
    }
}
