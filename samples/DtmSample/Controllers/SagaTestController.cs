using Dtmcli;
using DtmSample.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DtmSample.Controllers
{
    /// <summary>
    /// SAGA 示例
    /// </summary>
    [ApiController]
    [Route("/api")]
    public class SagaTestController : ControllerBase
    {
        private readonly ILogger<SagaTestController> _logger;
        private readonly IDtmClient _dtmClient;
        private readonly IDtmTransFactory _transFactory;
        private readonly AppSettings _settings;

        public SagaTestController(ILogger<SagaTestController> logger, IOptions<AppSettings> optionsAccs, IDtmClient dtmClient, IDtmTransFactory transFactory)
        {
            _logger = logger;
            _settings = optionsAccs.Value;
            _dtmClient = dtmClient;
            _transFactory = transFactory;
        }

        /// <summary>
        /// SAGA 常规成功
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost("saga")]
        public async Task<IActionResult> Saga(CancellationToken cancellationToken)
        {
            var gid = await _dtmClient.GenGid(cancellationToken);
            var saga = _transFactory.NewSaga(gid)
                .Add(_settings.BusiUrl + "/TransOut", _settings.BusiUrl + "/TransOutRevert", new TransRequest("1", -30))
                .Add(_settings.BusiUrl + "/TransIn", _settings.BusiUrl + "/TransInRevert", new TransRequest("2", 30))
                ;

            await saga.Submit(cancellationToken);

            _logger.LogInformation("result gid is {0}", gid);

            return Ok(TransResponse.BuildSucceedResponse());
        }

        /// <summary>
        /// SAGA 失败回滚
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost("saga-cancel")]
        public async Task<IActionResult> SagaCancel(CancellationToken cancellationToken)
        {
            var gid = await _dtmClient.GenGid(cancellationToken);
            var saga = _transFactory.NewSaga(gid)
                .Add(_settings.BusiUrl + "/TransOutError", _settings.BusiUrl + "/TransOutRevert", new TransRequest("1", -30))
                .Add(_settings.BusiUrl + "/TransIn", _settings.BusiUrl + "/TransInRevert", new TransRequest("2", 30))
                ;

            await saga.Submit(cancellationToken);

            _logger.LogInformation("result gid is {0}", gid);

            return Ok(TransResponse.BuildSucceedResponse());
        }

        /// <summary>
        /// SAGA 等待结果
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost("saga-waitresult")]
        public async Task<IActionResult> SagaWaitResult(CancellationToken cancellationToken)
        {
            var gid = await _dtmClient.GenGid(cancellationToken);
            var saga = _transFactory.NewSaga(gid)
                .Add(_settings.BusiUrl + "/TransOut", _settings.BusiUrl + "/TransOutRevert", new TransRequest("1", -30))
                .Add(_settings.BusiUrl + "/TransIn", _settings.BusiUrl + "/TransInRevert", new TransRequest("2", 30))
                .EnableWaitResult()
                ;

            await saga.Submit(cancellationToken);

            _logger.LogInformation("result gid is {0}", gid);

            return Ok(TransResponse.BuildSucceedResponse());
        }

        /// <summary>
        /// SAGA 并发
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost("saga-multi")]
        public async Task<IActionResult> SagaMulti(CancellationToken cancellationToken)
        {
            var gid = await _dtmClient.GenGid(cancellationToken);
            var saga = _transFactory.NewSaga(gid)
                .Add(_settings.BusiUrl + "/TransOut", _settings.BusiUrl + "/TransOutRevert", new TransRequest("1", -30))
                .Add(_settings.BusiUrl + "/TransOut", _settings.BusiUrl + "/TransOutRevert", new TransRequest("1", -30))
                .Add(_settings.BusiUrl + "/TransIn", _settings.BusiUrl + "/TransInRevert", new TransRequest("2", 30))
                .Add(_settings.BusiUrl + "/TransIn", _settings.BusiUrl + "/TransInRevert", new TransRequest("2", 30))
                .EnableConcurrent()
                .AddBranchOrder(2, new List<int> { 0, 1 })
                .AddBranchOrder(3, new List<int> { 0, 1 })
                ;

            await saga.Submit(cancellationToken);

            _logger.LogInformation("result gid is {0}", gid);

            return Ok(TransResponse.BuildSucceedResponse());
        }

        /// <summary>
        /// SAGA 异常触发子事务屏障(mysql)
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost("saga-mysqlbarrier")]
        public async Task<IActionResult> SagaMySQLBarrier(CancellationToken cancellationToken)
        {
            var gid = await _dtmClient.GenGid(cancellationToken);
            var saga = _transFactory.NewSaga(gid)
                .Add(_settings.BusiUrl + "/barrierTransOutSaga", _settings.BusiUrl + "/barrierTransOutSagaRevert", new TransRequest("1", -30))
                .Add(_settings.BusiUrl + "/barrierTransInSaga", _settings.BusiUrl + "/barrierTransInSagaRevert", new TransRequest("2", 30))
                ;

            await saga.Submit(cancellationToken);

            _logger.LogInformation("result gid is {0}", gid);

            return Ok(TransResponse.BuildSucceedResponse());
        }

        /// <summary>
        /// SAGA 异常触发子事务屏障(mongodb)
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost("saga-mongobarrier")]
        public async Task<IActionResult> SagaMongoBarrier(CancellationToken cancellationToken)
        {
            var gid = await _dtmClient.GenGid(cancellationToken);
            var saga = _transFactory.NewSaga(gid)
                .Add(_settings.BusiUrl + "/mg/barrierTransOutSaga", _settings.BusiUrl + "/mg/barrierTransOutSagaRevert", new TransRequest("1", -30))
                .Add(_settings.BusiUrl + "/mg/barrierTransInSaga", _settings.BusiUrl + "/mg/barrierTransInSagaRevert", new TransRequest("2", 30))
                ;

            await saga.Submit(cancellationToken);

            _logger.LogInformation("result gid is {0}", gid);

            return Ok(TransResponse.BuildSucceedResponse());
        }
    }
}
