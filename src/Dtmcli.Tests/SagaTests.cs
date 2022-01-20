using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Dtmcli.Tests
{
    public class SagaTests
    {
        [Fact]
        public async void Submit_Should_Succeed()
        {
            var mockHttpMessageHandler = new SageMockHttpMessageHandler();

            var httpClient = new HttpClient(mockHttpMessageHandler)
            {
                BaseAddress = new System.Uri("http://localhost:36789")
            };
            var dtmClient = new DtmClient(httpClient);

            var gid = "TestSagaNormal";
            var sage = new Saga(dtmClient, gid);

            var busi = "http://localhost:8081/api/busi";

            var req = new { Amount = 30 };

            sage.Add(string.Concat(busi, "/TransOut"), string.Concat(busi, "/TransOutRevert"), req)
                .Add(string.Concat(busi, "/TransOut"), string.Concat(busi, "/TransOutRevert"), req)
                .Add(string.Concat(busi, "/TransIn"), string.Concat(busi, "/TransInRevert"), req)
                .Add(string.Concat(busi, "/TransIn"), string.Concat(busi, "/TransInRevert"), req)
                .EnableConcurrent()
                .AddBranchOrder(2, new System.Collections.Generic.List<int> { 0, 1 })
                .AddBranchOrder(3, new System.Collections.Generic.List<int> { 0, 1 })
                .EnableWaitResult();

            await sage.Submit();
        }

        public class SageMockHttpMessageHandler : DelegatingHandler
        {
            public SageMockHttpMessageHandler()
            {
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var str = await request.Content?.ReadAsStringAsync() ?? "";

                var transBase = System.Text.Json.JsonSerializer.Deserialize<DtmImp.TransBase>(str);

                /*
{
    "gid":"TestSagaNormal",
    "trans_type":"saga",
    "steps":[
        {
            "action":"http://localhost:8081/api/busi/TransOut",
            "compensate":"http://localhost:8081/api/busi/TransOutRevert"
        },
        {
            "action":"http://localhost:8081/api/busi/TransOut",
            "compensate":"http://localhost:8081/api/busi/TransOutRevert"
        },
        {
            "action":"http://localhost:8081/api/busi/TransIn",
            "compensate":"http://localhost:8081/api/busi/TransInRevert"
        },
        {
            "action":"http://localhost:8081/api/busi/TransIn",
            "compensate":"http://localhost:8081/api/busi/TransInRevert"
        }
    ],
    "payloads":[
        "{\"Amount\":30}",
        "{\"Amount\":30}",
        "{\"Amount\":30}",
        "{\"Amount\":30}"
    ],
    "custom_data":"{\"orders\":{\"2\":[0,1],\"3\":[0,1]},\"concurrent\":true}",
    "wait_result": true
}
                 */
                Assert.Equal("TestSagaNormal", transBase.Gid);
                Assert.Equal("saga", transBase.TransType);
                Assert.NotEmpty(transBase.CustomData);
                Assert.True(transBase.WaitResult);
                Assert.Equal(4, transBase.Payloads.Count);
                Assert.Equal(4, transBase.Steps.Count);

                var content = new StringContent("{\"dtm_result\":\"SUCCESS\"}");

                var resp = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
                resp.Content = content;

                return resp;
            }
        }
    }
}