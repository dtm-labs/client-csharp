using Apps72.Dev.Data.DbMocker;
using DtmCommon;
using Dtmgrpc.DtmGImp;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Xunit;

namespace Dtmgrpc.Tests
{
    public class MsgTests
    {
        private readonly IBranchBarrierFactory _branchBarrierFactory;

        private static readonly string busi = "localhost:8081/busisvc";

        public MsgTests()
        {
            var dtm = "http://localhost:36790";
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDtmGrpc(x =>
            {
                x.DtmGrpcUrl = dtm;
            });

            var provider = services.BuildServiceProvider();

            var factory = provider.GetRequiredService<IBranchBarrierFactory>();
            _branchBarrierFactory = factory;
        }

        [Fact]
        public async void Submit_Should_Succeed()
        {
            var dtmClient = new Mock<IDtmgRPCClient>();
            TransMockHelper.MockTransCallDtm(dtmClient, Constant.Op.Prepare, false);
            TransMockHelper.MockTransRequestBranch(dtmClient, false);

            var gid = "TestMsgNormal";
            var msg = new MsgGrpc(dtmClient.Object, _branchBarrierFactory, "", gid);

            var req = new Empty();

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
        public async void DoAndSubmit_Should_Throw_Exception_When_Transbase_InValid()
        {
            var dtmClient = new Mock<IDtmgRPCClient>();
            var bbFactory = new Mock<IBranchBarrierFactory>();

            bbFactory.Setup(x => x.CreateBranchBarrier(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null))
                .Returns(new BranchBarrier("", "", "", "", null, null));

            var gid = string.Empty;
            var msg = new MsgGrpc(dtmClient.Object, bbFactory.Object, "", gid);

            var req = new Empty();
            msg.Add(busi + "/TransOut", req);
            await Assert.ThrowsAsync<DtmException>(async () => await msg.DoAndSubmit(busi + "/query", x => Task.CompletedTask));
        }

        [Fact]
        public async void DoAndSubmitDB_Should_Throw_Exception_When_Transbase_InValid()
        {
            var dtmClient = new Mock<IDtmgRPCClient>();
            TransMockHelper.MockTransCallDtm(dtmClient, Constant.Op.Prepare, false);

            var gid = string.Empty;
            var msg = new MsgGrpc(dtmClient.Object, _branchBarrierFactory, "", gid);

            var req = new Empty();

            msg.Add(busi + "/TransOut", req);

            var db = new MockDbConnection();

            await Assert.ThrowsAsync<DtmException>(async () => await msg.DoAndSubmitDB(busi + "/query", db, x => Task.CompletedTask));
        }

        [Fact]
        public async void DoAndSubmitDB_Should_Not_Call_Barrier_When_Prepare_Fail()
        {
            var dtmClient = new Mock<IDtmgRPCClient>();
            TransMockHelper.MockTransCallDtm(dtmClient, Constant.Op.Prepare, true);

            var gid = "TestMsgNormal";
            var msg = new MsgGrpc(dtmClient.Object, _branchBarrierFactory, "", gid);

            var req = new Empty();

            msg.Add(busi + "/TransOut", req);

            var db = new MockDbConnection();
            var mockBusiCall = new Mock<Func<DbTransaction, Task<bool>>>();

            await Assert.ThrowsAnyAsync<Exception>(async () => await msg.DoAndSubmitDB(busi + "/query", db, x => Task.FromResult(true)));
        }

        [Fact]
        public async void DoAndSubmitDB_Should_Succeed()
        {
            var dtmClient = new Mock<IDtmgRPCClient>();
            TransMockHelper.MockTransCallDtm(dtmClient, Constant.Op.Prepare, false);
            TransMockHelper.MockTransCallDtm(dtmClient, Constant.Op.Submit, false);

            var gid = "TestMsgNormal";
            var msg = new MsgGrpc(dtmClient.Object, _branchBarrierFactory, "", gid);

            var req = new Empty();

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
            var dtmClient = new Mock<IDtmgRPCClient>();
            TransMockHelper.MockTransCallDtm(dtmClient, Constant.Op.Prepare, false);
            TransMockHelper.MockTransCallDtm(dtmClient, Constant.Op.Abort, false);

            var gid = "TestMsgNormal";
            var msg = new MsgGrpc(dtmClient.Object, _branchBarrierFactory, "", gid);

            var req = new Empty();

            msg.Add(busi + "/TransOut", req);

            var db = new MockDbConnection();
            db.Mocks.When(x => x.CommandText.Contains("insert", StringComparison.OrdinalIgnoreCase)).ReturnsScalar(cmd => 1);
            db.Mocks.When(x => x.CommandText.Contains("select", StringComparison.OrdinalIgnoreCase)).ReturnsScalar(cmd => "rollback");

            var mockBusiCall = new Mock<Func<DbTransaction, Task>>();
            mockBusiCall.Setup(x => x.Invoke(It.IsAny<DbTransaction>())).Throws(new DtmFailureException());

            await Assert.ThrowsAsync<DtmFailureException>(async () => await msg.DoAndSubmitDB(busi + "/query", db, mockBusiCall.Object));
            dtmClient.Verify(x => x.DtmGrpcCall(It.IsAny<TransBase>(), Constant.Op.Abort), Times.Once);
        }

        [Fact]
        public async void DoAndSubmitDB_Should_QueryPrepared_When_BusiCall_ThrowExeption_Without_ResultFailure()
        {
            var dtmClient = new Mock<IDtmgRPCClient>();
            TransMockHelper.MockTransCallDtm(dtmClient, Constant.Op.Prepare, false);
            TransMockHelper.MockTransCallDtm(dtmClient, Constant.Op.Abort, false);
            TransMockHelper.MockTransCallDtm(dtmClient, Constant.Op.Submit, false);
            TransMockHelper.MockTransRequestBranch(dtmClient, false);

            var gid = "TestMsgNormal";
            var msg = new MsgGrpc(dtmClient.Object, _branchBarrierFactory, "", gid);

            var req = new Empty();

            msg.Add(busi + "/TransOut", req);

            var db = new MockDbConnection();
            db.Mocks.When(x => x.CommandText.Contains("insert", StringComparison.OrdinalIgnoreCase)).ReturnsScalar(cmd => 1);
            db.Mocks.When(x => x.CommandText.Contains("select", StringComparison.OrdinalIgnoreCase)).ReturnsScalar(cmd => "rollback");

            var mockBusiCall = new Mock<Func<DbTransaction, Task<bool>>>();
            mockBusiCall.Setup(x => x.Invoke(It.IsAny<DbTransaction>())).Throws(new Exception("ex"));

            await Assert.ThrowsAsync<Exception>(async () => await msg.DoAndSubmitDB(busi + "/query", db, mockBusiCall.Object));
            dtmClient.Verify(x => x.InvokeBranch<Empty, Empty>(It.IsAny<TransBase>(), It.IsAny<Empty>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }
    }
}
