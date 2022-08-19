using DtmSERedisBarrier;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Dtmgrpc.IntegrationTests
{
    public class MsgGrpcRedisBarrierTest
    {
        [Fact, TestPriority(0)]
        public async Task Submit_Should_Succeed()
        {
            var provider = ITTestHelper.AddDtmGrpc();
            var transFactory = provider.GetRequiredService<IDtmTransFactory>();

            // init balance 
            var b1 = 1000;
            var b2 = 0;
            await SetBalanceByUID(1, b1);
            await SetBalanceByUID(2, b2);

            var gid = "msg-Submit_Should_Succeed" + DateTime.Now.ToString("MMddHHmmss");
            var msg = transFactory.NewMsgGrpc(gid);
            var req = ITTestHelper.GenBusiReq(false, false, 30);
            var busiGrpc = ITTestHelper.BuisgRPCUrl;
            msg.Add(busiGrpc + "/busi.Busi/TransInRedis", req);

            await msg.DoAndSubmit(busiGrpc + "/busi.Busi/QueryPreparedRedis", async bb => 
            {
                await bb.RedisCheckAdjustAmount(await ITTestHelper.GetRedis(), ITTestHelper.GetRedisAccountKey(1), -30, 86400);
            });

            await Task.Delay(2000);
            var status = await ITTestHelper.GetTranStatus(gid);
            Assert.Equal("succeed", status);

            var b11 = await GetBalanceByUID(1);
            var b21 = await GetBalanceByUID(2);

            Assert.NotEqual(b1, b11);
            Assert.Equal(b1 + b2, b11 + b21);
        }

        [Fact, TestPriority(1)]
        public async Task Submit_Should_Failed()
        {
            var provider = ITTestHelper.AddDtmGrpc();
            var transFactory = provider.GetRequiredService<IDtmTransFactory>();

            // init balance 
            var b1 = 1000;
            var b2 = 0;
            await SetBalanceByUID(1, b1);
            await SetBalanceByUID(2, b2);

            var gid = "msg-Submit_Should_Failed" + DateTime.Now.ToString("MMddHHmmss");
            var msg = transFactory.NewMsgGrpc(gid);
            var req = ITTestHelper.GenBusiReq(false, false, 30);
            var busiGrpc = ITTestHelper.BuisgRPCUrl;
            msg.Add(busiGrpc + "/busi.Busi/TransInRedis", req);

            await Assert.ThrowsAnyAsync<Exception>(async () =>
            {
                await msg.DoAndSubmit(busiGrpc + "/busi.Busi/QueryPreparedRedis", async bb =>
                {
                    await Task.CompletedTask;
                    throw new Exception("ex");
                });
            });

            var b11 = await GetBalanceByUID(1);
            var b21 = await GetBalanceByUID(2);

            Assert.Equal(b1, b11);
            Assert.Equal(b2, b21);
        }


        private async Task<int> GetBalanceByUID(int uid)
        {
            var db = await ITTestHelper.GetRedis();
            var key = ITTestHelper.GetRedisAccountKey(uid);
            var res = await db.StringGetAsync(key);
            if (res.TryParse(out int val))
            {
                return val;
            }

            throw new Exception("ex ex ex");
        }

        private async Task SetBalanceByUID(int uid, int amount)
        {
            var db = await ITTestHelper.GetRedis();
            var key = ITTestHelper.GetRedisAccountKey(uid);
            await db.StringSetAsync(key, amount);
        }
    }
}
