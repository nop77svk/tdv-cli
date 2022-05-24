namespace NoP77svk.TibcoDV.CLI
{
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using CommandLine;
    using log4net;
#if DEBUG
    using Microsoft.Extensions.Configuration;
#endif
    using NoP77svk.IO;
    using NoP77svk.TibcoDV.API;
    using NoP77svk.TibcoDV.CLI.Commons;
    using NoP77svk.TibcoDV.Commons;
    using NoP77svk.Web.WS;
    using Pegasus.Common;

    internal class Program
        : BaseProgram
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Program));

        private static readonly IInfoOutput _out = new ConsoleInfoOutput(writeToStdErr: true);

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
            catch (StatementParseException e)
            {
                _log.Error(e.FailedCommand, e);

                string error = FormatParserError(e);
                _out.Error(error);

                returnCode = 125;
            }
            catch (Exception e)
            {
                _log.Fatal(e);
                _out.Error("Fatal error!");
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

            _log.Debug($"{nameof(TdvWebServiceClient)}.{nameof(TdvWebServiceClient.FolderDelimiter)} = {TdvWebServiceClient.FolderDelimiter}");
            PathExt.FolderDelimiter = TdvWebServiceClient.FolderDelimiter;

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

            _out.InfoNoEoln($"Connecting as {args.TdvServerUserName} to {args.TdvServerWsScheme}://{args.TdvServerHost}:{args.TdvServerWsApiPort}...");
            using HttpClient httpClient = InitHttpConnectionPool(args);

            TdvWebServiceClient tdvClient = InitTdvRestClient(
                httpClient,
                args,
                1,
                obj =>
                {
                    // 2do! do this in a more elegant way somehow!
                    _log.Debug("Injecting credentials into HTTP request");
                    obj.Headers.Authorization = HttpWebServiceClient.GetHeaderForBasicAuthentication(args.TdvServerUserName, args.TdvServerUserPassword);

                    DebugLogHttpRequest(obj);
                },
                DebugLogHttpResponse
            );

            if (!args.DryRun)
            {
                await tdvClient.BeginSession();
                _out.Info(" Connected");
            }
            else
            {
                _out.Info(string.Empty);
            }

            try
            {
                ParserState parserState = new ParserState()
                {
                    CommandDelimiter = ";"
                };
                ScriptFileParser fileParser = new ScriptFileParser(() => parserState.CommandDelimiter);
                PierresTibcoSqlParser sqlParser = new PierresTibcoSqlParser();

                // do your stuff
                foreach (ScriptFileParserOutPOCO statement in fileParser.SplitScriptsToStatements(args.PrivilegeDefinitionFiles))
                {
                    _log.Debug(statement);
                    object commandAST;

                    try
                    {
                        commandAST = sqlParser.Parse(statement.Statement);
                    }
                    catch (FormatException e)
                    {
                        throw new StatementParseException(statement.FileName, statement.FileLine, statement.Statement, e.Message, e);
                    }

                    if (args.DryRun)
                        _out.Info(commandAST?.ToString() ?? "(null command)");
                    else
                        await ExecuteParsedStatement(tdvClient, commandAST, parserState);
                }
            }
            finally
            {
                if (!args.DryRun)
                {
                    await tdvClient.CloseSession();
                }
            }

            _out.Info("All done");
        }

        private static async Task ExecuteParsedStatement(TdvWebServiceClient tdvClient, object commandAST, ParserState parserState)
        {
            using var log = new TraceLog(_log, nameof(ExecuteParsedStatement));

            if (commandAST is AST.IAsyncExecutable stmtAsync)
                await stmtAsync.Execute(tdvClient, _out, parserState);
            else if (commandAST is AST.ISyncExecutable stmt)
                stmt.Execute(tdvClient, _out, parserState);
            else
                throw new ArgumentOutOfRangeException(nameof(commandAST), commandAST?.GetType() + " :: " + commandAST?.ToString(), "Unrecognized type of parsed statement");
        }

        private static string FormatParserError(StatementParseException e)
        {
            StringBuilder error = new StringBuilder();

            error.Append($"Parse error in file {e.FileName}, line {e.FileLine}");

            if (e.InnerException is FormatException && e.InnerException?.Data["cursor"] is Cursor)
            {
                Cursor? ec = (Cursor?)e.InnerException.Data["cursor"];
                if (ec is not null)
                    error.Append($", statement line {ec.Line}, column {ec.Column}");
            }

            error.Append($" - {e.Message}:\n{e.FailedCommand}");

            return error.ToString();
        }
    }
}