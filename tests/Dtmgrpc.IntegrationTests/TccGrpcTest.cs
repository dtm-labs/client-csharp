using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Dtmgrpc.IntegrationTests
{
    public class TccGrpcTest
    {
        [Fact]
        public async Task Execute_Should_Succeed()
        {
            var provider = ITTestHelper.AddDtmGrpc();
            var globalTransaction = provider.GetRequiredService<TccGlobalTransaction>();
            
            var gid = "tccTestGid" + Guid.NewGuid().ToString();
            var req = ITTestHelper.GenBusiReq(false, false);
            var busiGrpc = ITTestHelper.BuisgRPCUrl;
            var res = await globalTransaction.Excecute(gid, async tcc =>
            {
                await tcc.CallBranch<busi.BusiReq, Empty>(req, busiGrpc + "/busi.Busi/TransOut", busiGrpc + "/busi.Busi/TransOutConfirm", busiGrpc + "/busi.Busi/TransOutRevert");
                await tcc.CallBranch<busi.BusiReq, Empty>(req, busiGrpc + "/busi.Busi/TransIn", busiGrpc + "/busi.Busi/TransInConfirm", busiGrpc + "/busi.Busi/TransInRevert");
            });

            Assert.Equal(gid, res);

            await Task.Delay(2000);
            var status = await ITTestHelper.GetTranStatus(gid);
            Assert.Equal("succeed", status);
        }

        [Fact]
        public async Task Rollback_Should_Succeed()
        {
            var provider = ITTestHelper.AddDtmGrpc();
            var globalTransaction = provider.GetRequiredService<TccGlobalTransaction>();

            var gid = "tccTestGid" + Guid.NewGuid().ToString();
            var req = ITTestHelper.GenBusiReq(false, true);
            var busiGrpc = ITTestHelper.BuisgRPCUrl;
            var res = await globalTransaction.Excecute(gid, async tcc =>
            {
                await tcc.CallBranch<busi.BusiReq, Empty>(req, busiGrpc + "/busi.Busi/TransOutTcc", busiGrpc + "/busi.Busi/TransOutConfirm", busiGrpc + "/busi.Busi/TransOutRevert");
                await tcc.CallBranch<busi.BusiReq, Empty>(req, busiGrpc + "/busi.Busi/TransInTcc", busiGrpc + "/busi.Busi/TransInConfirm", busiGrpc + "/busi.Busi/TransInRevert");
            });

            Assert.Empty(res);

            await Task.Delay(2000);
            var status = await ITTestHelper.GetTranStatus(gid);
            Assert.Equal("failed", status);
        }
    }
}
