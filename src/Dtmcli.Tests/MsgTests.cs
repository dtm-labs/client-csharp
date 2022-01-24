using Apps72.Dev.Data.DbMocker;
using Moq;
using System;
using System.Data.Common;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Dtmcli.Tests
{
    public class MsgTests
    {
        [Fact]
        public async void Submit_Should_Succeed()
        {
            var mockHttpMessageHandler = new MsgMockHttpMessageHandler();

            var httpClient = new HttpClient(mockHttpMessageHandler)
            {
                BaseAddress = new Uri("http://localhost:36789")
            };
            var dtmClient = new DtmClient(httpClient);

            var gid = "TestMsgNormal";
            var msg = new Msg(dtmClient, gid);

            var busi = "http://localhost:8081/api/busi";

            var req = new { Amount = 30 };

            msg.Add(busi + "/TransOut", req)
               .Add(busi + "/TransIn", req)
               .EnableWaitResult();

            var prepareRes = await msg.Prepare(busi + "/query");
            Assert.True(prepareRes);

            var submitRes = await msg.Submit();
            Assert.True(submitRes);
        }

        [Fact]
        public async void PrepareAndSubmit_Should_Throw_Exception_When_Transbase_InValid()
        {
            var dtmClient = new Mock<IDtmClient>();
            MockTransCallDtm(dtmClient, Constant.Request.OPERATION_PREPARE, true);

            var gid = string.Empty;
            var msg = new Msg(dtmClient.Object, gid);

            var busi = "http://localhost:8081/api/busi";

            var req = new { Amount = 30 };

            msg.Add(busi + "/TransOut", req);

            var db = new MockDbConnection();

            await Assert.ThrowsAsync<DtmcliException>(async () => await msg.PrepareAndSubmit(busi + "/query", db, x => Task.CompletedTask));
        }

        [Fact]
        public async void PrepareAndSubmit_Should_Not_Call_Barrier_When_Prepare_Fail()
        {
            var dtmClient = new Mock<IDtmClient>();
            MockTransCallDtm(dtmClient, Constant.Request.OPERATION_PREPARE, false);

            var gid = "TestMsgNormal";
            var msg = new Msg(dtmClient.Object, gid);

            var busi = "http://localhost:8081/api/busi";

            var req = new { Amount = 30 };

            msg.Add(busi + "/TransOut", req);

            var db = new MockDbConnection();
            var mockBusiCall = new Mock<Func<DbTransaction, Task>>();

            var res = await msg.PrepareAndSubmit(busi + "/query", db, x => Task.CompletedTask);

            Assert.False(res);
            mockBusiCall.Verify(x => x.Invoke(It.IsAny<DbTransaction>()), Times.Never);
        }

        [Fact]
        public async void PrepareAndSubmit_Should_Succeed()
        {
            var dtmClient = new Mock<IDtmClient>();
            MockTransCallDtm(dtmClient, Constant.Request.OPERATION_PREPARE, true);
            MockTransCallDtm(dtmClient, Constant.Request.OPERATION_SUBMIT, true);

            var gid = "TestMsgNormal";
            var msg = new Msg(dtmClient.Object, gid);

            var busi = "http://localhost:8081/api/busi";

            var req = new { Amount = 30 };

            msg.Add(busi + "/TransOut", req);

            var db = new MockDbConnection();
            db.Mocks.When(x => x.CommandText.Contains("insert", StringComparison.OrdinalIgnoreCase)).ReturnsScalar(cmd => 1);
            db.Mocks.When(x => x.CommandText.Contains("select", StringComparison.OrdinalIgnoreCase)).ReturnsScalar(cmd => "rollback");

            var mockBusiCall = new Mock<Func<DbTransaction, Task>>();
            mockBusiCall.Setup(x => x.Invoke(It.IsAny<DbTransaction>())).Returns(Task.CompletedTask);

            var res = await msg.PrepareAndSubmit(busi + "/query", db, mockBusiCall.Object);

            Assert.True(res);
            mockBusiCall.Verify(x => x.Invoke(It.IsAny<DbTransaction>()), Times.Once);
        }

        [Fact]
        public async void PrepareAndSubmit_Should_Abort_When_BusiCall_ThrowException()
        {
            var dtmClient = new Mock<IDtmClient>();
            MockTransCallDtm(dtmClient, Constant.Request.OPERATION_PREPARE, true);
            MockTransCallDtm(dtmClient, Constant.Request.OPERATION_ABORT, true);
            
            var gid = "TestMsgNormal";
            var msg = new Msg(dtmClient.Object, gid);

            var busi = "http://localhost:8081/api/busi";

            var req = new { Amount = 30 };

            msg.Add(busi + "/TransOut", req);

            var db = new MockDbConnection();
            db.Mocks.When(x => x.CommandText.Contains("insert", StringComparison.OrdinalIgnoreCase)).ReturnsScalar(cmd => 1);
            db.Mocks.When(x => x.CommandText.Contains("select", StringComparison.OrdinalIgnoreCase)).ReturnsScalar(cmd => "rollback");

            var mockBusiCall = new Mock<Func<DbTransaction, Task>>();
            mockBusiCall.Setup(x => x.Invoke(It.IsAny<DbTransaction>())).Throws(new Exception("ex"));

            var res = await msg.PrepareAndSubmit(busi + "/query", db, mockBusiCall.Object);

            Assert.False(res);
            dtmClient.Verify(x => x.TransCallDtm(It.IsAny<DtmImp.TransBase>(), It.IsAny<object>(), Constant.Request.OPERATION_ABORT, It.IsAny<CancellationToken>()), Times.Once);
        }

        private void MockTransCallDtm(Mock<IDtmClient> mock, string op, bool result)
        {
            mock
                .Setup(x => x.TransCallDtm(It.IsAny<DtmImp.TransBase>(), It.IsAny<object>(), op, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));
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