namespace NoP77svk.TibcoDV.Commons
{
    using System;
    using System.Collections.Generic;
    using CommandLine;
    using log4net;
    using NoP77svk.Console;
    using NoP77svk.Data.Utils;
    using NoP77svk.Web.WS;

    public abstract class BaseCLI
    {
        public const string DefaultPrincipalDomain = "composite";

        private static readonly ILog _log = LogManager.GetLogger(typeof(BaseCLI));

        [Option('u', "user", Required = true, HelpText = "\n"
            + "Full connection string to TDV server\n"
            + "[<user_domain>:]<username>[/<password>]@<server_host>[:<server_port>])]")]
        public string? TdvServerConnectionString { get; set; }

        [Option("use-ssl", Required = false, Default = "auto", HelpText = "\n"
            + "Force use HTTPS instead of HTTP (auto|yes|no)\n"
            + "\"auto\" = Autodetect based on the --port argument (9402, 9403 = yes, 9400, 9401 = no)")]
        public string? UseSSL { get; set; }

        [Option("ignore-invalid-ssl-certificates", Required = false, Default = false, HelpText = "\n"
            + "Disables checking for validity of SSL certificates\n"
            + "Note: Valid only when --use-ssl=yes is in effect")]
        public bool IgnoreInvalidSslCertificates { get; set; }

        [Option("proxy", Required = false, HelpText = "Full HTTP/HTTPS proxy connection string\n"
            + "When ommitted = Use whatever OS-level configuration of proxy server")]
        public string? HttpProxy { get; set; }

        [Option("no-proxy", Required = false, Default = false, HelpText = "\n"
            + "Enforce direct connection even if OS-level proxy server is configured")]
        public bool NoUseProxy { get; set; }

        [Option("http-connection-pool-size", Required = false, Default = 16, HelpText = "\n"
            + "Maximum number of concurrently open HTTP(S) connections to the TDV Server")]
        public int HttpConnectionPoolSize { get; set; }

        // ----------------------------------------------------------------------------------------
        // cleaned-up CLI arguments
        // ----------------------------------------------------------------------------------------
        public string? TdvServerPrincipalDomain { get; set; } = DefaultPrincipalDomain;
        public string? TdvServerUserName { get; set; }
        public string? TdvServerUserPassword { get; set; }
        public ServerSchemeEnum TdvServerWsScheme { get; set; }
        public string? TdvServerHost { get; set; }
        public int? TdvServerWsApiPort { get; set; }
        public int? TdvServerDbApiPort { get; set; }

