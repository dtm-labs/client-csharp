using Apps72.Dev.Data.DbMocker;
using DtmCommon;
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
                })
               .SetPassthroughHeaders(new List<string> { "bh1" });

            await msg.Prepare(busi + "/query");
            await msg.Submit();

            Assert.True(true);
        }

        [Fact]
        public async void DoAndSubmit_Should_Throw_Exception_When_BB_InValid()
        {
            var dtmClient = new Mock<IDtmClient>();
            var bbFactory = new Mock<IBranchBarrierFactory>();

            bbFactory.Setup(x => x.CreateBranchBarrier(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null))
                .Returns(new BranchBarrier("", "", "", "", null, null));

            var msg = new Msg(dtmClient.Object, bbFactory.Object, "123");
            await Assert.ThrowsAnyAsync<DtmException>(async () => await msg.DoAndSubmit("", x => Task.CompletedTask));
        }

        [Fact]
        public async void DoAndSubmitDB_Should_Throw_Exception_When_Transbase_InValid()
        {
            var dtmClient = new Mock<IDtmClient>();
            TestHelper.MockTransCallDtm(dtmClient, Constant.Request.OPERATION_PREPARE, false);

            var gid = string.Empty;
            var msg = new Msg(dtmClient.Object, _branchBarrierFactory, gid);

            var req = new { Amount = 30 };

            msg.Add(busi + "/TransOut", req);

            var db = new MockDbConnection();

            await Assert.ThrowsAsync<DtmException>(async () => await msg.DoAndSubmitDB(busi + "/query", db, x => Task.FromResult<bool>(true)));
        }

        [Fact]
        public async void DoAndSubmitDB_Should_Not_Call_Barrier_When_Prepare_Fail()
        {
            var dtmClient = new Mock<IDtmClient>();
            TestHelper.MockTransCallDtm(dtmClient, Constant.Request.OPERATION_PREPARE, true);

            var gid = "TestMsgNormal";
            var msg = new Msg(dtmClient.Object, _branchBarrierFactory, gid);

            var req = new { Amount = 30 };

            msg.Add(busi + "/TransOut", req);

            var db = new MockDbConnection();
            var mockBusiCall = new Mock<Func<DbTransaction, Task<bool>>>();

            await Assert.ThrowsAnyAsync<Exception>(async () => await msg.DoAndSubmitDB(busi + "/query", db, x => Task.FromResult(true)));            
            mockBusiCall.Verify(x => x.Invoke(It.IsAny<DbTransaction>()), Times.Never);
        }

        [Fact]
        public async void DoAndSubmitDB_Should_Succeed()
        {
            var dtmClient = new Mock<IDtmClient>();
            TestHelper.MockTransCallDtm(dtmClient, Constant.Request.OPERATION_PREPARE, false);
            TestHelper.MockTransCallDtm(dtmClient, Constant.Request.OPERATION_SUBMIT, false);

            var gid = "TestMsgNormal";
            var msg = new Msg(dtmClient.Object, _branchBarrierFactory, gid);

            var req = new { Amount = 30 };

            msg.Add(busi + "/TransOut", req);

            var db = new MockDbConnection();
            db.Mocks.When(x => x.CommandText.Contains("insert", StringComparison.OrdinalIgnoreCase)).ReturnsScalar(cmd => 1);
            db.Mocks.When(x => x.CommandText.Contains("select", StringComparison.OrdinalIgnoreCase)).ReturnsScalar(cmd => "rollback");

            var mockBusiCall = new Mock<Func<DbTransaction, Task<bool>>>();
            mockBusiCall.Setup(x => x.Invoke(It.IsAny<DbTransaction>())).Returns(Task.FromResult(true));

            await msg.DoAndSubmitDB(busi + "/query", db, mockBusiCall.Object);

            mockBusiCall.Verify(x => x.Invoke(It.IsAny<DbTransaction>()), Times.Once);
        }

        [Fact]
        public async void DoAndSubmitDB_Should_Abort_When_BusiCall_ThrowExeption_With_ResultFailure()
        {
            var dtmClient = new Mock<IDtmClient>();
            TestHelper.MockTransCallDtm(dtmClient, Constant.Request.OPERATION_PREPARE, false);
            TestHelper.MockTransCallDtm(dtmClient, Constant.Request.OPERATION_ABORT, false);

            var gid = "TestMsgNormal";
            var msg = new Msg(dtmClient.Object, _branchBarrierFactory, gid);

            var req = new { Amount = 30 };

            msg.Add(busi + "/TransOut", req);

            var db = new MockDbConnection();
            db.Mocks.When(x => x.CommandText.Contains("insert", StringComparison.OrdinalIgnoreCase)).ReturnsScalar(cmd => 1);
            db.Mocks.When(x => x.CommandText.Contains("select", StringComparison.OrdinalIgnoreCase)).ReturnsScalar(cmd => "rollback");

            var mockBusiCall = new Mock<Func<DbTransaction, Task>>();
            mockBusiCall.Setup(x => x.Invoke(It.IsAny<DbTransaction>())).Throws(new DtmFailureException());

            await Assert.ThrowsAsync<DtmFailureException>(async () => await msg.DoAndSubmitDB(busi + "/query", db, mockBusiCall.Object));
            dtmClient.Verify(x => x.TransCallDtm(It.IsAny<TransBase>(), It.IsAny<object>(), Constant.Request.OPERATION_ABORT, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async void DoAndSubmitDB_Should_QueryPrepared_When_BusiCall_ThrowExeption_Without_ResultFailure()
        {
            var dtmClient = new Mock<IDtmClient>();
            TestHelper.MockTransCallDtm(dtmClient, Constant.Request.OPERATION_PREPARE, false);
            TestHelper.MockTransCallDtm(dtmClient, Constant.Request.OPERATION_ABORT, false);
            TestHelper.MockTransCallDtm(dtmClient, Constant.Request.OPERATION_SUBMIT, false);
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

            await Assert.ThrowsAsync<Exception>(async () => await msg.DoAndSubmitDB(busi + "/query", db, mockBusiCall.Object));
            dtmClient.Verify(x => x.TransRequestBranch(It.IsAny<TransBase>(), It.IsAny<HttpMethod>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        public class MsgMockHttpMessageHandler : DelegatingHandler
        {
            public MsgMockHttpMessageHandler()
            {
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var str = await request.Content?.ReadAsStringAsync() ?? "";

                var transBase = System.Text.Json.JsonSerializer.Deserialize<TransBase>(str);

                Assert.Equal("TestMsgNormal", transBase.Gid);
                Assert.Equal("msg", transBase.TransType);
                Assert.True(transBase.WaitResult);
                Assert.Equal(10, transBase.RetryInterval);
                Assert.Equal(100, transBase.TimeoutToFail);
                Assert.Contains("bh1", transBase.BranchHeaders.Keys);
                Assert.Contains("bh2", transBase.BranchHeaders.Keys);
                Assert.Equal(2, transBase.Payloads.Count);
                Assert.Equal(2, transBase.Steps.Count);
                Assert.Contains("bh1", transBase.PassthroughHeaders);

                var content = new StringContent("{\"dtm_result\":\"SUCCESS\"}");

                var resp = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
                resp.Content = content;

                return resp;
            }
        }
    }
}