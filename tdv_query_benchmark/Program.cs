namespace NoP77svk.TibcoDV.QueryBenchmark
{
    using System;
    using System.Data.CompositeClient;
    using System.Linq;
    using System.Threading.Tasks;
    using CommandLine;
    using log4net;
    using Microsoft.Extensions.Configuration;
    using NoP77svk.TibcoDV.Commons;

    internal class Program
        : BaseProgram
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Program));

        internal static async Task<int> Main(string[] args)
        {
            InitLogging();
            using var traceLog = new TraceLog(_log, nameof(Main));

            if (_log.IsDebugEnabled)
                _log.Debug(string.Join(Environment.NewLine, args.Prepend($"{nameof(args)}:")));

            int returnCode = 127;

            try
            {
                await Parser
                    .Default
                    .ParseArguments<CommandLineOptions>(args)
                    .WithParsedAsync(async argsParsed => await MainWithParsedOptions(argsParsed));
                returnCode = 0;
            }
            catch (Exception e)
            {
                _log.Fatal("Generator failed", e);
#if DEBUG
                throw;
#else
            returnCode = 126;
#endif
            }

            _log.Debug($"{nameof(returnCode)} = {returnCode}");
            return returnCode;
        }

        internal static async Task MainWithParsedOptions(CommandLineOptions args)
        {
            using var traceLog = new TraceLog(_log, nameof(MainWithParsedOptions));

#if DEBUG
            _log.Debug("Loading user secrets");
            IConfiguration config = new ConfigurationBuilder()
                .AddUserSecrets<Program>()
                .Build();

            _log.Debug("Validating config");
            args.ValidateAndCleanUp(config.AsEnumerable());
#else
            _log.Debug("Validating config");
            args.ValidateAndCleanUp(null);
#endif

            _log.Info($"Connecting as {args.TdvServerUserName}@{args.TdvServerPrincipalDomain} to {args.TdvServerHost}:{args.TdvServerDbApiPort}, data source {args.TdvPublishedDataSource}");

            CompositeConnectionStringBuilder connectionStringBuilder = new CompositeConnectionStringBuilder()
            {
                Domain = args.TdvServerPrincipalDomain,
                User = args.TdvServerUserName,
                Password = args.TdvServerUserPassword,
                Host = args.TdvServerHost,
                Port = args.TdvServerDbApiPort ?? throw new ArgumentNullException(nameof(args) + "." + nameof(args.TdvServerDbApiPort)),
                DataSource = args.TdvPublishedDataSource,
                Catalog = args.TdvPublishedCatalog,
                DefaultCatalog = args.TdvPublishedCatalog,
                CaseSensitive = false,
                IgnoreTrailingSpaces = true,
                Encrypt = args.TdvServerWsScheme == Web.WS.ServerSchemeEnum.HTTPS,
                Readonly = true,
                StripTrailingZeros = true
            };

            if (args.MaxFetchRows != null)
                connectionStringBuilder.FetchRows = args.MaxFetchRows ?? -1;

            if (args.MaxFetchBytes != null)
                connectionStringBuilder.FetchBytes = args.MaxFetchBytes ?? -1;

            _log.Debug(connectionStringBuilder.ConnectionString);

            using CompositeConnection connection = new CompositeConnection(connectionStringBuilder.ConnectionString);
            try
            {
                await connection.OpenAsync();
            }
            finally
            {
                await connection.CloseAsync();
            }
        }
    }
}
