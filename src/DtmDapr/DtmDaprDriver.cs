using Dtmcli;
using DtmCommon;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace DtmDapr
{
    public static class DtmDaprDriver
    {
        public static string GenerateDtmUrl(string dtmAppId = "dtm")
        {
            var httpPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3500";

            return $"http://localhost:{httpPort}/v1.0/invoke/{dtmAppId}/method";
        }

        public static string AddrForHTTP(string appId, string method)
        {
            return $"{Consts.SchemaHTTP}://DAPR_ENV/{appId}/{method}";
        }

        public static string AddrForProxiedHTTP(string appId, string pathAndQuery)
        {
            if (!pathAndQuery.StartsWith("/"))
                pathAndQuery = "/" + pathAndQuery;
            return $"{Consts.SchemaProxiedHTTP}://DAPR_ENV/{appId}{pathAndQuery}";
        }

        public static Saga AddUseDapr(this Saga self, string appId, string actionMethod, string compensateMethod, object postData)
        {
            return self.Add(AddrForHTTP(appId, actionMethod),
                string.IsNullOrWhiteSpace(compensateMethod) ? "" : AddrForHTTP(appId, compensateMethod),
                postData);
        }

        public static Msg AddUseDapr(this Msg self, string appId, string actionMethod, object postData)
        {
            return self.Add(AddrForHTTP(appId, actionMethod), postData);
        }

        public static Task PrepareUseDapr(this Msg self, string appId, string queryPreparedMethod, CancellationToken cancellationToken = default)
        {
            return self.Prepare(AddrForHTTP(appId, queryPreparedMethod), cancellationToken);
        }

        public static Task DoAndSubmitDbUseDapr(this Msg self, string appId, string queryPreparedMethod, DbConnection db, Func<DbTransaction, Task> busiCall, CancellationToken cancellationToken = default)
        {
            return self.DoAndSubmitDB(AddrForHTTP(appId, queryPreparedMethod), db, busiCall, cancellationToken);
        }

        public static Task DoAndSubmitUseDapr(this Msg self, string appId, string queryPreparedMethod, Func<BranchBarrier, Task> busiCall, CancellationToken cancellationToken = default)
        {
            return self.DoAndSubmit(AddrForHTTP(appId, queryPreparedMethod), busiCall, cancellationToken);
        }

        public static Task<string> CallBranchUseDapr(this Tcc self, object body, string appId, string tryMethod, string confirmMethod, string cancelMethod, CancellationToken cancellationToken = default)
        {
            return self.CallBranch(body, AddrForHTTP(appId, tryMethod), AddrForHTTP(appId, confirmMethod), AddrForHTTP(appId, cancelMethod), cancellationToken);
        }
    }
}
