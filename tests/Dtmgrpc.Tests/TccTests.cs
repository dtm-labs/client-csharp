using DtmCommon;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Collections.Generic;
using Xunit;

namespace Dtmgrpc.Tests
{
    public class TccTests
    {
        [Fact]
        public async void Execute_Should_Submit()
        {
            var dtmClient = new Mock<IDtmgRPCClient>();
            TransMockHelper.MockTransCallDtm(dtmClient, Constant.Op.Prepare, false);
            TransMockHelper.MockRegisterBranch(dtmClient, false);
            TransMockHelper.MockTransRequestBranch(dtmClient, false);

            var gid = "tcc_gid";

            var transFactory = new Mock<IDtmTransFactory>();
            transFactory.Setup(x => x.NewTccGrpc(It.IsAny<string>())).Returns(new TccGrpc(dtmClient.Object, TransBase.NewTransBase(gid, "tcc", "", "")));
            
            var globalTrans = new TccGlobalTransaction(dtmClient.Object, NullLoggerFactory.Instance, transFactory.Object);
            var res = await globalTrans.Excecute(gid, async (tcc) =>
            {
                await tcc.CallBranch<Empty, Empty>(new Empty(), "localhost:9999/svc/TransOutTry", "localhost:9999/svc/TransOutConfirm", "localhost:9999/svc/TransOutCancel");
                await tcc.CallBranch<Empty, Empty>(new Empty(), "localhost:9999/svc/TransInTry", "localhost:9999/svc/TransInConfirm", "localhost:9999/svc/TransInCancel");
            });

            Assert.Equal(gid, res);
        }

        [Fact]
        public async void Execute_Should_Abort_When_CallBranch_With_Exception()
        {
            var dtmClient = new Mock<IDtmgRPCClient>();
            TransMockHelper.MockTransCallDtm(dtmClient, Constant.Op.Prepare, false);
            TransMockHelper.MockTransCallDtm(dtmClient, Constant.Op.Abort, false);
            TransMockHelper.MockRegisterBranch(dtmClient, false);
            TransMockHelper.MockTransRequestBranch(dtmClient, true);

            var gid = "tcc_gid";

            var transFactory = new Mock<IDtmTransFactory>();
            transFactory.Setup(x => x.NewTccGrpc(It.IsAny<string>())).Returns(new TccGrpc(dtmClient.Object, TransBase.NewTransBase(gid, "tcc", "", "")));

            var globalTrans = new TccGlobalTransaction(dtmClient.Object, NullLoggerFactory.Instance, transFactory.Object);
            var res = await globalTrans.Excecute(gid, async (tcc) =>
            {
                await tcc.CallBranch<Empty, Empty>(new Empty(), "localhost:9999/svc/TransOutTry", "localhost:9999/svc/TransOutConfirm", "localhost:9999/svc/TransOutCancel");
                await tcc.CallBranch<Empty, Empty>(new Empty(), "localhost:9999/svc/TransInTry", "localhost:9999/svc/TransInConfirm", "localhost:9999/svc/TransInCancel");
            });

            Assert.Empty(res);
            dtmClient.Verify(x => x.DtmGrpcCall(It.IsAny<TransBase>(), Constant.Op.Abort), Times.Once);
        }

        [Fact]
        public async void Set_TransOptions_Should_Succeed()
        {
            var dtmClient = new Mock<IDtmgRPCClient>();
            TransMockHelper.MockTransCallDtm(dtmClient, Constant.Op.Prepare, false);
            TransMockHelper.MockRegisterBranch(dtmClient, false);
            TransMockHelper.MockTransRequestBranch(dtmClient, false);

            var gid = "tcc_gid";

            var transFactory = new Mock<IDtmTransFactory>();
            transFactory.Setup(x => x.NewTccGrpc(It.IsAny<string>())).Returns(new TccGrpc(dtmClient.Object, TransBase.NewTransBase(gid, "tcc", "", "")));

            var globalTrans = new TccGlobalTransaction(dtmClient.Object, NullLoggerFactory.Instance, transFactory.Object);
            var res = await globalTrans.Excecute(gid, tcc =>
            {
                tcc.EnableWaitResult();
                tcc.SetRetryInterval(10);
                tcc.SetTimeoutToFail(100);
                tcc.SetBranchHeaders(new Dictionary<string, string>
                {
                    { "bh1", "123" },
                    { "bh2", "456" },
                });
            }, async (tcc) =>
            {
                await tcc.CallBranch<Empty, Empty>(new Empty(), "localhost:9999/svc/TransOutTry", "localhost:9999/svc/TransOutConfirm", "localhost:9999/svc/TransOutCancel");
                await tcc.CallBranch<Empty, Empty>(new Empty(), "localhost:9999/svc/TransInTry", "localhost:9999/svc/TransInConfirm", "localhost:9999/svc/TransInCancel");

                var transBase = tcc.GetTransBase();

                Assert.True(transBase.WaitResult);
                Assert.Equal(10, transBase.RetryInterval);
                Assert.Equal(100, transBase.TimeoutToFail);
                Assert.Contains("bh1", transBase.BranchHeaders.Keys);
                Assert.Contains("bh2", transBase.BranchHeaders.Keys);
            });

            Assert.Equal(gid, res);
        }
    }
}
