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
        /// <summary>
        /// Generate dtm dapr sidecar url
        /// </summary>
        /// <param name="dtmAppId"></param>
        /// <returns></returns>
        public static string GenerateDtmUrl(string dtmAppId = "dtm")
        {
            var httpPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3500";

            return $"http://localhost:{httpPort}/v1.0/invoke/{dtmAppId}/method";
        }

        /// <summary>
        /// Generate dapr app service invocation http address for dtm
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public static string AddrForHTTP(string appId, string method)
        {
            return $"{Consts.SchemaHTTP}://DAPR_ENV/{appId}/{method}";
        }

        /// <summary>
        /// Generate dapr app service invocation http address for TCC try action
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="tryMethod"></param>
        /// <returns></returns>
        public static string AddrTccTryForHTTP(string appId, string tryMethod)
        {
            var httpPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3500";

            return $"http://localhost:{httpPort}/v1.0/invoke/{appId}/method/{tryMethod}";
        }

        /// <summary>
        /// Generate dapr app service invocation proxied http address for dtm
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="pathAndQuery"></param>
        /// <returns></returns>
        public static string AddrForProxiedHTTP(string appId, string pathAndQuery)
        {
            if (!pathAndQuery.StartsWith("/"))
                pathAndQuery = "/" + pathAndQuery;
            return $"{Consts.SchemaProxiedHTTP}://DAPR_ENV/{appId}{pathAndQuery}";
        }

        /// <summary>
        /// Add sage action and compensate for dapr app
        /// </summary>
        /// <param name="self"></param>
        /// <param name="appId"></param>
        /// <param name="actionMethod"></param>
        /// <param name="compensateMethod"></param>
        /// <param name="postData"></param>
        /// <returns></returns>
        public static Saga Add(this Saga self, string appId, string actionMethod, string compensateMethod, object postData)
        {
            return self.Add(AddrForHTTP(appId, actionMethod),
                string.IsNullOrWhiteSpace(compensateMethod) ? "" : AddrForHTTP(appId, compensateMethod),
                postData);
        }

        /// <summary>
        /// Add msg action for dapr app
        /// </summary>
        /// <param name="self"></param>
        /// <param name="appId"></param>
        /// <param name="actionMethod"></param>
        /// <param name="postData"></param>
        /// <returns></returns>
        public static Msg Add(this Msg self, string appId, string actionMethod, object postData)
        {
            return self.Add(AddrForHTTP(appId, actionMethod), postData);
        }

        /// <summary>
        /// Prepare msg and add msg query-prepared for dapr app
        /// </summary>
        /// <param name="self"></param>
        /// <param name="appId"></param>
        /// <param name="queryPreparedMethod"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task Prepare(this Msg self, string appId, string queryPreparedMethod, CancellationToken cancellationToken = default)
        {
            return self.Prepare(AddrForHTTP(appId, queryPreparedMethod), cancellationToken);
        }

        /// <summary>
        /// Add msg DoAndSubmitDB action for dapr app
        /// </summary>
        /// <param name="self"></param>
        /// <param name="appId"></param>
        /// <param name="queryPreparedMethod"></param>
        /// <param name="db"></param>
        /// <param name="busiCall"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task DoAndSubmitDB(this Msg self, string appId, string queryPreparedMethod, DbConnection db, Func<DbTransaction, Task> busiCall, CancellationToken cancellationToken = default)
        {
            return self.DoAndSubmitDB(AddrForHTTP(appId, queryPreparedMethod), db, busiCall, cancellationToken);
        }

        /// <summary>
        /// Add msg DoAndSubmit action for dapr app
        /// </summary>
        /// <param name="self"></param>
        /// <param name="appId"></param>
        /// <param name="queryPreparedMethod"></param>
        /// <param name="busiCall"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task DoAndSubmit(this Msg self, string appId, string queryPreparedMethod, Func<BranchBarrier, Task> busiCall, CancellationToken cancellationToken = default)
        {
            return self.DoAndSubmit(AddrForHTTP(appId, queryPreparedMethod), busiCall, cancellationToken);
        }

        /// <summary>
        /// Add tcc CallBranch for dapr app
        /// </summary>
        /// <param name="self"></param>
        /// <param name="body"></param>
        /// <param name="appId"></param>
        /// <param name="tryMethod"></param>
        /// <param name="confirmMethod"></param>
        /// <param name="cancelMethod"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task<string> CallBranch(this Tcc self, object body, string appId, string tryMethod, string confirmMethod, string cancelMethod, CancellationToken cancellationToken = default)
        {
            return self.CallBranch(body, AddrTccTryForHTTP(appId, tryMethod), AddrForHTTP(appId, confirmMethod), AddrForHTTP(appId, cancelMethod), cancellationToken);
        }
    }
}
