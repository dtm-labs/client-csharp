using Apps72.Dev.Data.DbMocker;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Dtmcli.Tests
{
    public class MsgTests
    {
        private readonly IBranchBarrierFactory _branchBarrierFactory;

        private static readonly string busi = "http://localhost:8081/busisvc";

        public MsgTests()
        {
            var provider = TestHelper.AddDtmCli();

            var factory = provider.GetRequiredService<IBranchBarrierFactory>();
            _branchBarrierFactory = factory;
        }

        [Fact]
        public async void Submit_Should_Succeed()
        {
            var fakeFactory = new Mock<IHttpClientFactory>();

            var mockHttpMessageHandler = new MsgMockHttpMessageHandler();
            var httpClient = new HttpClient(mockHttpMessageHandler);
            fakeFactory.Setup(x=>x.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var dtmOptions = new DtmOptions { DtmUrl = "http://localhost:36789" };
            var dtmClient = new DtmClient(fakeFactory.Object, Microsoft.Extensions.Options.Options.Create(dtmOptions));

            var gid = "TestMsgNormal";
            var msg = new Msg(dtmClient, _branchBarrierFactory, gid);

            var req = new { Amount = 30 };

            msg.Add(busi + "/TransOut", req)
               .Add(busi + "/TransIn", req)
               .EnableWaitResult()
               .SetRetryInterval(10)
               .SetTimeoutToFail(100)
               .SetBranchHeaders(new Dictionary<string, string>
                {
                    { "bh1", "123" },
                    { "bh2", "456" },
                });

            var prepareRes = await msg.Prepare(busi + "/query");
            Assert.True(prepareRes);

            var submitRes = await msg.Submit();
            Assert.True(submitRes);
        }

        [Fact]
        public async void DoAndSubmitDB_Should_Throw_Exception_When_Transbase_InValid()
        {
            var dtmClient = new Mock<IDtmClient>();
            TestHelper.MockTransCallDtm(dtmClient, Constant.Request.OPERATION_PREPARE, true);

            var gid = string.Empty;
            var msg = new Msg(dtmClient.Object, _branchBarrierFactory, gid);

            var req = new { Amount = 30 };

            msg.Add(busi + "/TransOut", req);

            var db = new MockDbConnection();

            await Assert.ThrowsAsync<DtmcliException>(async () => await msg.DoAndSubmitDB(busi + "/query", db, x => Task.FromResult<bool>(true)));
        }

        [Fact]
        public async void DoAndSubmitDB_Should_Not_Call_Barrier_When_Prepare_Fail()
        {
            var dtmClient = new Mock<IDtmClient>();
            TestHelper.MockTransCallDtm(dtmClient, Constant.Request.OPERATION_PREPARE, false);

            var gid = "TestMsgNormal";
            var msg = new Msg(dtmClient.Object, _branchBarrierFactory, gid);

            var req = new { Amount = 30 };

            msg.Add(busi + "/TransOut", req);

            var db = new MockDbConnection();
            var mockBusiCall = new Mock<Func<DbTransaction, Task<bool>>>();

            var res = await msg.DoAndSubmitDB(busi + "/query", db, x => Task.FromResult(true));

            Assert.False(res);
            mockBusiCall.Verify(x => x.Invoke(It.IsAny<DbTransaction>()), Times.Never);
        }

        [Fact]
        public async void DoAndSubmitDB_Should_Succeed()
        {
            var dtmClient = new Mock<IDtmClient>();
            TestHelper.MockTransCallDtm(dtmClient, Constant.Request.OPERATION_PREPARE, true);
            TestHelper.MockTransCallDtm(dtmClient, Constant.Request.OPERATION_SUBMIT, true);

            var gid = "TestMsgNormal";
            var msg = new Msg(dtmClient.Object, _branchBarrierFactory, gid);

            var req = new { Amount = 30 };

            msg.Add(busi + "/TransOut", req);

            var db = new MockDbConnection();
            db.Mocks.When(x => x.CommandText.Contains("insert", StringComparison.OrdinalIgnoreCase)).ReturnsScalar(cmd => 1);
            db.Mocks.When(x => x.CommandText.Contains("select", StringComparison.OrdinalIgnoreCase)).ReturnsScalar(cmd => "rollback");

            var mockBusiCall = new Mock<Func<DbTransaction, Task<bool>>>();
            mockBusiCall.Setup(x => x.Invoke(It.IsAny<DbTransaction>())).Returns(Task.FromResult(true));

            var res = await msg.DoAndSubmitDB(busi + "/query", db, mockBusiCall.Object);

            Assert.True(res);
            mockBusiCall.Verify(x => x.Invoke(It.IsAny<DbTransaction>()), Times.Once);
        }

        [Fact]
        public async void DoAndSubmitDB_Should_Abort_When_BusiCall_ThrowExeption_With_ResultFailure()
        {
            var dtmClient = new Mock<IDtmClient>();
            TestHelper.MockTransCallDtm(dtmClient, Constant.Request.OPERATION_PREPARE, true);
            TestHelper.MockTransCallDtm(dtmClient, Constant.Request.OPERATION_ABORT, true);

            var gid = "TestMsgNormal";
            var msg = new Msg(dtmClient.Object, _branchBarrierFactory, gid);

            var req = new { Amount = 30 };

            msg.Add(busi + "/TransOut", req);

            var db = new MockDbConnection();
            db.Mocks.When(x => x.CommandText.Contains("insert", StringComparison.OrdinalIgnoreCase)).ReturnsScalar(cmd => 1);
            db.Mocks.When(x => x.CommandText.Contains("select", StringComparison.OrdinalIgnoreCase)).ReturnsScalar(cmd => "rollback");

            var mockBusiCall = new Mock<Func<DbTransaction, Task>>();
            mockBusiCall.Setup(x => x.Invoke(It.IsAny<DbTransaction>())).Throws(new Exception(Constant.ResultFailure));

            var res = await msg.DoAndSubmitDB(busi + "/query", db, mockBusiCall.Object);

            Assert.False(res);
            dtmClient.Verify(x => x.TransCallDtm(It.IsAny<DtmImp.TransBase>(), It.IsAny<object>(), Constant.Request.OPERATION_ABORT, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async void DoAndSubmitDB_Should_QueryPrepared_When_BusiCall_ThrowExeption_Without_ResultFailure()
        {
            var dtmClient = new Mock<IDtmClient>();
            TestHelper.MockTransCallDtm(dtmClient, Constant.Request.OPERATION_PREPARE, true);
            TestHelper.MockTransCallDtm(dtmClient, Constant.Request.OPERATION_ABORT, true);
            TestHelper.MockTransCallDtm(dtmClient, Constant.Request.OPERATION_SUBMIT, true);
            TestHelper.MockTransRequestBranch(dtmClient, System.Net.HttpStatusCode.OK);

            var gid = "TestMsgNormal";
            var msg = new Msg(dtmClient.Object, _branchBarrierFactory, gid);

            var req = new { Amount = 30 };

            msg.Add(busi + "/TransOut", req);

            var db = new MockDbConnection();
            db.Mocks.When(x => x.CommandText.Contains("insert", StringComparison.OrdinalIgnoreCase)).ReturnsScalar(cmd => 1);
            db.Mocks.When(x => x.CommandText.Contains("select", StringComparison.OrdinalIgnoreCase)).ReturnsScalar(cmd => "rollback");

            var mockBusiCall = new Mock<Func<DbTransaction, Task<bool>>>();
            mockBusiCall.Setup(x => x.Invoke(It.IsAny<DbTransaction>())).Throws(new Exception("ex"));

            var res = await msg.DoAndSubmitDB(busi + "/query", db, mockBusiCall.Object);

            Assert.False(res);
            dtmClient.Verify(x => x.TransRequestBranch(It.IsAny<DtmImp.TransBase>(), It.IsAny<HttpMethod>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        public class MsgMockHttpMessageHandler : DelegatingHandler
        {
            public MsgMockHttpMessageHandler()
            {
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var str = await request.Content?.ReadAsStringAsync() ?? "";

                var transBase = System.Text.Json.JsonSerializer.Deserialize<DtmImp.TransBase>(str);

                Assert.Equal("TestMsgNormal", transBase.Gid);
                Assert.Equal("msg", transBase.TransType);
                Assert.True(transBase.WaitResult);
                Assert.Equal(10, transBase.RetryInterval);
                Assert.Equal(100, transBase.TimeoutToFail);
                Assert.Contains("bh1", transBase.BranchHeaders.Keys);
                Assert.Contains("bh2", transBase.BranchHeaders.Keys);
                Assert.Equal(2, transBase.Payloads.Count);
                Assert.Equal(2, transBase.Steps.Count);

                var content = new StringContent("{\"dtm_result\":\"SUCCESS\"}");

                var resp = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
                resp.Content = content;

                return resp;
            }
        }
    }
}