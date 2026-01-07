using Microsoft.Extensions.DependencyInjection;
using System;
using System.Data.Common;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using Grpc.Core;
using MySqlConnector;
using Xunit;

namespace Dtmgrpc.IntegrationTests
{
    public class MsgHttpTest
    {
        [Fact]
        public async Task Submit_Should_Succeed()
        {
            var provider = ITTestHelper.AddDtmHttp();
            var transFactory = provider.GetRequiredService<Dtmcli.IDtmTransFactory>();

            var gid = "msgTestGid" + Guid.NewGuid().ToString();
            var msg = transFactory.NewMsg(gid);
            msg.EnableWaitResult();
            var req = ITTestHelper.GenBusiReq(false, false);
            var busiGrpc = ITTestHelper.BuisHttpUrl;
            msg.Add(busiGrpc + "/busi.Busi/TransOut", req)
                .Add(busiGrpc + "/busi.Busi/TransIn", req);

            await msg.Prepare(busiGrpc + "/busi.Busi/QueryPrepared_404");
            await msg.Submit();

            var status = await ITTestHelper.GetTranStatus(gid);
            Assert.Equal("succeed", status);
        }

        [Fact]
        public async Task Submit_With_NextCronTime_Should_Succeed_Later()
        {
            var provider = ITTestHelper.AddDtmHttp();
            var transFactory = provider.GetRequiredService<Dtmcli.IDtmTransFactory>();

            var gid = "msgTestGid" + Guid.NewGuid().ToString();
            var msg = transFactory.NewMsg(gid, DateTime.Now.AddSeconds(10));
            var req = ITTestHelper.GenBusiReq(false, false);
            var busiGrpc = ITTestHelper.BuisHttpUrl;
            msg.Add(busiGrpc + "/busi.Busi/TransOut", req)
                .Add(busiGrpc + "/busi.Busi/TransIn", req);

            await msg.Prepare(busiGrpc + "/busi.Busi/QueryPrepared_404");
            await msg.Submit();

            // Since the downstream execution is delayed by 10 seconds, it will be 'submitted' after 2 seconds and 'succeed' after 15 seconds
            await Task.Delay(TimeSpan.FromSeconds(0));
            var status = await ITTestHelper.GetTranStatus(gid);
            Assert.Equal("submitted", status);

            await Task.Delay(TimeSpan.FromSeconds(2));
            status = await ITTestHelper.GetTranStatus(gid);
            Assert.Equal("submitted", status);

            await Task.Delay(TimeSpan.FromSeconds(13));
            status = await ITTestHelper.GetTranStatus(gid);
            Assert.Equal("succeed", status);
        }
    }
}