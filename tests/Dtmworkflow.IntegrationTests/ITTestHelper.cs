using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dtmworkflow.IntegrationTests
{
    public class ITTestHelper
    {
        public static string DTMHttpUrl = "http://localhost:36789";
        public static string DTMgRPCUrl = "http://localhost:36790";
        public static string BuisgRPCUrl = "localhost:5005";
        public static string BuisHttpUrl = "localhost:5006";
        private static System.Net.Http.HttpClient _client = new System.Net.Http.HttpClient();

        public static async Task<string> GetTranStatus(string gid)
        {
            var resp = await _client.GetAsync($"{DTMHttpUrl}/api/dtmsvr/query?gid={gid}").ConfigureAwait(false);

            if (resp.IsSuccessStatusCode)
            {
                var content = await resp.Content.ReadAsStringAsync();
                var res = System.Text.Json.JsonSerializer.Deserialize<QueryResult>(content);
                return res.Transaction.Status;
            }

            return string.Empty;
        }

        public class QueryResult
        {
            public class TransBranchStore
            {

            }

            public class TransGlobalStore
            {
                [System.Text.Json.Serialization.JsonPropertyName("status")]
                public string Status { get; set; }
            }


            [System.Text.Json.Serialization.JsonPropertyName("branches")]
            public List<TransBranchStore> Branches { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("transaction")]
            public TransGlobalStore Transaction { get; set; }
        }

        public static busi.BusiReq GenBusiReq(bool outFailed, bool inFailed, int amount = 30)
        {
            return new busi.BusiReq
            {
                Amount = amount,
                TransOutResult = outFailed ? "FAILURE" : "",
                TransInResult = inFailed ? "FAILURE" : ""
            };
        }

        public static ServiceProvider AddDtmworkflow(int dtmTimout = 10000)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDtmWorkflow(x =>
            {
                x.DtmUrl = DTMHttpUrl;
                x.DtmGrpcUrl = DTMgRPCUrl;
                x.DtmTimeout = dtmTimout;
            });

            var provider = services.BuildServiceProvider();
            return provider;
        }

        public static string GetRedisAccountKey(int uid) => $"dtm:busi:redis-account-key-{uid}";

        public static async Task<StackExchange.Redis.IDatabase> GetRedis()
        {
            // NOTE: this redis connection code is only for sample, don't use in production
            var config = StackExchange.Redis.ConfigurationOptions.Parse("localhost:6379");
            var conn = await StackExchange.Redis.ConnectionMultiplexer.ConnectAsync(config);
            return conn.GetDatabase();
        }
    }

}