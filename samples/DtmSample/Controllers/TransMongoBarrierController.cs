using Dtmcli;
using DtmMongoBarrier;
using DtmSample.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Threading.Tasks;

namespace DtmSample.Controllers
{
    [ApiController]
    [Route("/api/mg")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class TransMongoBarrierController : ControllerBase
    {
        private readonly ILogger<TransMongoBarrierController> _logger;
        private readonly AppSettings _settings;
        private readonly IBranchBarrierFactory _barrierFactory;

        public TransMongoBarrierController(ILogger<TransMongoBarrierController> logger, IOptions<AppSettings> optionsAccs, IBranchBarrierFactory barrierFactory)
        {
            _logger = logger;
            _settings = optionsAccs.Value;
            _barrierFactory = barrierFactory;
        }

        private IMongoClient GetBarrierConn() => new MongoClient(_settings.MongoBarrierConn);

        #region TCC
        [HttpPost("barrierTransOutTry")]
        public async Task<IActionResult> BarrierTransOutTry([FromBody] TransRequest body)
        {
            _logger.LogInformation("BarrierTransOutTry, QueryString={0}, body={1}", Request.QueryString, body.ToString());

            var branchBarrier = _barrierFactory.CreateBranchBarrier(Request.Query);

            await branchBarrier.MongoCall(GetBarrierConn(), async (tx) =>
            {
                _logger?.LogInformation("用户: {0},转出 {1} 元---准备", body.UserId, body.Amount);
                await Task.CompletedTask;
            });

            return Ok(TransResponse.BuildSucceedResponse());
        }

        [HttpPost("barrierTransOutConfirm")]
        public async Task<IActionResult> BarrierTransOutConfirm([FromBody] TransRequest body)
        {
            _logger.LogInformation("BarrierTransOutConfirm, QueryString={0}, body={1}", Request.QueryString, body.ToString());

            var branchBarrier = _barrierFactory.CreateBranchBarrier(Request.Query);

            await branchBarrier.MongoCall(GetBarrierConn(), async (tx) =>
            {
                _logger?.LogInformation("用户: {0},转出 {1} 元---提交", body.UserId, body.Amount);
                await Task.CompletedTask;
            });

            return Ok(TransResponse.BuildSucceedResponse());
        }

        [HttpPost("barrierTransOutCancel")]
        public async Task<IActionResult> BarrierTransOutCancel([FromBody] TransRequest body)
        {
            _logger.LogInformation("BarrierTransOutCancel, QueryString={0}, body={1}", Request.QueryString, body.ToString());

            var branchBarrier = _barrierFactory.CreateBranchBarrier(Request.Query);

            await branchBarrier.MongoCall(GetBarrierConn(), async (tx) =>
            {
                _logger?.LogInformation("用户: {0},转出 {1} 元---回滚", body.UserId, body.Amount);
                await Task.CompletedTask;
            });

            return Ok(TransResponse.BuildSucceedResponse());
        }

        [HttpPost("barrierTransInTry")]
        public async Task<IActionResult> BarrierTransInTry([FromBody] TransRequest body)
        {
            _logger.LogInformation("BarrierTransInTry, QueryString={0}, body={1}", Request.QueryString, body.ToString());

            var branchBarrier = _barrierFactory.CreateBranchBarrier(Request.Query);

            await branchBarrier.MongoCall(GetBarrierConn(), async (tx) =>
            {
                _logger?.LogInformation("用户: {0},转入 {1} 元---准备", body.UserId, body.Amount);
                await Task.CompletedTask;
            });

            return Ok(TransResponse.BuildSucceedResponse());
        }

        [HttpPost("barrierTransInTryError")]
        public async Task<IActionResult> BarrierTransInTryError([FromBody] TransRequest body)
        {
            _logger.LogInformation("barrierTransInTryError, QueryString={0}, body={1}", Request.QueryString, body.ToString());

            await Task.Yield();
            return BadRequest();
        }

        [HttpPost("barrierTransInConfirm")]
        public async Task<IActionResult> BarrierTransInConfirm([FromBody] TransRequest body)
        {
            _logger.LogInformation("BarrierTransInConfirm, QueryString={0}, body={1}", Request.QueryString, body.ToString());

            var branchBarrier = _barrierFactory.CreateBranchBarrier(Request.Query);

            await branchBarrier.MongoCall(GetBarrierConn(), async (tx) =>
            {
                _logger?.LogInformation("用户: {0},转入 {1} 元---提交", body.UserId, body.Amount);
                await Task.CompletedTask;
            });

            return Ok(TransResponse.BuildFailureResponse());
        }

        [HttpPost("barrierTransInCancel")]
        public async Task<IActionResult> BarrierTransInCancel([FromBody] TransRequest body)
        {
            _logger.LogInformation("BarrierTransInCancel, QueryString={0}, body={1}", Request.QueryString, body.ToString());

            var branchBarrier = _barrierFactory.CreateBranchBarrier(Request.Query);

            await branchBarrier.MongoCall(GetBarrierConn(), async (tx) =>
            {
                _logger?.LogInformation("用户: {0},转入 {1} 元---回滚", body.UserId, body.Amount);
                await Task.CompletedTask;
            });

            return Ok(TransResponse.BuildSucceedResponse());
        }
        #endregion

        #region SAGA
        [HttpPost("barrierTransOutSaga")]
        public async Task<IActionResult> BarrierTransOutSaga([FromBody] TransRequest body)
        {
            _logger.LogInformation("barrierTransOutSaga, QueryString={0}, body={1}", Request.QueryString, body.ToString());

            var branchBarrier = _barrierFactory.CreateBranchBarrier(Request.Query);

            await branchBarrier.MongoCall(GetBarrierConn(), async (tx) =>
            {
                _logger?.LogInformation("用户: {0},转出 {1} 元---正向操作", body.UserId, body.Amount);
                await Task.CompletedTask;
            });

            return Ok(TransResponse.BuildSucceedResponse());
        }

        [HttpPost("barrierTransOutSagaRevert")]
        public async Task<IActionResult> BarrierTransOutSagaRevert([FromBody] TransRequest body)
        {
            _logger.LogInformation("barrierTransOutSagaRevert, QueryString={0}", Request.QueryString);

            var branchBarrier = _barrierFactory.CreateBranchBarrier(Request.Query);

            await branchBarrier.MongoCall(GetBarrierConn(), async (tx) =>
            {
                _logger?.LogInformation("用户: {0},转出 {1} 元---回滚", body.UserId, body.Amount);
                await Task.CompletedTask;
            });

            return Ok(TransResponse.BuildSucceedResponse());
        }

        [HttpPost("barrierTransInSaga")]
        public async Task<IActionResult> BarrierTransInSaga([FromBody] TransRequest body)
        {
            _logger.LogInformation("barrierTransInSaga, QueryString={0}", Request.QueryString);

            // 模拟转入失败，触发转入空补偿
            await Task.Yield();
            return Ok(TransResponse.BuildFailureResponse());
        }

        [HttpPost("barrierTransInSagaRevert")]
        public async Task<IActionResult> BarrierTransInSagaRevert([FromBody] TransRequest body)
        {
            _logger.LogInformation("BarrierTransInSagaRevert, QueryString={0}, body={1}", Request.QueryString, body.ToString());

            var branchBarrier = _barrierFactory.CreateBranchBarrier(Request.Query);

            await branchBarrier.MongoCall(GetBarrierConn(), async (tx) =>
            {
                // 用户 1 转出 30 成功
                // 用户 2 转入 30 失败
                // 用户 2 转入 30, 不应该补偿，也就是不会打印下面的日志
                // 用户 1 转出 30, 应该补偿。
                _logger?.LogInformation("用户: {0},转入 {1} 元---回滚", body.UserId, body.Amount);
                await Task.CompletedTask;
            });

            return Ok(TransResponse.BuildSucceedResponse());
        }
        #endregion
    }
}