        // ----------------------------------------------------------------------------------------
        // validation and clean up routines
        // ----------------------------------------------------------------------------------------
        public virtual void ValidateAndCleanUp(IEnumerable<KeyValuePair<string, string>>? defaults = null)
        {
            using var log = new TraceLog(_log, nameof(ValidateAndCleanUp));

            // preload defaults into a searchable collection
            Dictionary<string, string> defaultsDict;
            if (defaults != null)
                defaultsDict = new Dictionary<string, string>(defaults);
            else
                defaultsDict = new Dictionary<string, string>();

            // read host+port from CLI
            ConnectionStringParser<GenericCredentialsParser, HostPortServerStringParser> connectionStringParser = new ConnectionStringParser<GenericCredentialsParser, HostPortServerStringParser>(
                new GenericCredentialsParser(), new HostPortServerStringParser());
            connectionStringParser.ConnectionString = TdvServerConnectionString;

            // parse the server host
            if (string.IsNullOrWhiteSpace(connectionStringParser.ServerParser.Host))
                throw new ArgumentNullException(null, "TDV server host/IP not supplied");
            else
                TdvServerHost = connectionStringParser.ServerParser.Host;

            // parse the server port, if available
            if (connectionStringParser.ServerParser.Port != null)
            {
                try
                {
                    TdvServerWsApiPort = int.Parse(connectionStringParser.ServerParser.Port);
                }
                catch (ArgumentException)
                {
                    throw new ArgumentException($"WS API port \"{connectionStringParser.ServerParser.Port}\" must be a number", nameof(TdvServerWsApiPort));
                }
            }
            else
            {
                TdvServerWsApiPort = null;
            }

            // choose/autodetect server request scheme (HTTP vs HTTPS) and server port
            TdvServerWsScheme = UseSSL?.ToLower() switch
            {
                "yes" or "true" or "1" or "y" or null or "" => ServerSchemeEnum.HTTPS,
                "no" or "false" or "0" or "n" => ServerSchemeEnum.HTTP,
                "auto" or "autodetect" or "detect" or null or "" => TdvServerWsApiPort switch
                {
                    9402 or 9403 => ServerSchemeEnum.HTTPS,
                    9400 or 9401 => ServerSchemeEnum.HTTP,
                    _ => throw new ArgumentException($"Server request scheme (Plain vs SSL/TLS) autodetection from port {TdvServerWsApiPort} failed")
                },
                _ => throw new ArgumentException($"Unrecognized value \"{UseSSL}\" of --use-ssl parameter")
            };

            // autodetect server port if not supplied and if possible
            if (TdvServerWsApiPort is null)
            {
                TdvServerWsApiPort = TdvServerWsScheme switch
                {
                    ServerSchemeEnum.HTTP => 9400,
                    ServerSchemeEnum.HTTPS => 9402,
                    _ => throw new ArgumentException($"Unable to autodetect TDV WS API server port since the connection scheme (Plain vs SSL/TLS) was not determined")
                };
            }

            if (TdvServerDbApiPort is null)
            {
                TdvServerDbApiPort = TdvServerWsScheme switch
                {
                    ServerSchemeEnum.HTTP => 9401,
                    ServerSchemeEnum.HTTPS => 9403,
                    _ => throw new ArgumentException($"Unable to autodetect TDV DB API server port since the connection scheme (Plain vs SSL/TLS) was not determined")
                };
            }

            // validate combinations of proxy config arguments
            if (!string.IsNullOrWhiteSpace(HttpProxy) && NoUseProxy)
                throw new ArgumentException("Proxy server address supplied, yet use of proxy server disabled... What are you trying to achieve?");

            // validate HTTP connection pool size
            if (HttpConnectionPoolSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(HttpConnectionPoolSize), HttpConnectionPoolSize, "A positive integer value expected for --http-connection-pool-size parameter");

            // validate and/or fill TDV user name
            TdvServerUserName = connectionStringParser.UserParser.Name;
            if (string.IsNullOrWhiteSpace(TdvServerUserName))
            {
                try
                {
                    TdvServerUserName = defaultsDict[$"tdvCredentials:{TdvServerHost}:{TdvServerWsApiPort}:userName"];
                }
                catch (KeyNotFoundException)
                {
                    TdvServerUserName = null;
                }
            }

            if (string.IsNullOrWhiteSpace(TdvServerUserName))
                throw new ArgumentNullException(null, $"Empty user name supplied for {TdvServerHost}");

            // validate and/or fill TDV user password
            TdvServerUserPassword = connectionStringParser.UserParser.Password;
            if (string.IsNullOrEmpty(TdvServerUserPassword))
            {
                try
                {
                    TdvServerUserPassword = defaultsDict[$"tdvCredentials:{TdvServerHost}:{TdvServerWsApiPort}:userPassword"];
                }
                catch (KeyNotFoundException)
                {
                    TdvServerUserPassword = null;
                }
            }

            if (string.IsNullOrEmpty(TdvServerUserPassword))
            {
                Console.Error.Write($"Enter password for {TdvServerUserName}@{TdvServerHost}: ");
                Random charRandomizer = new ();
                TdvServerUserPassword = SystemConsoleExt.ReadLineInSecret((x) => Convert.ToChar(charRandomizer.Next(32, 127)), true);
            }

            if (string.IsNullOrEmpty(TdvServerUserPassword))
                throw new ArgumentNullException(null, $"Empty password supplied for {TdvServerUserName}@{TdvServerHost}");
        }
    }
}
