using Dtmcli;
using DtmMongoBarrier;
using DtmSample.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
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

        private MySqlConnection GetMysqlConn() => new(_settings.SqlBarrierConn);

        private SqlConnection GetMssqlConn() => new(_settings.SqlBarrierConn);

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

            using (MySqlConnection conn = GetMysqlConn())
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
        /// MSG DoAndSubmitDB (mssql)
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost("msg-db-mssql")]
        public async Task<IActionResult> MsgDbMsSql(CancellationToken cancellationToken)
        {
            var gid = await _dtmClient.GenGid(cancellationToken);

            var msg = _transFactory.NewMsg(gid)
                .Add(_settings.BusiUrl + "/TransOut", new TransRequest("1", -30))
                .Add(_settings.BusiUrl + "/TransIn", new TransRequest("2", 30));

            using (SqlConnection conn = GetMssqlConn())
            {
                await msg.DoAndSubmitDB(_settings.BusiUrl + "/msg-mssqlqueryprepared", conn, async tx =>
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
            using (MySqlConnection conn = GetMysqlConn())
            {
                var res = await bb.QueryPrepared(conn);

                return Ok(new { dtm_result = res });
            }
        }


        /// <summary>
        /// MSG QueryPrepared(mssql)
        /// 
        /// tips: Starting with server v1.10, dtm server changed to use the http status code, but was compatible with the body returned by older versions
        /// The http status code 200 with unrecognized body It will be as normal!
        /// eg: v1.18.0 [dtm/client/dtmcli/utils.go · dtm-labs/dtm](https://github.com/dtm-labs/dtm/blob/v1.18.0/client/dtmcli/utils.go)
        /// func HTTPResp2DtmError(resp *resty.Response) error {
        /// code := resp.StatusCode()
        ///     str := resp.String()
        /// if code == http.StatusTooEarly || strings.Contains(str, ResultOngoing) {
        ///     return ErrorMessage2Error(str, ErrOngoing)
        /// } else if code == http.StatusConflict || strings.Contains(str, ResultFailure) {
        ///     return ErrorMessage2Error(str, ErrFailure)
        /// } else if code != http.StatusOK {
        ///     return errors.New(str)
        /// }
        ///     return nil
        /// }
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpGet("msg-mssqlqueryprepared")]
        public async Task<IActionResult> MsgMsSqlQueryPrepared(CancellationToken cancellationToken)
        {
            var bb = _factory.CreateBranchBarrier(Request.Query);
            _logger.LogInformation("bb {0}", bb);
            using (SqlConnection conn = GetMssqlConn())
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

        /// <summary>
        /// MSG with not exist topic will get 【topic not found】
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpGet("msg-topic-notfound")]
        public async Task<IActionResult> MsgWithTopicNotFound(CancellationToken cancellationToken)
        {
            var gid = await _dtmClient.GenGid(cancellationToken);
            var req = new TransRequest("1", -30);
            var msg = _transFactory.NewMsg(gid)
                .AddTopic("not_exist_topic", req);

            await msg.Prepare(_settings.BusiUrl + "/msg-queryprepared", cancellationToken);
            await msg.Submit(cancellationToken);

            return Ok(TransResponse.BuildSucceedResponse());
        }

        /// <summary>
        /// MSG with exist topic
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpGet("msg-topic")]
        public async Task<IActionResult> MsgWithTopic(CancellationToken cancellationToken)
        {
            var gid = await _dtmClient.GenGid(cancellationToken);

            // should subscribe at first
            var topic ="mytopic";

            var req = new TransRequest("1", -30);
            var msg = _transFactory.NewMsg(gid)
                .AddTopic(topic, req);

            await msg.Prepare(_settings.BusiUrl + "/msg-queryprepared", cancellationToken);
            await msg.Submit(cancellationToken);

            return Ok(TransResponse.BuildSucceedResponse());
        }
        
        /// <summary>
        /// MSG with exist topic
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpGet("query")]
        public async Task<IActionResult> Query(string gid, CancellationToken cancellationToken)
        {
            TransGlobal trans = await _dtmClient.Query(gid, cancellationToken);
            return Ok(trans);
        }
    }
}
