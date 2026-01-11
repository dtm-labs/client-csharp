using System.Text.Json;
using BusiIntegrationService.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace BusiIntegrationService.Controllers
{
    [ApiController]
    [Route("http/busi.Busi")]
    public class BusiApiController : ControllerBase
    {
        private readonly ILogger<BusiApiController> _logger;
        private readonly Dtmcli.IBranchBarrierFactory _barrierFactory;
        private readonly Dtmgrpc.IBranchBarrierFactory _grpcBarrierFactory;

        public BusiApiController(ILogger<BusiApiController> logger, Dtmcli.IBranchBarrierFactory barrierFactory, Dtmgrpc.IBranchBarrierFactory grpcBarrierFactory)
        {
            _logger = logger;
            _barrierFactory = barrierFactory;
            _grpcBarrierFactory = grpcBarrierFactory;
        }

        [HttpGet("Test")]
        public async Task<IActionResult> Test()
        {
            return this.Ok(nameof(this.Test));
        }

        [HttpPost("TransIn")]
        public async Task<IActionResult> TransIn([FromBody] BusiRequest request)
        {
            _logger.LogInformation("TransIn req={req}", JsonSerializer.Serialize(request));

            if (DateTime.Now < request.EffectTime)
                return this.StatusCode(425, new { error = "Early" });

            if (string.IsNullOrWhiteSpace(request.TransInResult) || request.TransInResult.Equals("SUCCESS"))
            {
                await Task.CompletedTask;
                return Ok();
            }
            else if (request.TransInResult.Equals("FAILURE"))
            {
                return StatusCode(422, new { error = "FAILURE" }); // 422 Unprocessable Entity for business failure
            }
            else if (request.TransInResult.Equals("ONGOING"))
            {
                return StatusCode(425, new { error = "ONGOING" }); // 425 Too Early for ongoing state
            }

            return StatusCode(500, new { error = $"unknown result {request.TransInResult}" });
        }

        [HttpPost("TransOut")]
        public async Task<IActionResult> TransOut([FromBody] BusiRequest request)
        {
            _logger.LogInformation("TransOut req={req}", JsonSerializer.Serialize(request));

            if (DateTime.Now < request.EffectTime)
                return this.StatusCode(425, new { error = "Early" });

            if (string.IsNullOrWhiteSpace(request.TransOutResult) || request.TransOutResult.Equals("SUCCESS"))
            {
                await Task.CompletedTask;
                return Ok();
            }
            else if (request.TransOutResult.Equals("FAILURE"))
            {
                return StatusCode(422, new { error = "FAILURE" }); // 422 Unprocessable Entity for business failure
            }
            else if (request.TransOutResult.Equals("ONGOING"))
            {
                return StatusCode(425, new { error = "ONGOING" }); // 425 Too Early for ongoing state
            }

            return StatusCode(500, new { error = $"unknown result {request.TransOutResult}" });
        }
    }
}