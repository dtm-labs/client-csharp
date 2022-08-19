using busi;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MySqlConnector;
using System.Text.Json;
using Dapper;
using System.Data.Common;
using DtmSERedisBarrier;

namespace BusiGrpcService.Services
{
    public class BusiApiService : Busi.BusiBase
    {

        private readonly ILogger<BusiApiService> _logger;
        private readonly Dtmgrpc.IDtmgRPCClient _client;
        private readonly Dtmgrpc.IBranchBarrierFactory _barrierFactory;

        public BusiApiService(ILogger<BusiApiService> logger, Dtmgrpc.IDtmgRPCClient client, Dtmgrpc.IBranchBarrierFactory barrierFactory)
        { 
            this._logger = logger;
            this._client = client;
            this._barrierFactory = barrierFactory;
        }

        public override async Task<Empty> TransIn(BusiReq request, ServerCallContext context)
        {
            _logger.LogInformation("TransIn req={req}", JsonSerializer.Serialize(request));

            if (string.IsNullOrWhiteSpace(request.TransInResult) || request.TransInResult.Equals("SUCCESS"))
            {
                await Task.CompletedTask;
                return new Empty();
            }
            else if (request.TransInResult.Equals("FAILURE"))
            {
                throw new Grpc.Core.RpcException(new Status(StatusCode.Aborted, "FAILURE"));
            }
            else if (request.TransInResult.Equals("ONGOING"))
            {
                throw new Grpc.Core.RpcException(new Status(StatusCode.FailedPrecondition, "ONGOING"));
            }

            throw new Grpc.Core.RpcException(new Status(StatusCode.Internal, $"unknow result {request.TransInResult}"));
        }

        public override async Task<Empty> TransInTcc(BusiReq request, ServerCallContext context)
        {
            _logger.LogInformation("TransIn req={req}", JsonSerializer.Serialize(request));

            if (string.IsNullOrWhiteSpace(request.TransInResult) || request.TransInResult.Equals("SUCCESS"))
            {
                await Task.CompletedTask;
                return new Empty();
            }
            else if (request.TransInResult.Equals("FAILURE"))
            {
                throw new Grpc.Core.RpcException(new Status(StatusCode.Aborted, "FAILURE"));
            }
            else if (request.TransInResult.Equals("ONGOING"))
            {
                throw new Grpc.Core.RpcException(new Status(StatusCode.FailedPrecondition, "ONGOING"));
            }

            throw new Grpc.Core.RpcException(new Status(StatusCode.Internal, $"unknow result {request.TransInResult}"));
        }

        public override async Task<Empty> TransInConfirm(BusiReq request, ServerCallContext context)
        {
            var tb = _client.TransBaseFromGrpc(context);

            _logger.LogInformation("TransInConfirm tb={tb}, req={req}", JsonSerializer.Serialize(tb), JsonSerializer.Serialize(request));
            await Task.CompletedTask;
            return new Empty();
        }

        public override async Task<Empty> TransInRevert(BusiReq request, ServerCallContext context)
        {
            var tb = _client.TransBaseFromGrpc(context);

            _logger.LogInformation("TransInRevert tb={tb}, req={req}", JsonSerializer.Serialize(tb), JsonSerializer.Serialize(request));
            await Task.CompletedTask;
            return new Empty();
        }

        public override async Task<Empty> TransOut(BusiReq request, ServerCallContext context)
        {
            _logger.LogInformation("TransOut req={req}", JsonSerializer.Serialize(request));
            await Task.CompletedTask;
            return new Empty();
        }

        public override async Task<Empty> TransOutTcc(BusiReq request, ServerCallContext context)
        {
            _logger.LogInformation("TransOut req={req}", JsonSerializer.Serialize(request));
            await Task.CompletedTask;
            return new Empty();
        }

        public override async Task<Empty> TransOutConfirm(BusiReq request, ServerCallContext context)
        {
            var tb = _client.TransBaseFromGrpc(context);

            _logger.LogInformation("TransOutConfirm tb={tb}, req={req}", JsonSerializer.Serialize(tb), JsonSerializer.Serialize(request));
            await Task.CompletedTask;
            return new Empty();
        }

        public override async Task<Empty> TransOutRevert(BusiReq request, ServerCallContext context)
        {
            var tb = _client.TransBaseFromGrpc(context);

            _logger.LogInformation("TransOutRevert tb={tb}, req={req}", JsonSerializer.Serialize(tb), JsonSerializer.Serialize(request));
            await Task.CompletedTask;
            return new Empty();
        }

        public override async Task<BusiReply> QueryPrepared(BusiReq request, ServerCallContext context)
        {
            var tb = _client.TransBaseFromGrpc(context);

            _logger.LogInformation("TransOutRevert tb={tb}, req={req}", JsonSerializer.Serialize(tb), JsonSerializer.Serialize(request));

            Exception ex = null;

            if (request.TransInResult.Contains("qp-yes") || string.IsNullOrWhiteSpace(request.TransInResult))
            {
                await Task.CompletedTask;
                return new BusiReply { Message = "a sample data" };
            }
            else if(request.TransInResult.Contains("qp-failure"))
            {
                ex = Dtmgrpc.DtmGImp.Utils.String2DtmError("FAILURE");
            }
            else if (request.TransInResult.Contains("qp-ongoing"))
            {
                ex = Dtmgrpc.DtmGImp.Utils.String2DtmError("ONGOING");
            }

            throw Dtmgrpc.DtmGImp.Utils.DtmError2GrpcError(ex);
        }

