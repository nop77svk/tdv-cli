namespace tdv_ws_test
{
    using NoP77svk.IO;
    using NoP77svk.TibcoDV.API;
    using NoP77svk.TibcoDV.API.PolledServerTasks;
    using NoP77svk.TibcoDV.CLI.Commons;
    using NoP77svk.TibcoDV.Commons;
    using NoP77svk.Web.WS;
    using WSDL = NoP77svk.TibcoDV.API.WSDL;

    internal class Program : BaseProgram
    {
        private static readonly IInfoOutput _out = new ConsoleInfoOutput(writeToStdErr: true);

        static async Task Main()
        {
            PathExt.FolderDelimiter = TdvWebServiceClient.FolderDelimiter;

            BaseCLI args = new CLI()
            {
                TdvServerUserName = "h50suob_temp",
                TdvServerHost = "localhost",
                TdvServerWsApiPort = 10023,
                TdvServerWsScheme = ServerSchemeEnum.HTTP,
                TdvServerUserPassword = "qwerty",
                HttpConnectionPoolSize = 16,
                NoUseProxy = true
            };

            _out.Info($"Connecting as {args.TdvServerUserName} to {args.TdvServerWsScheme}://{args.TdvServerHost}:{args.TdvServerWsApiPort}");
            using HttpClient httpClient = InitHttpConnectionPool(args);

            TdvWebServiceClient tdvClient = InitTdvRestClient(
                httpClient,
                args,
                1,
                obj =>
                {
                    obj.Headers.Authorization = HttpWebServiceClient.GetHeaderForBasicAuthentication(args.TdvServerUserName, args.TdvServerUserPassword);

                    DebugLogHttpRequest(obj);
                },
                DebugLogHttpResponse
            );

            // --------------------------------------------------------------------------------------------

            _out.Info("begin session");
            string sessionToken = await tdvClient.BeginSession();
            _out.Info($"... session token = {sessionToken}");

            _out.Info("begin transaction");
            await tdvClient.BeginTransaction();

            _out.Info("get introspectable resource IDs");
            Task<List<WSDL.Admin.linkableResourceId>> getIntrospectableResourceIdsTask = tdvClient.GetIntrospectableResourceIds("/shared/L0_DataSources/EG/test_DQC").ToListAsync().AsTask();

            _out.Info("get introspected resource IDs");
            Task<List<WSDL.Admin.pathTypePair>> getIntrospectedResourceIdsTask = tdvClient
                .PolledServerTaskEnumerable(new GetIntrospectedResouceIdsPolledServerTaskHandler(tdvClient, "/shared/L0_DataSources/EG/test_DQC"))
                .ToListAsync()
                .AsTask();

            _out.Info($"... getIntrospected:{getIntrospectedResourceIdsTask.Status}, getIntrospectable:{getIntrospectableResourceIdsTask.Status}");

            await Task.WhenAll(getIntrospectableResourceIdsTask, getIntrospectedResourceIdsTask);

            _out.Info("get introspected resource IDs");
            foreach (var row in getIntrospectedResourceIdsTask.Result)
                _out.Info($"... {row.type.ToString().ToLower()} {row.path}");

            _out.Info("get introspectable resource IDs");
            foreach (var row in getIntrospectableResourceIdsTask.Result)
                _out.Info($"... {row.resourceId.type.ToString().ToLower()}/{row.resourceId.subtype.ToString().ToLower()} {row.resourceId.path}");

            _out.Info("intersection");
            foreach (var row in getIntrospectedResourceIdsTask.Result
                .Select(x => new ValueTuple<WSDL.Admin.resourceType, string>(x.type, x.path))
                .Intersect(getIntrospectableResourceIdsTask.Result
                    .Select(x => new ValueTuple<WSDL.Admin.resourceType, string>(x.resourceId.type, x.resourceId.path)))
                .Select(x => new WSDL.Admin.pathTypePair() { type = x.Item1, path = x.Item2 })
            )
                _out.Info($"... {row.type.ToString().ToLower()} {row.path}");

            _out.Info("rollback transaction");
            await tdvClient.RollbackTransaction();

            _out.Info("end session");
            await tdvClient.CloseSession();
        }
    }
}