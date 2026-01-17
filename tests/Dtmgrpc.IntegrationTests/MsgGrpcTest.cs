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
    public class MsgGrpcTest
    {
        [Fact]
        public async Task Submit_Should_Succeed()
        {
            var provider = ITTestHelper.AddDtmGrpc();
            var transFactory = provider.GetRequiredService<IDtmTransFactory>();

            var gid = "msgTestGid" + Guid.NewGuid().ToString();
            var msg = transFactory.NewMsgGrpc(gid);
            msg.EnableWaitResult();
            var req = ITTestHelper.GenBusiReq(false, false);
            var busiGrpc = ITTestHelper.BuisgRPCUrl;
            msg.Add(busiGrpc + "/busi.Busi/TransOut", req)
               .Add(busiGrpc + "/busi.Busi/TransIn", req);

            await msg.Prepare(busiGrpc + "/busi.Busi/QueryPrepared");
            await msg.Submit();
            
            var status = await ITTestHelper.GetTranStatus(gid);
            Assert.Equal("succeed", status);
        }

        [Fact]
        public async Task DoAndSubmit_Should_Succeed()
        {
            var provider = ITTestHelper.AddDtmGrpc();
            var transFactory = provider.GetRequiredService<IDtmTransFactory>();

            var gid = "msgTestGid" + Guid.NewGuid().ToString();
            var msg = transFactory.NewMsgGrpc(gid);
            var req = ITTestHelper.GenBusiReq(false, false);
            var busiGrpc = ITTestHelper.BuisgRPCUrl;

            msg.Add(busiGrpc + "/busi.Busi/TransIn", req);
            // do TransOut local, then TransIn with DTM.

            await msg.DoAndSubmit(busiGrpc + "/busi.Busi/QueryPreparedMySqlReal", async branchBarrier =>
            {
                MySqlConnection conn = getBarrierMySqlConnection();
                await branchBarrier.Call(conn, () =>
                    {
                        Task task = this.LocalAdjustBalance(conn, TransOutUID, -req.Amount, "SUCCESS");
                        return task;
                    },
                    TransactionScopeOption.Required,
                    IsolationLevel.ReadCommitted);
            });

            await Task.Delay(2000);
            var status = await ITTestHelper.GetTranStatus(gid);
            Assert.Equal("succeed", status);
        }
        
        [Fact]
        public async Task Submit_With_EffectTime_Should_Succeed_Later()
        {
            var provider = ITTestHelper.AddDtmGrpc();
            var transFactory = provider.GetRequiredService<IDtmTransFactory>();

            var gid = "msgTestGid" + Guid.NewGuid().ToString();
            DateTime effectTime = DateTime.Now.AddSeconds(10);
            var msg = transFactory.NewMsgGrpc(gid);
            var req = ITTestHelper.GenBusiReq(false, false);
            req.EffectTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(effectTime.ToUniversalTime());
            var busiGrpc = ITTestHelper.BuisgRPCUrl;
            msg.Add(busiGrpc + "/busi.Busi/TransOut", req)
                .Add(busiGrpc + "/busi.Busi/TransIn", req);

            await msg.Prepare(busiGrpc + "/busi.Busi/QueryPrepared");
            await msg.Submit();

            // Since the downstream execution is delayed by 10 seconds, it will be 'submitted' after 2 seconds and 'succeed' after 15 seconds
            await Task.Delay(TimeSpan.FromSeconds(2));
            var status = await ITTestHelper.GetTranStatus(gid);
            Assert.Equal("submitted", status);
            
            await Task.Delay(TimeSpan.FromSeconds(13));
            status = await ITTestHelper.GetTranStatus(gid);
            Assert.Equal("succeed", status);
        }
        
        [Fact]
        public async Task Submit_With_Delay_Should_Succeed_Later()
        {
            var provider = ITTestHelper.AddDtmGrpc();
            var transFactory = provider.GetRequiredService<IDtmTransFactory>();

            var gid = "msgTestGid" + Guid.NewGuid().ToString();
            var msg = transFactory.NewMsgGrpc(gid);
            msg.SetDelay(TimeSpan.FromSeconds(10));
            var req = ITTestHelper.GenBusiReq(false, false);
            var busiGrpc = ITTestHelper.BuisgRPCUrl;
            msg.Add(busiGrpc + "/busi.Busi/TransOut", req)
                .Add(busiGrpc + "/busi.Busi/TransIn", req);

            await msg.Prepare(busiGrpc + "/busi.Busi/QueryPrepared");
            await msg.Submit();

            // Since the downstream execution is delayed by 10 seconds, it will be 'submitted' after 2 seconds and 'succeed' after 15 seconds
            await Task.Delay(TimeSpan.FromSeconds(2));
            var status = await ITTestHelper.GetTranStatus(gid);
            Assert.Equal("submitted", status);
            
            await Task.Delay(TimeSpan.FromSeconds(13));
            status = await ITTestHelper.GetTranStatus(gid);
            Assert.Equal("succeed", status);
        }

        private static readonly int TransOutUID = 1;

        private static readonly int TransInUID = 2;

        private MySqlConnection getBarrierMySqlConnection() => new("Server=localhost;port=3306;User ID=root;Password=;Database=dtm_barrier");

        private async Task LocalAdjustBalance(DbConnection conn, int uid, long amount, string result)
        {
            // _logger.LogInformation("AdjustBalanceLocal uid={uid}, amount={amount}, result={result}", uid, amount, result);

            if (result.Equals("FAILURE"))
            {
                throw new RpcException(new Status(StatusCode.Aborted, "FAILURE"));
            }

            await conn.ExecuteAsync(
                sql: "update dtm_busi.user_account set balance = balance + @balance where user_id = @user_id",
                param: new { balance = amount, user_id = uid }
            );
        }
    }
}
