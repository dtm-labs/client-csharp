using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Dtmgrpc.IntegrationTests
{
    public class SagaGrpcTest
    {
        [Fact]
        public async Task Submit_Should_Succeed()
        {
            var provider = ITTestHelper.AddDtmGrpc();
            var transFactory = provider.GetRequiredService<IDtmTransFactory>();

            var gid = "sagaTestGid" + Guid.NewGuid().ToString();
            var saga = GenSagaGrpc(transFactory, gid, false, false);
            await saga.Submit();

            await Task.Delay(2000);
            var status = await ITTestHelper.GetTranStatus(gid);
            Assert.Equal("succeed", status);
        }

        [Fact]
        public async Task Rollback_Should_Succeed()
        {
            var provider = ITTestHelper.AddDtmGrpc();
            var transFactory = provider.GetRequiredService<IDtmTransFactory>();

            var gid = "sagaTestGid" + Guid.NewGuid().ToString();
            var saga = GenSagaGrpc(transFactory, gid, false, true);
            await saga.Submit();

            await Task.Delay(2000);
            var status = await ITTestHelper.GetTranStatus(gid);
            Assert.Equal("failed", status);
        }

        [Fact]
        public async Task WaitResult_Should_Succeed()
        {
            var provider = ITTestHelper.AddDtmGrpc();
            var transFactory = provider.GetRequiredService<IDtmTransFactory>();

            var gid = "sagaTestGid" + Guid.NewGuid().ToString();
            var saga = GenSagaGrpc(transFactory, gid, false, false);
            saga.EnableWaitResult();
            await saga.Submit();
            
            var status = await ITTestHelper.GetTranStatus(gid);
            Assert.Equal("succeed", status);
        }

        private SagaGrpc GenSagaGrpc(IDtmTransFactory transFactory, string gid, bool outFailed, bool inFailed)
        {
            var saga = transFactory.NewSagaGrpc(gid);
            var req = ITTestHelper.GenBusiReq(outFailed, inFailed);
            var busiGrpc = ITTestHelper.BuisgRPCUrl;
            saga.Add(busiGrpc + "/busi.Busi/TransOut", busiGrpc + "/busi.Busi/TransOutRevert", req);
            saga.Add(busiGrpc + "/busi.Busi/TransIn", busiGrpc + "/busi.Busi/TransInRevert", req);
            return saga;
        }
    }
}
