using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DtmCommon;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Dtmcli.Tests
{
    public class TccTests
    {
        [Fact]
        public async void Execute_Should_Submit()
        {
            var dtmClient = new Mock<IDtmClient>();
            dtmClient.Setup(x => x.GenGid(It.IsAny<CancellationToken>())).Returns(Task.FromResult("tcc_gid_gid"));
            TestHelper.MockTransCallDtm(dtmClient, Constant.Request.OPERATION_PREPARE, false);
            TestHelper.MockTransRegisterBranch(dtmClient, Constant.Request.OPERATION_REGISTERBRANCH, false);
            TestHelper.MockTransRequestBranch(dtmClient, System.Net.HttpStatusCode.OK);
            
            var globalTrans = new TccGlobalTransaction(dtmClient.Object, NullLoggerFactory.Instance);
            var res = await globalTrans.Excecute(async (tcc) =>
            {
                var res1 = await tcc.CallBranch(new { }, "http://localhost:9999/TransOutTry", "http://localhost:9999/TransOutConfirm", "http://localhost:9999/TransOutCancel", default);
                var res2 = await tcc.CallBranch(new { }, "http://localhost:9999/TransInTry", "http://localhost:9999/TransInConfirm", "http://localhost:9999/TransInCancel", default);
            });

            Assert.Equal("tcc_gid_gid", res);
        }

        [Fact]
        public async void Execute_With_GId_Should_Submit()
        {
            var dtmClient = new Mock<IDtmClient>();
            TestHelper.MockTransCallDtm(dtmClient, Constant.Request.OPERATION_PREPARE, false);
            TestHelper.MockTransRegisterBranch(dtmClient, Constant.Request.OPERATION_REGISTERBRANCH, false);
            TestHelper.MockTransRequestBranch(dtmClient, System.Net.HttpStatusCode.OK);

            var gid = "tcc_gid";
            var globalTrans = new TccGlobalTransaction(dtmClient.Object, NullLoggerFactory.Instance);
            var res = await globalTrans.Excecute(gid, async (tcc) =>
            {
                var res1 = await tcc.CallBranch(new { }, "http://localhost:9999/TransOutTry", "http://localhost:9999/TransOutConfirm", "http://localhost:9999/TransOutCancel", default);
                var res2 = await tcc.CallBranch(new { }, "http://localhost:9999/TransInTry", "http://localhost:9999/TransInConfirm", "http://localhost:9999/TransInCancel", default);
            });

            Assert.Equal(gid, res);
        }

        [Fact]
        public async void Execute_Should_Abort_When_CallBranch_With_Old_Ver_Exception()
        {
            var dtmClient = new Mock<IDtmClient>();
            TestHelper.MockTransCallDtm(dtmClient, Constant.Request.OPERATION_PREPARE, false);
            TestHelper.MockTransCallDtm(dtmClient, Constant.Request.OPERATION_ABORT, false);
            TestHelper.MockTransRegisterBranch(dtmClient, Constant.Request.OPERATION_REGISTERBRANCH, false);
            TestHelper.MockTransRequestBranch(dtmClient, System.Net.HttpStatusCode.OK, "FAILURE");

            var gid = "tcc_gid";
            var globalTrans = new TccGlobalTransaction(dtmClient.Object, NullLoggerFactory.Instance);
            var res = await globalTrans.Excecute(gid, async (tcc) =>
            {
                var res1 = await tcc.CallBranch(new { }, "http://localhost:9999/TransOutTry", "http://localhost:9999/TransOutConfirm", "http://localhost:9999/TransOutCancel", default);
                var res2 = await tcc.CallBranch(new { }, "http://localhost:9999/TransInTry", "http://localhost:9999/TransInConfirm", "http://localhost:9999/TransInCancel", default);
            });

            Assert.Empty(res);
            dtmClient.Verify(x => x.TransCallDtm(It.IsAny<TransBase>(), It.IsAny<object>(), Constant.Request.OPERATION_ABORT, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async void Execute_Should_Abort_When_CallBranch_With_New_Ver_Exception()
        {
            var dtmClient = new Mock<IDtmClient>();
            TestHelper.MockTransCallDtm(dtmClient, Constant.Request.OPERATION_PREPARE, false);
            TestHelper.MockTransCallDtm(dtmClient, Constant.Request.OPERATION_ABORT, false);
            TestHelper.MockTransRegisterBranch(dtmClient, Constant.Request.OPERATION_REGISTERBRANCH, false);
            TestHelper.MockTransRequestBranch(dtmClient, System.Net.HttpStatusCode.BadRequest);

            var gid = "tcc_gid";
            var globalTrans = new TccGlobalTransaction(dtmClient.Object, NullLoggerFactory.Instance);
            var res = await globalTrans.Excecute(gid, async (tcc) =>
            {
                var res1 = await tcc.CallBranch(new { }, "http://localhost:9999/TransOutTry", "http://localhost:9999/TransOutConfirm", "http://localhost:9999/TransOutCancel", default);
                var res2 = await tcc.CallBranch(new { }, "http://localhost:9999/TransInTry", "http://localhost:9999/TransInConfirm", "http://localhost:9999/TransInCancel", default);
            });

            Assert.Empty(res);
            dtmClient.Verify(x => x.TransCallDtm(It.IsAny<TransBase>(), It.IsAny<object>(), Constant.Request.OPERATION_ABORT, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async void Set_TransOptions_Should_Succeed()
        {
            var dtmClient = new Mock<IDtmClient>();
            TestHelper.MockTransCallDtm(dtmClient, Constant.Request.OPERATION_PREPARE, false);
            TestHelper.MockTransRegisterBranch(dtmClient, Constant.Request.OPERATION_REGISTERBRANCH, false);
            TestHelper.MockTransRequestBranch(dtmClient, System.Net.HttpStatusCode.OK);

            var gid = "tcc_gid";
            var globalTrans = new TccGlobalTransaction(dtmClient.Object, NullLoggerFactory.Instance);
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
                tcc.SetPassthroughHeaders(new List<string> { "bh1" });
            },  async (tcc) =>
            {
                var res1 = await tcc.CallBranch(new { }, "http://localhost:9999/TransOutTry", "http://localhost:9999/TransOutConfirm", "http://localhost:9999/TransOutCancel", default);
                var res2 = await tcc.CallBranch(new { }, "http://localhost:9999/TransInTry", "http://localhost:9999/TransInConfirm", "http://localhost:9999/TransInCancel", default);

                var transBase = tcc.GetTransBase();

                Assert.True(transBase.WaitResult);
                Assert.Equal(10, transBase.RetryInterval);
                Assert.Equal(100, transBase.TimeoutToFail);
                Assert.Contains("bh1", transBase.BranchHeaders.Keys);
                Assert.Contains("bh2", transBase.BranchHeaders.Keys);
                Assert.Contains("bh1", transBase.PassthroughHeaders);
            });

            Assert.Equal(gid, res);
        }
    }
}