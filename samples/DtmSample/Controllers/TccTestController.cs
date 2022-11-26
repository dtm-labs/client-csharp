using Dtmcli;
using DtmSample.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DtmSample.Controllers
{
    /// <summary>
    /// TCC 示例
    /// </summary>
    [ApiController]
    [Route("/api")]
    public class TccTestController : ControllerBase
    {

        private readonly ILogger<TccTestController> _logger;
        private readonly TccGlobalTransaction _globalTransaction;
        private readonly AppSettings _settings;

        public TccTestController(ILogger<TccTestController> logger, IOptions<AppSettings> optionsAccs, TccGlobalTransaction transaction)
        {
            _logger = logger;
            _settings = optionsAccs.Value;
            _globalTransaction = transaction;
        }

        /// <summary>
        /// TCC 成功提交
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost("tcc")]
        public async Task<IActionResult> Tcc(CancellationToken cancellationToken)
        {
            try
            {
                await _globalTransaction.Excecute(async (tcc) =>
                {
                    // 用户1 转出30元
                    var res1 = await tcc.CallBranch(new TransRequest("1", -30), _settings.BusiUrl + "/TransOutTry", _settings.BusiUrl + "/TransOutConfirm", _settings.BusiUrl + "/TransOutCancel", cancellationToken);

                    // 用户2 转入30元
                    var res2 = await tcc.CallBranch(new TransRequest("2", 30), _settings.BusiUrl + "/TransInTry", _settings.BusiUrl + "/TransInConfirm", _settings.BusiUrl + "/TransInCancel", cancellationToken);
                    _logger.LogInformation($"tcc returns: {res1}-{res2}");
                }, cancellationToken);

                return Ok(TransResponse.BuildSucceedResponse());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tcc Error");
                return Ok(TransResponse.BuildFailureResponse());
            }
        }

        /// <summary>
        /// TCC 失败回滚
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost("tcc-cancel")]
        public async Task<IActionResult> TccCancel(CancellationToken cancellationToken)
        {
            try
            {
                await _globalTransaction.Excecute(async (tcc) =>
                {
                    // 用户1 转出30元
                    var res1 = await tcc.CallBranch(new TransRequest("1", -30), _settings.BusiUrl + "/TransOutTryError", _settings.BusiUrl + "/TransOutConfirm", _settings.BusiUrl + "/TransOutCancel", cancellationToken);

                    // 用户2 转入30元
                    var res2 = await tcc.CallBranch(new TransRequest("2", 30), _settings.BusiUrl + "/TransInTry", _settings.BusiUrl + "/TransInConfirm", _settings.BusiUrl + "/TransInCancel", cancellationToken);
                    _logger.LogInformation($"tcc returns: {res1}-{res2}");

                    // 老版本的需要自己抛异常，新版本会抛出 DtmcliException
                    // https://github.com/dtm-labs/dtmcli-csharp/issues/10
                    // ==========================================================
                    // 判断转入转出是否成功，不成功，要抛出异常，走 tcc 回滚
                    //if (!res1.Contains("SUCCESS")) throw new Exception("转出失败了");
                    //if (!res2.Contains("SUCCESS")) throw new Exception("转入失败了");

                }, cancellationToken);

                return Ok(TransResponse.BuildSucceedResponse());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TccCancel Error");
                return Ok(TransResponse.BuildFailureResponse());
            }
        }

        /// <summary>
        /// TCC 成功提交 自定义gid
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost("tcc-cusgid")]
        public async Task<IActionResult> TccCusGid(CancellationToken cancellationToken)
        {
            var gid = Guid.NewGuid().ToString("N");
            try
            {
                await _globalTransaction.Excecute(gid, async (tcc) =>
                {
                    // 用户1 转出30元
                    var res1 = await tcc.CallBranch(new TransRequest("1", -30), _settings.BusiUrl + "/TransOutTry", _settings.BusiUrl + "/TransOutConfirm", _settings.BusiUrl + "/TransOutCancel", cancellationToken);

                    // 用户2 转入30元
                    var res2 = await tcc.CallBranch(new TransRequest("2", 30), _settings.BusiUrl + "/TransInTry", _settings.BusiUrl + "/TransInConfirm", _settings.BusiUrl + "/TransInCancel", cancellationToken);
                    _logger.LogInformation($"tcc returns: {res1}-{res2}");
                }, cancellationToken);

                return Ok(TransResponse.BuildSucceedResponse());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TccCusGid Error");
                return Ok(TransResponse.BuildFailureResponse());
            }
        }

        /// <summary>
        /// TCC 异常触发子事务屏障(mysql)
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost("tcc-mysqlbarrier")]
        public async Task<IActionResult> TccBarrier(CancellationToken cancellationToken)
        {
            try
            {
                await _globalTransaction.Excecute(async (tcc) =>
                {
                    // 用户1 转出30元
                    var res1 = await tcc.CallBranch(new TransRequest("1", -30), _settings.BusiUrl + "/barrierTransOutTry", _settings.BusiUrl + "/barrierTransOutConfirm", _settings.BusiUrl + "/barrierTransOutCancel", cancellationToken);

                    // 用户2 转入30元
                    var res2 = await tcc.CallBranch(new TransRequest("2", 30), _settings.BusiUrl + "/barrierTransInTryError", _settings.BusiUrl + "/barrierTransInConfirm", _settings.BusiUrl + "/barrierTransInCancel", cancellationToken);
                    _logger.LogInformation($"tcc returns: {res1}-{res2}");

                    // 老版本的需要自己抛异常，新版本会抛出 DtmcliException
                    // https://github.com/dtm-labs/dtmcli-csharp/issues/10
                    // ==========================================================
                    //// 判断转入转出是否成功，不成功，要抛出异常，走 tcc 回滚
                    //if (!res1.Contains("SUCCESS")) throw new Exception("转出失败了");
                    //if (!res2.Contains("SUCCESS")) throw new Exception("转入失败了");

                }, cancellationToken);

                return Ok(TransResponse.BuildSucceedResponse());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TccBarrier Error");
                return Ok(TransResponse.BuildFailureResponse());
            }
        }

        /// <summary>
        /// TCC 异常触发子事务屏障(mongo)
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost("tcc-mongobarrier")]
        public async Task<IActionResult> TccMongoBarrier(CancellationToken cancellationToken)
        {
            try
            {
                await _globalTransaction.Excecute(async (tcc) =>
                {
                    // 用户1 转出30元
                    var res1 = await tcc.CallBranch(new TransRequest("1", -30), _settings.BusiUrl + "/mg/barrierTransOutTry", _settings.BusiUrl + "/mg/barrierTransOutConfirm", _settings.BusiUrl + "/mg/barrierTransOutCancel", cancellationToken);

                    // 用户2 转入30元
                    var res2 = await tcc.CallBranch(new TransRequest("2", 30), _settings.BusiUrl + "/mg/barrierTransInTryError", _settings.BusiUrl + "/mg/barrierTransInConfirm", _settings.BusiUrl + "/mg/barrierTransInCancel", cancellationToken);
                    _logger.LogInformation($"tcc returns: {res1}-{res2}");

                    // 老版本的需要自己抛异常，新版本会抛出 DtmcliException
                    // https://github.com/dtm-labs/dtmcli-csharp/issues/10
                    // ==========================================================
                    //// 判断转入转出是否成功，不成功，要抛出异常，走 tcc 回滚
                    //if (!res1.Contains("SUCCESS")) throw new Exception("转出失败了");
                    //if (!res2.Contains("SUCCESS")) throw new Exception("转入失败了");

                }, cancellationToken);

                return Ok(TransResponse.BuildSucceedResponse());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TccBarrier Error");
                return Ok(TransResponse.BuildFailureResponse());
            }
        }
    }
}
