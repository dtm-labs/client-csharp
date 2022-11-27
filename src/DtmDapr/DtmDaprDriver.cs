using System;
using System.Collections.Generic;
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
    }
}
