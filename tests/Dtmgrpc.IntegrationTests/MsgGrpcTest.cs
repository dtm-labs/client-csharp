using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Dtmgrpc.IntegrationTests
{
    public class MsgGrpcTest
    {
        [Fact]
        public async Task Submit_Should_Succeed()
        {
            var provider = ITTestHelper.AddDtmGrpc();
            var transFactory = provider.GetRequiredService<IDtmTransFactory>();

            var gid = "msgTestGid" + Guid.NewGuid().ToString();
            var msg = transFactory.NewMsgGrpc(gid);
            var req = ITTestHelper.GenBusiReq(false, false);
            var busiGrpc = ITTestHelper.BuisgRPCUrl;
            msg.Add(busiGrpc + "/busi.Busi/TransOut", req)
               .Add(busiGrpc + "/busi.Busi/TransIn", req);

            await msg.Prepare(busiGrpc + "/busi.Busi/QueryPrepared");
            await msg.Submit();

            await Task.Delay(2000);
            var status = await ITTestHelper.GetTranStatus(gid);
            Assert.Equal("succeed", status);
        }
    }
}
