using System;
using Xunit;
using System.Net;
using Moq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;

namespace Dtmcli.Tests
{
    public class DtmClientTests
    {
        [Fact]
        public async void GenGid_Should_Succeed()
        {
            var factory = new Mock<IHttpClientFactory>();
            var options = Microsoft.Extensions.Options.Options.Create(new DtmCommon.DtmOptions { DtmUrl = "http://localhost:8080" });
            var mockHttpMessageHandler = new ClientMockHttpMessageHandler(HttpStatusCode.OK, "{\"dtm_result\":\"SUCCESS\",\"gid\":\"123\"}");
            factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(mockHttpMessageHandler));

            var client = new DtmClient(factory.Object, options);

            var res = await client.GenGid(new CancellationToken());

            Assert.Equal("123", res);
        }

        [Fact]
        public async void GenGid_Should_Throw_Failure_Exception()
        {
            var factory = new Mock<IHttpClientFactory>();
            var options = Microsoft.Extensions.Options.Options.Create(new DtmCommon.DtmOptions { DtmUrl = "http://localhost:8080" });
            var mockHttpMessageHandler = new ClientMockHttpMessageHandler(HttpStatusCode.BadGateway, "");
            factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(mockHttpMessageHandler));

            var client = new DtmClient(factory.Object, options);

            await Assert.ThrowsAsync<DtmCommon.DtmException>( async()=> await client.GenGid(new CancellationToken()));
        }

        [Fact]
        public async void TransRegisterBranch_Should_Succeed()
        {
            var factory = new Mock<IHttpClientFactory>();
            var options = Microsoft.Extensions.Options.Options.Create(new DtmCommon.DtmOptions { DtmUrl = "http://localhost:8080" });
            var mockHttpMessageHandler = new ClientMockHttpMessageHandler(HttpStatusCode.OK, "");
            factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(mockHttpMessageHandler));

            var client = new DtmClient(factory.Object, options);

            var tb = new DtmCommon.TransBase() { Gid = "123", TransType = "tcc" };

            await client.TransRegisterBranch(tb, null, "OP", new CancellationToken());
        }

        [Fact]
        public async void TransRegisterBranch_With_Added_Should_Succeed()
        {
            var factory = new Mock<IHttpClientFactory>();
            var options = Microsoft.Extensions.Options.Options.Create(new DtmCommon.DtmOptions { DtmUrl = "http://localhost:8080" });
            var mockHttpMessageHandler = new ClientMockHttpMessageHandler(HttpStatusCode.OK, "");
            factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(mockHttpMessageHandler));

            var client = new DtmClient(factory.Object, options);

            var tb = new DtmCommon.TransBase() { Gid = "123", TransType = "tcc" };
            var added = new System.Collections.Generic.Dictionary<string, string>() { { "a", "b" } };

            await client.TransRegisterBranch(tb, added, "OP", new CancellationToken());
        }

        [Fact]
        public async void TransRequestBranch_Should_Succeed()
        {
            var factory = new Mock<IHttpClientFactory>();
            var options = Microsoft.Extensions.Options.Options.Create(new DtmCommon.DtmOptions { DtmUrl = "http://localhost:8080" });
            var mockHttpMessageHandler = new ClientMockHttpMessageHandler(HttpStatusCode.OK, "");
            factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(mockHttpMessageHandler));

            var client = new DtmClient(factory.Object, options);

            var tb = new DtmCommon.TransBase()
            {
                Gid = "123",
                TransType = "tcc",
            };

            await client.TransRequestBranch(tb, HttpMethod.Post, new { }, "00", "try", "http://www.baidu.com?a=1", new CancellationToken());
        }

        [Fact]
        public async void TransRequestBranch_With_BranchHeaders_Should_Succeed()
        {
            var factory = new Mock<IHttpClientFactory>();
            var options = Microsoft.Extensions.Options.Options.Create(new DtmCommon.DtmOptions { DtmUrl = "http://localhost:8080" });
            var mockHttpMessageHandler = new ClientMockHttpMessageHandler(HttpStatusCode.OK, "");
            factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(mockHttpMessageHandler));

            var client = new DtmClient(factory.Object, options);

            var tb = new DtmCommon.TransBase() 
            { 
                Gid = "123", 
                TransType = "tcc",
                BranchHeaders = new System.Collections.Generic.Dictionary<string, string> { { "a", "b" } }
            };
            
            await client.TransRequestBranch(tb, HttpMethod.Post, new { }, "00", "try", "http://www.baidu.com", new CancellationToken());
        }

