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
    /// XA 示例
    /// </summary>
    [ApiController]
    [Route("/api")]
    public class XaTestController : ControllerBase
    {

        private readonly ILogger<TccTestController> _logger;
        private readonly XaGlobalTransaction _globalTransaction;
        private readonly AppSettings _settings;

        public XaTestController(ILogger<TccTestController> logger, IOptions<AppSettings> optionsAccs, XaGlobalTransaction transaction)
        {
            _logger = logger;
            _settings = optionsAccs.Value;
            _globalTransaction = transaction;
        }

        /// <summary>
        /// Xa 成功提交
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost("commit")]
        public async Task<IActionResult> Commit(CancellationToken cancellationToken)
        {
            //todo: Currently only supported by mysql, please modify the appsettings.json
            try
            {
                await _globalTransaction.ExcecuteAsync(async (Xa xa) =>
                {
                    //// 用户1 转出30元
                    var res1 = await xa.CallBranch(new TransRequest("1", -30), _settings.BusiUrl + "/XaTransOut", cancellationToken);

                    //// 用户2 转入30元
                    var res2 = await xa.CallBranch(new TransRequest("2", 30), _settings.BusiUrl + "/XaTransIn", cancellationToken);
                }, cancellationToken);

                return Ok(TransResponse.BuildSucceedResponse());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Xa Error");
                return Ok(TransResponse.BuildFailureResponse());
            }
        }


        /// <summary>
        /// Xa 失败回滚
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost("rollbcak")]
        public async Task<IActionResult> Rollbcak(CancellationToken cancellationToken)
        {
            //todo: Currently only supported by mysql, please modify the appsettings.json
            try
            {
                await _globalTransaction.ExcecuteAsync(async (Xa xa) =>
            {
                //// 用户1 转出30元
                var res1 = await xa.CallBranch(new TransRequest("1", -30), _settings.BusiUrl + "/XaTransOut", cancellationToken);

                //// 用户2 转入30元
                var res2 = await xa.CallBranch(new TransRequest("2", 30), _settings.BusiUrl + "/XaTransIn", cancellationToken);

                throw new Exception("rollbcak");
            }, cancellationToken);

            return Ok(TransResponse.BuildSucceedResponse());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Xa Error");
                return Ok(TransResponse.BuildFailureResponse());
            }
        }
    }
}
