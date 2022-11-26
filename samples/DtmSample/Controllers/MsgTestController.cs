using Dtmcli;
using DtmMongoBarrier;
using DtmSample.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using System.Threading;
using System.Threading.Tasks;

namespace DtmSample.Controllers
{
    /// <summary>
    /// MSG 示例
    /// </summary>
    [ApiController]
    [Route("/api")]
    public class MsgTestController : ControllerBase
    {
        private readonly ILogger<MsgTestController> _logger;
        private readonly IDtmClient _dtmClient;
        private readonly IBranchBarrierFactory _factory;
        private readonly IDtmTransFactory _transFactory;
        private readonly AppSettings _settings;

        public MsgTestController(ILogger<MsgTestController> logger, IOptions<AppSettings> optionsAccs, IDtmClient dtmClient, IBranchBarrierFactory factory, IDtmTransFactory transFactory)
        {
            _logger = logger;
            _settings = optionsAccs.Value;
            _factory = factory;
            _dtmClient = dtmClient;
            _transFactory = transFactory;
        }

        private MySqlConnection GetConn() => new(_settings.BarrierConn);

        private MySqlConnection GetErrConn() => new("");

        /// <summary>
        /// MSG 常规成功
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost("msg")]
        public async Task<IActionResult> Msg(CancellationToken cancellationToken)
        {
            var gid = await _dtmClient.GenGid(cancellationToken);

            var msg = _transFactory.NewMsg(gid)
                .Add(_settings.BusiUrl + "/TransOut", new TransRequest("1", -30))
                .Add(_settings.BusiUrl + "/TransIn", new TransRequest("2", 30));

            await msg.Prepare(_settings.BusiUrl + "/msg-queryprepared", cancellationToken);
            await msg.Submit(cancellationToken);

            _logger.LogInformation("result gid is {0}", gid);

            return Ok(TransResponse.BuildSucceedResponse());
        }

        /// <summary>
        /// MSG DoAndSubmitDB (mysql)
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost("msg-db-mysql")]
        public async Task<IActionResult> MsgDbMySql(CancellationToken cancellationToken)
        {
            var gid = await _dtmClient.GenGid(cancellationToken);

            var msg = _transFactory.NewMsg(gid)
                .Add(_settings.BusiUrl + "/TransOut", new TransRequest("1", -30))
                .Add(_settings.BusiUrl + "/TransIn", new TransRequest("2", 30));

            using (MySqlConnection conn = GetConn())
            {
                await msg.DoAndSubmitDB(_settings.BusiUrl + "/msg-queryprepared", conn, async tx =>
                {
                    await Task.CompletedTask;
                });
            }

            _logger.LogInformation("result gid is {0}", gid);

            return Ok(TransResponse.BuildSucceedResponse());
        }

        /// <summary>
        /// MSG DoAndSubmit (mongo)
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost("msg-db-mongo")]
        public async Task<IActionResult> MsgDbMongo(CancellationToken cancellationToken)
        {
            var gid = await _dtmClient.GenGid(cancellationToken);

            var msg = _transFactory.NewMsg(gid)
                .Add(_settings.BusiUrl + "/TransOut", new TransRequest("1", -30))
                .Add(_settings.BusiUrl + "/TransIn", new TransRequest("2", 30));

            MongoDB.Driver.IMongoClient cli = new MongoDB.Driver.MongoClient(_settings.MongoBarrierConn);
            await msg.DoAndSubmit(_settings.BusiUrl + "/msg-mongoqueryprepared", async bb => 
            {
                await bb.MongoCall(cli, async x => 
                {
                    await Task.CompletedTask;
                });
            });
           
            _logger.LogInformation("result gid is {0}", gid);

            return Ok(TransResponse.BuildSucceedResponse());
        }

        /// <summary>
        /// MSG EnableWaitResult
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost("msg-waitresult")]
        public async Task<IActionResult> MsgWaitResult(CancellationToken cancellationToken)
        {
            var gid = await _dtmClient.GenGid(cancellationToken);

            var msg = _transFactory.NewMsg(gid)
                .Add(_settings.BusiUrl + "/TransOut", new TransRequest("1", -30))
                .Add(_settings.BusiUrl + "/TransIn", new TransRequest("2", 30))
                .EnableWaitResult();

            await msg.Prepare(_settings.BusiUrl + "/msg-queryprepared", cancellationToken);
            await msg.Submit(cancellationToken);

            _logger.LogInformation("result gid is {0}", gid);

            return Ok(TransResponse.BuildSucceedResponse());
        }

        /// <summary>
        /// MSG QueryPrepared(mysql)
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpGet("msg-queryprepared")]
        public async Task<IActionResult> MsgMySqlQueryPrepared(CancellationToken cancellationToken)
        {
            var bb = _factory.CreateBranchBarrier(Request.Query);
            _logger.LogInformation("bb {0}", bb);
            using (MySqlConnection conn = GetConn())
            {
                var res = await bb.QueryPrepared(conn);

                return Ok(new { dtm_result = res });
            }
        }

        /// <summary>
        /// MSG QueryPrepared(mongo)
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpGet("msg-mongoqueryprepared")]
        public async Task<IActionResult> MsgMongoQueryPrepared(CancellationToken cancellationToken)
        {
            var bb = _factory.CreateBranchBarrier(Request.Query);
            _logger.LogInformation("bb {0}", bb);

            MongoDB.Driver.IMongoClient cli = new MongoDB.Driver.MongoClient(_settings.MongoBarrierConn);
            var res = await bb.MongoQueryPrepared(cli);
            return Ok(new { dtm_result = res });
        }
    }
}
