namespace NoP77svk.TibcoDV.CLI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using CommandLine;
    using log4net;
#if DEBUG
    using Microsoft.Extensions.Configuration;
#endif
    using NoP77svk.IO;
    using NoP77svk.Linq;
    using NoP77svk.Text.RegularExpressions;
    using NoP77svk.TibcoDV.API;
    using NoP77svk.TibcoDV.CLI.AST;
    using NoP77svk.TibcoDV.CLI.Commons;
    using NoP77svk.TibcoDV.Commons;
    using NoP77svk.Web.WS;
    using Pegasus.Common;
    using WSDL = NoP77svk.TibcoDV.API.WSDL;

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

            _out.Info($"Connecting as {args.TdvServerUserName} to {args.TdvServerWsScheme}://{args.TdvServerHost}:{args.TdvServerWsApiPort}");
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

            ScriptFileParser fileParser = new ScriptFileParser();
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
                    string error = error = FormatParserError(statement, e);
                    _log.Error(error, e);
                    _out.Error(error, e);

                    throw new StatementParseException(statement.FileName, statement.FileLine, statement.Statement, e.Message, e);
                }

                if (!args.DryRun)
                    await ExecuteParsedStatement(tdvClient, commandAST);
                else
                    _out.Info(commandAST?.ToString() ?? "(null command)");
            }

            _out.Info("All done");
        }

        private static async Task ExecuteParsedStatement(TdvWebServiceClient tdvClient, object commandAST)
        {
            using var log = new TraceLog(_log, nameof(ExecuteParsedStatement));

            if (commandAST is AST.IAsyncStatement stmtAsync)
                await stmtAsync.Execute(tdvClient, _out);
            else if (commandAST is AST.IStatement stmt)
                stmt.Execute(tdvClient, _out);
            else
                throw new ArgumentOutOfRangeException(nameof(commandAST), commandAST?.GetType() + " :: " + commandAST?.ToString(), "Unrecognized type of parsed statement");
        }

        private static string FormatParserError(ScriptFileParserOutPOCO statement, FormatException e)
        {
            string error;
            Cursor? ec = null;
            if (e.Data["cursor"] is Cursor)
                ec = (Cursor?)e.Data["cursor"];

            if (ec is not null)
                error = $"File {statement.FileName}, line {statement.FileLine}, statement line {ec.Line}, column {ec.Column}\":\n{statement.Statement}";
            else
                error = $"File {statement.FileName}, line {statement.FileLine}, failed to parse:\n{statement}";
            return error;
        }
    }
}