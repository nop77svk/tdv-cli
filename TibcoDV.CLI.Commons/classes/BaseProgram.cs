﻿namespace NoP77svk.TibcoDV.Commons
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using log4net;
    using NoP77svk.TibcoDV.API;
    using NoP77svk.Web.WS;

    public class BaseProgram
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(BaseCLI));

        protected static TdvWebServiceClient? TdvClient { get; set; }

        protected static string PrepareObjectAnnotation(string? authorOverride = null)
        {
            string author = authorOverride ?? (Environment.UserDomainName + '/' + Environment.UserName);
            return $"Generated by {author} on {DateTime.Now}";
        }

        protected static HttpClient InitHttpConnectionPool(BaseCLI args)
        {
            using var log = new TraceLog(_log, nameof(InitHttpConnectionPool));

            HttpClientHandler httpClientHandler = new ()
            {
                Proxy = !string.IsNullOrWhiteSpace(args.HttpProxy) ? new WebProxy(args.HttpProxy) : HttpClient.DefaultProxy,
                UseProxy = !args.NoUseProxy,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(args.TdvServerUserName, args.TdvServerUserPassword),
                MaxConnectionsPerServer = args.HttpConnectionPoolSize
            };
            if (args.IgnoreInvalidSslCertificates)
                httpClientHandler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;

            return new HttpClient(httpClientHandler);
        }

        protected static TdvWebServiceClient InitTdvRestClient(
            HttpClient? httpClient,
            BaseCLI args,
            int tdvApiVersion = 1,
            Action<HttpRequestMessage>? httpRequestPostprocess = null,
            Action<HttpResponseMessage>? httpResponsePostprocess = null
        )
        {
            using var log = new TraceLog(_log, nameof(InitTdvRestClient));

            if (httpClient is null)
                throw new ArgumentNullException(nameof(httpClient));

            HttpWebServiceClient genericRestClient = new HttpWebServiceClient(httpClient, args.TdvServerHost)
            {
                ServerScheme = args.TdvServerWsScheme switch
                {
                    ServerSchemeEnum.HTTP => "http",
                    ServerSchemeEnum.HTTPS => "https",
                    _ => throw new ArgumentOutOfRangeException("Unrecognized/unknown server scheme")
                },
                ServerPort = args.TdvServerWsApiPort,
                HttpRequestPostprocess = httpRequestPostprocess,
                HttpResponsePostprocess = httpResponsePostprocess
            };

            return new TdvWebServiceClient(genericRestClient, tdvApiVersion);
        }

        protected static void InitLogging()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            log4net.Config.XmlConfigurator.Configure(new FileInfo("log4net.config"));
        }

        protected static void DebugLogHttpResponse(HttpResponseMessage obj)
        {
            if (!_log.IsDebugEnabled)
                return;

            using var log = new TraceLog(_log, nameof(DebugLogHttpResponse));

            StringBuilder logMsg = new StringBuilder();
            logMsg.AppendLine("HTTP response details...");
            HttpWebServiceClient.AggregateResponseAsString(obj, logMsg);

            _log.Debug(logMsg.ToString());
        }

        protected static void DebugLogHttpRequest(HttpRequestMessage obj)
        {
            if (!_log.IsDebugEnabled)
                return;

            using var log = new TraceLog(_log, nameof(DebugLogHttpRequest));

            StringBuilder logMsg = new StringBuilder();
            logMsg.AppendLine("HTTP request details...");
            HttpWebServiceClient.AggregateRequestAsString(obj, logMsg);

            _log.Debug(logMsg.ToString());
        }
    }
}