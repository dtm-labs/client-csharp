using Dapper;
using Dtmcli;
using DtmSample.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System.Threading;
using System.Threading.Tasks;

namespace DtmSample.Controllers
{
    [ApiController]
    [Route("/api")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class TransController : ControllerBase
    {
        private readonly ILogger<TransController> _logger;
        private readonly XaLocalTransaction _xaTrans;

        public TransController(ILogger<TransController> logger, XaLocalTransaction xaTrans)
        {
            _logger = logger;
            _xaTrans = xaTrans;
        }

        #region TCC
        [HttpPost("TransOutTry")]
        public IActionResult TransOutTry([FromBody] TransRequest body)
        {
            _logger.LogInformation("TransOutTry, QueryString={0}", Request.QueryString);
            _logger.LogInformation("用户: {0},转出 {1} 元---准备", body.UserId, body.Amount);
            return Ok(TransResponse.BuildSucceedResponse());
        }

        [HttpPost("TransOutTryError")]
        public IActionResult TransOutTryError([FromBody] TransRequest body)
        {
            _logger.LogInformation("TransOutTry, QueryString={0}", Request.QueryString);
            _logger.LogInformation("用户: {0},转出 {1} 元---准备", body.UserId, body.Amount);

            // status code >= 400 || ( status code == 200 && content contains FAILURE)
            //return Ok(TransResponse.BuildFailureResponse());
            return BadRequest();
        }

        [HttpPost("TransOutConfirm")]
        public IActionResult TransOutConfirm([FromBody] TransRequest body)
        {
            _logger.LogInformation("TransOutConfirm, QueryString={0}", Request.QueryString);
            _logger.LogInformation("用户: {0},转出 {1} 元---提交", body.UserId, body.Amount);
            return Ok(TransResponse.BuildSucceedResponse());
        }

        [HttpPost("TransOutCancel")]
        public IActionResult TransOutCancel([FromBody] TransRequest body)
        {
            _logger.LogInformation("TransOutCancel, QueryString={0}", Request.QueryString);
            _logger.LogInformation("用户: {0},转出 {1} 元---回滚", body.UserId, body.Amount);
            return Ok(TransResponse.BuildSucceedResponse());
        }

        [HttpPost("TransInTry")]
        public IActionResult TransInTry([FromBody] TransRequest body)
        {
            _logger.LogInformation("TransInTry, QueryString={0}", Request.QueryString);
            _logger.LogInformation("用户: {0},转入 {1} 元---准备", body.UserId, body.Amount);
            return Ok(TransResponse.BuildSucceedResponse());
        }

        [HttpPost("TransInConfirm")]
        public IActionResult TransInConfirm([FromBody] TransRequest body)
        {
            _logger.LogInformation("TransInConfirm, QueryString={0}", Request.QueryString);
            _logger.LogInformation("用户: {0},转入 {1} 元---提交", body.UserId, body.Amount);
            return Ok(TransResponse.BuildSucceedResponse());
        }

        [HttpPost("TransInCancel")]
        public IActionResult TransInCancel([FromBody] TransRequest body)
        {
            _logger.LogInformation("TransInCancel, QueryString={0}", Request.QueryString);
            _logger.LogInformation("用户: {0},转入 {1} 元---回滚", body.UserId, body.Amount);
            return Ok(TransResponse.BuildSucceedResponse());
        }
        #endregion

        #region SAGA
        [HttpPost("TransOut")]
        public IActionResult TransOut([FromBody] TransRequest body)
        {
            _logger.LogInformation("TransOut, QueryString={0}", Request.QueryString);
            _logger.LogInformation("用户: {0},转出 {1} 元---正向操作", body.UserId, body.Amount);
            return Ok(TransResponse.BuildSucceedResponse());
        }

        [HttpPost("TransOutError")]
        public IActionResult TransOutError([FromBody] TransRequest body)
        {
            _logger.LogInformation("TransOutError, QueryString={0}", Request.QueryString);
            _logger.LogInformation("用户: {0},转出 {1} 元---正向操作", body.UserId, body.Amount);

            // status code = 409 || content contains FAILURE
            //return Ok(TransResponse.BuildFailureResponse());
            return new StatusCodeResult(409);
        }

        [HttpPost("TransOutRevert")]
        public IActionResult TransOutRevert([FromBody] TransRequest body)
        {
            _logger.LogInformation("TransOutConfirm, QueryString={0}", Request.QueryString);
            _logger.LogInformation("用户: {0},转出 {1} 元---回滚", body.UserId, body.Amount);
            return Ok(TransResponse.BuildSucceedResponse());
        }

        [HttpPost("TransIn")]
        public IActionResult TransIn([FromBody] TransRequest body)
        {
            _logger.LogInformation("TransInTry, QueryString={0}", Request.QueryString);
            _logger.LogInformation("用户: {0},转入 {1} 元---正向操作", body.UserId, body.Amount);
            return Ok(TransResponse.BuildSucceedResponse());
        }

        [HttpPost("TransInRevert")]
        public IActionResult TransInRevert([FromBody] TransRequest body)
        {
            _logger.LogInformation("TransInConfirm, QueryString={0}", Request.QueryString);
            _logger.LogInformation("用户: {0},转入 {1} 元---回滚", body.UserId, body.Amount);
            return Ok(TransResponse.BuildSucceedResponse());
        }
        #endregion

        #region Xa
        [HttpPost("XaTransOut")]
        public async Task<IActionResult> XaTransOut(CancellationToken token)
        {
            //todo: Connection pooling needs to be turned off for mysql versions below 8.0
            using var conn = new MySqlConnection("Server=en.dtm.pub; Port=3306; User ID=dtm; Password=passwd123dtm; Database=dtm_busi;Pooling=False");
            await this._xaTrans.ExcecuteAsync(this.Request.Query, conn, "mysql", async (dbConn, xa) =>
            {
                var body = await this.Request.ReadFromJsonAsync<TransRequest>();
                await dbConn.ExecuteAsync($"UPDATE dtm_busi.user_account SET balance = balance + {body.Amount} where user_id = '{body.UserId}'");
                _logger.LogInformation("XaTransOut, QueryString={0}", Request.QueryString);
                _logger.LogInformation("用户: {0},转出 {1} 元", body.UserId, body.Amount);
            }, token);

            return Ok(TransResponse.BuildSucceedResponse());
        }

        [HttpPost("XaTransIn")]
        public async Task<IActionResult> XaTransIn(CancellationToken token)
        {
            //todo: Connection pooling needs to be turned off for mysql versions below 8.0
            using var conn = new MySqlConnection("Server=en.dtm.pub; Port=3306; User ID=dtm; Password=passwd123dtm; Database=dtm_busi;Pooling=False");
            await this._xaTrans.ExcecuteAsync(this.Request.Query, conn, "mysql", async (dbConn, xa) =>
            {
                var body = await this.Request.ReadFromJsonAsync<TransRequest>();
                await dbConn.ExecuteAsync($"UPDATE dtm_busi.user_account SET balance = balance + {body.Amount} where user_id = '{body.UserId}'");
                _logger.LogInformation("XaTransIn, QueryString={0}", Request.QueryString);
                _logger.LogInformation("用户: {0},转入 {1} 元", body.UserId, body.Amount);
            }, token);

            return Ok(TransResponse.BuildSucceedResponse());
        }
        #endregion
    }
}