        public override async Task<Empty> TransInRedis(BusiReq request, ServerCallContext context)
        {
            _logger.LogInformation("TransInRedis req={req}", JsonSerializer.Serialize(request));

            var barrier = _barrierFactory.CreateBranchBarrier(context);
            
            await DoSomethingWithgRpcException(async () =>
            {
                await barrier.RedisCheckAdjustAmount(await GetRedis(), GetRedisAccountKey(TransInUID), (int)request.Amount, 86400);
            });

            return new Empty();
        }

        public override async Task<Empty> TransInRevertRedis(BusiReq request, ServerCallContext context)
        {
            var barrier = _barrierFactory.CreateBranchBarrier(context);
            
            await DoSomethingWithgRpcException(async () =>
            {
                await barrier.RedisCheckAdjustAmount(await GetRedis(), GetRedisAccountKey(TransInUID), -(int)request.Amount, 86400);
            });

            return new Empty();
        }

        public override async Task<Empty> TransOutRedis(BusiReq request, ServerCallContext context)
        {
            var barrier = _barrierFactory.CreateBranchBarrier(context);
            
            await DoSomethingWithgRpcException(async () =>
            {
                await barrier.RedisCheckAdjustAmount(await GetRedis(), GetRedisAccountKey(TransOutUID), -(int)request.Amount, 86400);
            });

            return new Empty();
        }

        public override async Task<Empty> TransOutRevertRedis(BusiReq request, ServerCallContext context)
        {
            var barrier = _barrierFactory.CreateBranchBarrier(context);

            await DoSomethingWithgRpcException(async () => 
            {
                await barrier.RedisCheckAdjustAmount(await GetRedis(), GetRedisAccountKey(TransOutUID), (int)request.Amount, 86400);
            });
            
            return new Empty();
        }

        public override async Task<Empty> QueryPreparedRedis(BusiReq request, ServerCallContext context)
        {
            var barrier = _barrierFactory.CreateBranchBarrier(context);

            await DoSomethingWithgRpcException(async () =>
            {
                await barrier.RedisQueryPrepared(await GetRedis(), 86400);
            });

            return new Empty();
        }

        private static readonly int TransOutUID = 1;

        private static readonly int TransInUID = 2;

        public override async Task<Empty> TransInBSaga(BusiReq request, ServerCallContext context)
        {
            var barrier = _barrierFactory.CreateBranchBarrier(context);

            using (MySqlConnection conn = GetBarrierConn())
            {
                await barrier.Call(conn, async (tx) =>
                {
                    await SagaGrpcAdjustBalance(conn, tx, TransInUID, (int)request.Amount, request.TransInResult);
                });
            }

            return new Empty();
        }

        public override async Task<Empty> TransOutBSaga(BusiReq request, ServerCallContext context)
        {
            var barrier = _barrierFactory.CreateBranchBarrier(context);

            using (MySqlConnection conn = GetBarrierConn())
            {
                await barrier.Call(conn, async (tx) =>
                {
                    await SagaGrpcAdjustBalance(conn, tx, TransOutUID, -(int)request.Amount, request.TransOutResult);
                });
            }

            return new Empty();
        }

        public override async Task<Empty> TransInRevertBSaga(BusiReq request, ServerCallContext context)
        {
            var barrier = _barrierFactory.CreateBranchBarrier(context);

            using (MySqlConnection conn = GetBarrierConn())
            {
                await barrier.Call(conn, async (tx) =>
                {
                    await SagaGrpcAdjustBalance(conn, tx, TransInUID, -(int)request.Amount, "");
                });
            }

            return new Empty();
        }

        public override async Task<Empty> TransOutRevertBSaga(BusiReq request, ServerCallContext context)
        {
            var barrier = _barrierFactory.CreateBranchBarrier(context);

            using (MySqlConnection conn = GetBarrierConn())
            {
                await barrier.Call(conn, async (tx) =>
                {
                    await SagaGrpcAdjustBalance(conn, tx, TransOutUID, (int)request.Amount, "");
                });
            }

            return new Empty();
        }

        private MySqlConnection GetBarrierConn() => new("Server=localhost;port=3306;User ID=root;Password=123456;Database=dtm_barrier");

        private async Task<StackExchange.Redis.IDatabase> GetRedis()
        {
            // NOTE: this redis connection code is only for sample, don't use in production
            var config = StackExchange.Redis.ConfigurationOptions.Parse("localhost:6379");
            var conn = await StackExchange.Redis.ConnectionMultiplexer.ConnectAsync(config);
            return conn.GetDatabase();
        }

        private async Task SagaGrpcAdjustBalance(DbConnection conn, DbTransaction tx, int uid, int amount, string result)
        {
            _logger.LogInformation("SagaGrpcAdjustBalance uid={uid}, amount={amount}, result={result}", uid, amount, result);

            if (result.Equals("FAILURE"))
            {
                throw new RpcException(new Status(StatusCode.Aborted, "FAILURE"));
            }

            await conn.ExecuteAsync(
                sql: "update dtm_busi.user_account set balance = balance + @balance where user_id = @user_id",
                param: new { balance = amount, user_id = uid },
                transaction: tx);
        }

        private string GetRedisAccountKey(int uid) => $"dtm:busi:redis-account-key-{uid}";

        private async Task DoSomethingWithgRpcException(Func<Task> func)
        {
            try
            {
                await func();
            }
            catch (Exception ex)
            {
                Dtmgrpc.DtmGImp.Utils.DtmError2GrpcError(ex);
            }
        }
    }   
}