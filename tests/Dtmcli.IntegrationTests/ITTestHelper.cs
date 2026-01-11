using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dtmcli.IntegrationTests;

public class ITTestHelper
{
    public static string DTMHttpUrl = "http://localhost:36789";
    public static string BuisHttpUrl = "http://localhost:5006/http";
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

    public static BusiRequest GenBusiReq(bool outFailed, bool inFailed, int amount = 30)
    {
        return new BusiRequest
        {
            Amount = amount,
            TransOutResult = outFailed ? "FAILURE" : "",
            TransInResult = inFailed ? "FAILURE" : ""
        };
    }

    public static ServiceProvider AddDtmHttp(int dtmTimout = 10000)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient();
        services.AddDtmcli(option => { option.DtmUrl = DTMHttpUrl; });
        var provider = services.BuildServiceProvider();
        return provider;
    }
}