#if NET5_0_OR_GREATER
        [Fact]
        public void TransBaseFromQuery_Should_Succeed()
        {
            var factory = new Mock<IHttpClientFactory>();
            var options = Microsoft.Extensions.Options.Options.Create(new DtmCommon.DtmOptions { DtmUrl = "http://localhost:8080" });
            var mockHttpMessageHandler = new ClientMockHttpMessageHandler(HttpStatusCode.OK, "");
            factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(mockHttpMessageHandler));

            var client = new DtmClient(factory.Object, options);

            var dict = new System.Collections.Generic.Dictionary<string, Microsoft.Extensions.Primitives.StringValues>()
            {
                { "branch_id","11" },
                { "gid","1111" },
                { "op","try" },
                { "trans_type","tcc" },
            };

            var qs = new Microsoft.AspNetCore.Http.QueryCollection(dict);

            var tb = client.TransBaseFromQuery(qs);

            Assert.Equal(dict["op"], tb.Op);
            Assert.Equal(dict["gid"], tb.Gid);
            Assert.Equal(dict["trans_type"], tb.TransType);
            Assert.Equal(dict["branch_id"], tb.BranchIDGen.BranchID);
        }
#endif

        [Fact]
        public async Task Query_Should_Succeed()
        {
            var factory = new Mock<IHttpClientFactory>();
            var options = Microsoft.Extensions.Options.Options.Create(new DtmCommon.DtmOptions { DtmUrl = "http://localhost:8080" });
            /*
                json sample: saga succeed http (dtm-labs/dtm/qs/main.go)
                {
                 "branches": [
                   {
                     "id": 0,
                     "create_time": "2024-12-16T13:14:14.741109826+08:00",
                     "update_time": "2024-12-16T13:14:14.741109826+08:00",
                     "gid": "HZDVvoKbeCvABvXtVgPoyG",
                     "url": "http://localhost:8082/api/busi_start/TransOutCompensate",
                     "bin_data": "eyJhbW91bnQiOjMwfQ==",
                     "branch_id": "01",
                     "op": "compensate",
                     "status": "prepared"
                   },
                   {
                     "id": 0,
                     "create_time": "2024-12-16T13:14:14.741109826+08:00",
                     "update_time": "2024-12-16T13:14:14.746022823+08:00",
                     "gid": "HZDVvoKbeCvABvXtVgPoyG",
                     "url": "http://localhost:8082/api/busi_start/TransOut",
                     "bin_data": "eyJhbW91bnQiOjMwfQ==",
                     "branch_id": "01",
                     "op": "action",
                     "status": "succeed",
                     "finish_time": "2024-12-16T13:14:14.746022823+08:00"
                   },
                   {
                     "id": 0,
                     "create_time": "2024-12-16T13:14:14.741109826+08:00",
                     "update_time": "2024-12-16T13:14:14.741109826+08:00",
                     "gid": "HZDVvoKbeCvABvXtVgPoyG",
                     "url": "http://localhost:8082/api/busi_start/TransInCompensate",
                     "bin_data": "eyJhbW91bnQiOjMwfQ==",
                     "branch_id": "02",
                     "op": "compensate",
                     "status": "prepared"
                   },
                   {
                     "id": 0,
                     "create_time": "2024-12-16T13:14:14.741109826+08:00",
                     "update_time": "2024-12-16T13:14:14.748793116+08:00",
                     "gid": "HZDVvoKbeCvABvXtVgPoyG",
                     "url": "http://localhost:8082/api/busi_start/TransIn",
                     "bin_data": "eyJhbW91bnQiOjMwfQ==",
                     "branch_id": "02",
                     "op": "action",
                     "status": "succeed",
                     "finish_time": "2024-12-16T13:14:14.748793116+08:00"
                   }
                 ],
                 "transaction": {
                   "id": 7,
                   "create_time": "2024-12-16T13:14:14.741109826+08:00",
                   "update_time": "2024-12-16T13:14:14.750753431+08:00",
                   "gid": "HZDVvoKbeCvABvXtVgPoyG",
                   "trans_type": "saga",
                   "steps": [
                     {
                       "action": "http://localhost:8082/api/busi_start/TransOut",
                       "compensate": "http://localhost:8082/api/busi_start/TransOutCompensate"
                     },
                     {
                       "action": "http://localhost:8082/api/busi_start/TransIn",
                       "compensate": "http://localhost:8082/api/busi_start/TransInCompensate"
                     }
                   ],
                   "payloads": [
                     "{\"amount\":30}",
                     "{\"amount\":30}"
                   ],
                   "status": "succeed",
                   "protocol": "http",
                   "finish_time": "2024-12-16T13:14:14.750753431+08:00",
                   "options": "{\"concurrent\":false}",
                   "next_cron_interval": 10,
                   "next_cron_time": "2024-12-16T13:14:24.741068145+08:00",
                   "concurrent": false
                 }
               }
             */
            var mockHttpMessageHandler = new ClientMockHttpMessageHandler(HttpStatusCode.OK,
                "{\"branches\":[{\"id\":0,\"create_time\":\"2024-12-16T13:14:14.741109826+08:00\",\"update_time\":\"2024-12-16T13:14:14.741109826+08:00\",\"gid\":\"HZDVvoKbeCvABvXtVgPoyG\",\"url\":\"http://localhost:8082/api/busi_start/TransOutCompensate\",\"bin_data\":\"eyJhbW91bnQiOjMwfQ==\",\"branch_id\":\"01\",\"op\":\"compensate\",\"status\":\"prepared\"},{\"id\":0,\"create_time\":\"2024-12-16T13:14:14.741109826+08:00\",\"update_time\":\"2024-12-16T13:14:14.746022823+08:00\",\"gid\":\"HZDVvoKbeCvABvXtVgPoyG\",\"url\":\"http://localhost:8082/api/busi_start/TransOut\",\"bin_data\":\"eyJhbW91bnQiOjMwfQ==\",\"branch_id\":\"01\",\"op\":\"action\",\"status\":\"succeed\",\"finish_time\":\"2024-12-16T13:14:14.746022823+08:00\"},{\"id\":0,\"create_time\":\"2024-12-16T13:14:14.741109826+08:00\",\"update_time\":\"2024-12-16T13:14:14.741109826+08:00\",\"gid\":\"HZDVvoKbeCvABvXtVgPoyG\",\"url\":\"http://localhost:8082/api/busi_start/TransInCompensate\",\"bin_data\":\"eyJhbW91bnQiOjMwfQ==\",\"branch_id\":\"02\",\"op\":\"compensate\",\"status\":\"prepared\"},{\"id\":0,\"create_time\":\"2024-12-16T13:14:14.741109826+08:00\",\"update_time\":\"2024-12-16T13:14:14.748793116+08:00\",\"gid\":\"HZDVvoKbeCvABvXtVgPoyG\",\"url\":\"http://localhost:8082/api/busi_start/TransIn\",\"bin_data\":\"eyJhbW91bnQiOjMwfQ==\",\"branch_id\":\"02\",\"op\":\"action\",\"status\":\"succeed\",\"finish_time\":\"2024-12-16T13:14:14.748793116+08:00\"}],\"transaction\":{\"id\":7,\"create_time\":\"2024-12-16T13:14:14.741109826+08:00\",\"update_time\":\"2024-12-16T13:14:14.750753431+08:00\",\"gid\":\"HZDVvoKbeCvABvXtVgPoyG\",\"trans_type\":\"saga\",\"steps\":[{\"action\":\"http://localhost:8082/api/busi_start/TransOut\",\"compensate\":\"http://localhost:8082/api/busi_start/TransOutCompensate\"},{\"action\":\"http://localhost:8082/api/busi_start/TransIn\",\"compensate\":\"http://localhost:8082/api/busi_start/TransInCompensate\"}],\"payloads\":[\"{\\\"amount\\\":30}\",\"{\\\"amount\\\":30}\"],\"status\":\"succeed\",\"protocol\":\"http\",\"finish_time\":\"2024-12-16T13:14:14.750753431+08:00\",\"options\":\"{\\\"concurrent\\\":false}\",\"next_cron_interval\":10,\"next_cron_time\":\"2024-12-16T13:14:24.741068145+08:00\",\"concurrent\":false}}");
            factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(mockHttpMessageHandler));
            var client = new DtmClient(factory.Object, options);
            TransGlobal globalTrans = await client.Query(gid: "HZDVvoKbeCvABvXtVgPoyG", new CancellationToken());

            // 生成globalTrans.Transaction对象的所有属性断言
            Assert.Equal(7, globalTrans.Transaction.Id);
            Assert.Equal(DateTimeOffset.Parse("2024-12-16T13:14:14.741109826+08:00"), globalTrans.Transaction.CreateTime);
            Assert.Equal(DateTimeOffset.Parse("2024-12-16T13:14:14.750753431+08:00"), globalTrans.Transaction.UpdateTime);
            Assert.Equal("HZDVvoKbeCvABvXtVgPoyG", globalTrans.Transaction.Gid);
            Assert.Equal("saga", globalTrans.Transaction.TransType);
            Assert.Equal(2, globalTrans.Transaction.Steps.Count);
            Assert.Equal("http://localhost:8082/api/busi_start/TransOut", globalTrans.Transaction.Steps[0].Action);
            Assert.Equal("http://localhost:8082/api/busi_start/TransOutCompensate", globalTrans.Transaction.Steps[0].Compensate);
            Assert.Equal("http://localhost:8082/api/busi_start/TransIn", globalTrans.Transaction.Steps[1].Action);
            Assert.Equal("http://localhost:8082/api/busi_start/TransInCompensate", globalTrans.Transaction.Steps[1].Compensate);
            Assert.Equal(2, globalTrans.Transaction.Payloads.Count);
            Assert.Equal("{\"amount\":30}", globalTrans.Transaction.Payloads[0]);
            Assert.Equal("{\"amount\":30}", globalTrans.Transaction.Payloads[1]);
            Assert.Equal("succeed", globalTrans.Transaction.Status);
            Assert.Equal("http", globalTrans.Transaction.Protocol);
            Assert.Equal(DateTimeOffset.Parse("2024-12-16T13:14:14.750753431+08:00"), globalTrans.Transaction.FinishTime);
            Assert.Equal("{\"concurrent\":false}", globalTrans.Transaction.Options);
            Assert.Equal(10, globalTrans.Transaction.NextCronInterval);
            Assert.Equal(DateTimeOffset.Parse("2024-12-16T13:14:24.741068145+08:00"), globalTrans.Transaction.NextCronTime);
            Assert.False(globalTrans.Transaction.Concurrent);

            Assert.Equal(4, globalTrans.Branches.Count);
            // 1
            Assert.Equal(0, globalTrans.Branches[0].Id);
            Assert.Equal(DateTimeOffset.Parse("2024-12-16T13:14:14.741109826+08:00"), globalTrans.Branches[0].CreateTime);
            Assert.Equal(DateTimeOffset.Parse("2024-12-16T13:14:14.741109826+08:00"), globalTrans.Branches[0].UpdateTime);
            Assert.Equal("HZDVvoKbeCvABvXtVgPoyG", globalTrans.Branches[0].Gid);
            Assert.Equal("http://localhost:8082/api/busi_start/TransOutCompensate", globalTrans.Branches[0].Url);
            Assert.Equal("eyJhbW91bnQiOjMwfQ==", globalTrans.Branches[0].BinData);
            Assert.Equal("01", globalTrans.Branches[0].BranchId);
            Assert.Equal("compensate", globalTrans.Branches[0].Op);
            Assert.Equal("prepared", globalTrans.Branches[0].Status);
            // 2
            Assert.Equal(0, globalTrans.Branches[0].Id);
            Assert.Equal(DateTimeOffset.Parse("2024-12-16T13:14:14.741109826+08:00"), globalTrans.Branches[1].CreateTime);
            Assert.Equal(DateTimeOffset.Parse("2024-12-16T13:14:14.746022823+08:00"), globalTrans.Branches[1].UpdateTime);
            Assert.Equal("HZDVvoKbeCvABvXtVgPoyG", globalTrans.Branches[1].Gid);
            Assert.Equal("http://localhost:8082/api/busi_start/TransOut", globalTrans.Branches[1].Url);
            Assert.Equal("eyJhbW91bnQiOjMwfQ==", globalTrans.Branches[1].BinData);
            Assert.Equal("01", globalTrans.Branches[1].BranchId);
            Assert.Equal("action", globalTrans.Branches[1].Op);
            Assert.Equal("succeed", globalTrans.Branches[1].Status);
            // 3
            Assert.Equal(0, globalTrans.Branches[2].Id);
            Assert.Equal(DateTimeOffset.Parse("2024-12-16T13:14:14.741109826+08:00"), globalTrans.Branches[2].CreateTime);
            Assert.Equal(DateTimeOffset.Parse("2024-12-16T13:14:14.741109826+08:00"), globalTrans.Branches[2].UpdateTime);
            Assert.Equal("HZDVvoKbeCvABvXtVgPoyG", globalTrans.Branches[2].Gid);
            Assert.Equal("http://localhost:8082/api/busi_start/TransInCompensate", globalTrans.Branches[2].Url);
            Assert.Equal("eyJhbW91bnQiOjMwfQ==", globalTrans.Branches[2].BinData);
            Assert.Equal("02", globalTrans.Branches[2].BranchId);
            Assert.Equal("compensate", globalTrans.Branches[2].Op);
            Assert.Equal("prepared", globalTrans.Branches[2].Status);
            // 4
            Assert.Equal(0, globalTrans.Branches[3].Id);
            Assert.Equal(DateTimeOffset.Parse("2024-12-16T13:14:14.741109826+08:00"), globalTrans.Branches[3].CreateTime);
            Assert.Equal(DateTimeOffset.Parse("2024-12-16T13:14:14.748793116+08:00"), globalTrans.Branches[3].UpdateTime);
            Assert.Equal("HZDVvoKbeCvABvXtVgPoyG", globalTrans.Branches[3].Gid);
            Assert.Equal("http://localhost:8082/api/busi_start/TransIn", globalTrans.Branches[3].Url);
            Assert.Equal("eyJhbW91bnQiOjMwfQ==", globalTrans.Branches[3].BinData);
            Assert.Equal("02", globalTrans.Branches[3].BranchId);
            Assert.Equal("action", globalTrans.Branches[3].Op);
            Assert.Equal("succeed", globalTrans.Branches[3].Status);
        }
        
        [Fact]
        public async Task Query_Gid_NullOrEmpty()
        {
            var factory = new Mock<IHttpClientFactory>();
            var options = Microsoft.Extensions.Options.Options.Create(new DtmCommon.DtmOptions { DtmUrl = "http://localhost:8080" });
            factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient());
            var client = new DtmClient(factory.Object, options);
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await client.Query(gid: null, new CancellationToken()));
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await client.Query(gid: string.Empty, new CancellationToken()));
        }
        [Fact]
        public async Task Query_Not_Exist_Gid()
        {
            var factory = new Mock<IHttpClientFactory>();
            var options = Microsoft.Extensions.Options.Options.Create(new DtmCommon.DtmOptions { DtmUrl = "http://localhost:8080" });
            /*
               {
                 "branches": [],
                 "transaction": null
               }               
             */
            var mockHttpMessageHandler = new ClientMockHttpMessageHandler(HttpStatusCode.OK, "{\n  \"branches\": [],\n  \"transaction\": null\n}\n");
            factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(mockHttpMessageHandler));
            var client = new DtmClient(factory.Object, options);
            TransGlobal globalTrans = await client.Query(gid: "my-gid", new CancellationToken());
            Assert.NotNull(globalTrans);
            Assert.Null(globalTrans.Transaction);
        }
        
        [Fact]
        public async Task Query_Should_Throw_Exception()
        {
            var factory = new Mock<IHttpClientFactory>();
            var options = Microsoft.Extensions.Options.Options.Create(new DtmCommon.DtmOptions { DtmUrl = "http://localhost:8080" });
            var mockHttpMessageHandler = new ClientMockHttpMessageHandler(HttpStatusCode.InternalServerError, "");
            factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(mockHttpMessageHandler));
            var client = new DtmClient(factory.Object, options);
            await Assert.ThrowsAsync<DtmCommon.DtmException>(async () =>
            {
                await client.Query(gid: "my-gid", new CancellationToken());
            });
        }
        
        
        
        [Fact]
        public async Task QueryStatus_Should_Succeed()
        {
            var factory = new Mock<IHttpClientFactory>();
            var options = Microsoft.Extensions.Options.Options.Create(new DtmCommon.DtmOptions { DtmUrl = "http://localhost:8080" });
            /*
               {
                   "branches": [],
                   "transaction": {
                       "id": 7,
                       "gid": "mV9RGqZCV2mdn9YA6T2TPC",
                       "trans_type": "msg",
                       "status": "prepared",
                       "protocol": "http"
                   }
               }                  
             */
            var mockHttpMessageHandler = new ClientMockHttpMessageHandler(HttpStatusCode.OK, "{\n    \"branches\": [],\n    \"transaction\": {\n        \"id\": 7,\n        \"gid\": \"mV9RGqZCV2mdn9YA6T2TPC\",\n        \"trans_type\": \"msg\",\n        \"status\": \"prepared\",\n        \"protocol\": \"http\"\n    }\n}  ");
            factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(mockHttpMessageHandler));
            var client = new DtmClient(factory.Object, options);
            string status = await client.QueryStatus(gid: "my-gid", new CancellationToken());
            Assert.Equal("prepared", status);
        }
              
        [Fact]
        public async Task QueryStatus_Gid_NullOrEmpty()
        {
            var factory = new Mock<IHttpClientFactory>();
            var options = Microsoft.Extensions.Options.Options.Create(new DtmCommon.DtmOptions { DtmUrl = "http://localhost:8080" });
            factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient());
            var client = new DtmClient(factory.Object, options);
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await client.QueryStatus(gid: null, new CancellationToken()));
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await client.QueryStatus(gid: string.Empty, new CancellationToken()));
        }
        
        [Fact]
        public async Task QueryStatus_Not_Exist_Gid()
        {
            var factory = new Mock<IHttpClientFactory>();
            var options = Microsoft.Extensions.Options.Options.Create(new DtmCommon.DtmOptions { DtmUrl = "http://localhost:8080" });
            /*
               {
                 "branches": [],
                 "transaction": null
               }               
             */
            var mockHttpMessageHandler = new ClientMockHttpMessageHandler(HttpStatusCode.OK, "{\n  \"branches\": [],\n  \"transaction\": null\n}\n");
            factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(mockHttpMessageHandler));
            var client = new DtmClient(factory.Object, options);
            string status = await client.QueryStatus(gid: "my-gid", new CancellationToken());
            Assert.Empty(status);
        }
        
        [Fact]
        public async Task QueryStatus_Should_Throw_Exception()
        {
            var factory = new Mock<IHttpClientFactory>();
            var options = Microsoft.Extensions.Options.Options.Create(new DtmCommon.DtmOptions { DtmUrl = "http://localhost:8080" });
            var mockHttpMessageHandler = new ClientMockHttpMessageHandler(HttpStatusCode.InternalServerError, "");
            factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(mockHttpMessageHandler));
            var client = new DtmClient(factory.Object, options);
            await Assert.ThrowsAsync<DtmCommon.DtmException>(async () =>
            {
                await client.QueryStatus(gid: "my-gid", new CancellationToken());
            });
        }
    }

    internal class ClientMockHttpMessageHandler : DelegatingHandler
    {
        private readonly HttpStatusCode _code;
        private readonly string _msg;
        public ClientMockHttpMessageHandler(HttpStatusCode code, string msg)
        {
            this._code = code;
            this._msg = msg;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var content = new StringContent(_msg);
            var resp = new HttpResponseMessage(_code);
            resp.Content = content;

            return Task.FromResult(resp);
        }
    }
}