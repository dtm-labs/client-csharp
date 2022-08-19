using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Dtmgrpc.IntegrationTests
{
    public class SagaGrpcBarrierTest
    {
        [Fact]
        public async Task Submit_Should_Succeed()
        {
            var provider = ITTestHelper.AddDtmGrpc();
            var transFactory = provider.GetRequiredService<IDtmTransFactory>();

            var gid = "sagaTestGid" + Guid.NewGuid().ToString();
            var saga = GenSagaGrpc(transFactory, gid, false, false);
            await saga.Submit();

            await Task.Delay(10000);
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

            await Task.Delay(10000);
            var status = await ITTestHelper.GetTranStatus(gid);
            Assert.Equal("failed", status);
        }
    
        private SagaGrpc GenSagaGrpc(IDtmTransFactory transFactory, string gid, bool outFailed, bool inFailed)
        {
            var saga = transFactory.NewSagaGrpc(gid);
            var req = ITTestHelper.GenBusiReq(outFailed, inFailed);
            var busiGrpc = ITTestHelper.BuisgRPCUrl;
            saga.Add(busiGrpc + "/busi.Busi/TransOutBSaga", busiGrpc + "/busi.Busi/TransOutRevertBSaga", req);
            saga.Add(busiGrpc + "/busi.Busi/TransInBSaga", busiGrpc + "/busi.Busi/TransInRevertBSaga", req);
            return saga;
        }
    }
}
