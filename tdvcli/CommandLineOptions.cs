namespace NoP77svk.TibcoDV.CLI
{
    using System.Collections.Generic;
    using CommandLine;
    using NoP77svk.TibcoDV.Commons;

    internal class CommandLineOptions
        : BaseCLI
    {
        [Option('f', "privilege-scripts", Required = true, Separator = ',', HelpText = "\n"
            + "A comma-delimited list of privilege assignment CSVs")]
        public IEnumerable<string?>? PrivilegeDefinitionFiles { get; set; }
    }
}
