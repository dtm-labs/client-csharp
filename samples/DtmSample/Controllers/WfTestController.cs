using Dtmcli.DtmImp;
using DtmCommon;
using DtmSample.Dtos;
using Dtmworkflow;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Exception = System.Exception;

namespace DtmSample.Controllers
{
    [ApiController]
    [Route("/api")]
    public class WfTestController : ControllerBase
    {

        private readonly ILogger<WfTestController> _logger;
        private readonly WorkflowGlobalTransaction _globalTransaction;
        private readonly AppSettings _settings;

        public WfTestController(ILogger<WfTestController> logger, IOptions<AppSettings> optionsAccs, WorkflowGlobalTransaction transaction)
        {
            _logger = logger;
            _settings = optionsAccs.Value;
            _globalTransaction = transaction;
        }

        [HttpPost("wf-simple")]
        public async Task<IActionResult> Simple(CancellationToken cancellationToken)
        {
            try
            {
                var wfName = $"wf-simple-{Guid.NewGuid():N}";

                _globalTransaction.Register(wfName, async (wf, data) => 
                {
                    var content = new ByteArrayContent(data);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                    var outClient = wf.NewBranch().NewRequest();
                    await outClient.PostAsync(_settings.BusiUrl + "/TransOut", content);

                    var inClient = wf.NewBranch().NewRequest();
                    await inClient.PostAsync(_settings.BusiUrl + "/TransIn", content);

                    return null;
                });

                var req = JsonSerializer.Serialize(new TransRequest("1", -30));

                await _globalTransaction.Execute(wfName, Guid.NewGuid().ToString("N"), Encoding.UTF8.GetBytes(req), true);

                return Ok(TransResponse.BuildSucceedResponse());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Workflow Error");
                return Ok(TransResponse.BuildFailureResponse());
            }
        }

        [HttpPost("wf-saga")]
        public async Task<IActionResult> Saga(CancellationToken cancellationToken)
        {
            try
            {
                var wfName = $"wf-saga-{Guid.NewGuid():N}";

                _globalTransaction.Register(wfName, async (wf, data) =>
                {
                    var content = new ByteArrayContent(data);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                    WfPhase2Func outRbFunc = async bb => 
                    {
                        var rbClient = wf.NewRequest();
                        await rbClient.PostAsync(_settings.BusiUrl + "/TransOutRevert", content);
                    };
                    var outClient = wf.NewBranch().OnRollback(outRbFunc).NewRequest();
                    await outClient.PostAsync(_settings.BusiUrl + "/TransOut", content);

                    WfPhase2Func inRbFunc = async bb =>
                    {
                        var rbClient = wf.NewRequest();
                        await rbClient.PostAsync(_settings.BusiUrl + "/TransInRevert", content);
                    };
                    var inClient = wf.NewBranch().OnRollback(inRbFunc).NewRequest();
                    await inClient.PostAsync(_settings.BusiUrl + "/TransIn", content);

                    return null;
                });

                var req = JsonSerializer.Serialize(new TransRequest("1", -30));

                await _globalTransaction.Execute(wfName, Guid.NewGuid().ToString("N"), Encoding.UTF8.GetBytes(req), true);

                return Ok(TransResponse.BuildSucceedResponse());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Workflow Saga Error");
                return Ok(TransResponse.BuildFailureResponse());
            }
        }

        [HttpPost("wf-saga-rollback")]
        public async Task<IActionResult> SagaRollBack(CancellationToken cancellationToken)
        {
            try
            {
                var wfName = $"wf-saga-rollback-{Guid.NewGuid():N}";

                _globalTransaction.Register(wfName, async (wf, data) =>
                {
                    var content = new ByteArrayContent(data);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                    WfPhase2Func outRbFunc = async bb =>
                    {
                        var rbClient = wf.NewRequest();
                        await rbClient.PostAsync(_settings.BusiUrl + "/TransOutRevert", content);
                    };
                    var outClient = wf.NewBranch().OnRollback(outRbFunc).NewRequest();
                    var resp = await outClient.PostAsync(_settings.BusiUrl + "/TransOutError", content);

                    var ex = await Utils.RespAsErrorCompatible(resp);
                    if (ex != null) throw ex;

                    return null;
                });

                var req = JsonSerializer.Serialize(new TransRequest("1", -30));

                await _globalTransaction.Execute(wfName, Guid.NewGuid().ToString("N"), Encoding.UTF8.GetBytes(req), true);

                return Ok(TransResponse.BuildSucceedResponse());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Workflow Saga Error");
                return Ok(TransResponse.BuildFailureResponse());
            }
        }

        [HttpPost("wf-tcc")]
        public async Task<IActionResult> Tcc(CancellationToken cancellationToken)
        {
            try
            {
                var wfName = $"wf-tcc-{Guid.NewGuid():N}";

                _globalTransaction.Register(wfName, async (wf, data) =>
                {
                    var content = new ByteArrayContent(data);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                    WfPhase2Func outRbFunc = async bb =>
                    {
                        var client = wf.NewRequest();
                        await client.PostAsync(_settings.BusiUrl + "/TransOutCancel", content);
                    };

                    WfPhase2Func outCmFunc = async bb =>
                    {
                        var client = wf.NewRequest();
                        await client.PostAsync(_settings.BusiUrl + "/TransOutConfirm", content);
                    };

                    var outClient = wf.NewBranch().OnRollback(outRbFunc).OnCommit(outCmFunc).NewRequest();
                    await outClient.PostAsync(_settings.BusiUrl + "/TransOutTry", content);

                    WfPhase2Func inRbFunc = async bb =>
                    {
                        var client = wf.NewRequest();
                        await client.PostAsync(_settings.BusiUrl + "/TransInCancel", content);
                    };

                    WfPhase2Func inCmFunc = async bb =>
                    {
                        var client = wf.NewRequest();
                        await client.PostAsync(_settings.BusiUrl + "/TransInConfirm", content);
                    };

                    var inClient = wf.NewBranch().OnRollback(inRbFunc).OnCommit(inCmFunc).NewRequest();
                    await inClient.PostAsync(_settings.BusiUrl + "/TransInTry", content);

                    return null;
                });

                var req = JsonSerializer.Serialize(new TransRequest("1", -30));

                await _globalTransaction.Execute(wfName, Guid.NewGuid().ToString("N"), Encoding.UTF8.GetBytes(req), true);

                return Ok(TransResponse.BuildSucceedResponse());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Workflow Tcc Error");
                return Ok(TransResponse.BuildFailureResponse());
            }
        }

        [HttpPost("wf-tcc-rollback")]
        public async Task<IActionResult> TccRollBack(CancellationToken cancellationToken)
        {
            try
            {
                var wfName = $"wf-tcc-rollback-{Guid.NewGuid():N}";

                _globalTransaction.Register(wfName, async (wf, data) =>
                {
                    var content = new ByteArrayContent(data);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                    WfPhase2Func outRbFunc = async bb =>
                    {
                        var client = wf.NewRequest();
                        await client.PostAsync(_settings.BusiUrl + "/TransOutCancel", content);
                    };

                    WfPhase2Func outCmFunc = async bb =>
                    {
                        var client = wf.NewRequest();
                        await client.PostAsync(_settings.BusiUrl + "/TransOutConfirm", content);
                    };

                    var outClient = wf.NewBranch().OnRollback(outRbFunc).OnCommit(outCmFunc).NewRequest();
                    var resp = await outClient.PostAsync(_settings.BusiUrl + "/TransOutTryError", content);

                    if ((int)resp.StatusCode >= 400)
                    {
                        throw new DtmFailureException();
                    }

                    return null;
                });

                var req = JsonSerializer.Serialize(new TransRequest("1", -30));

                await _globalTransaction.Execute(wfName, Guid.NewGuid().ToString("N"), Encoding.UTF8.GetBytes(req), true);

                return Ok(TransResponse.BuildSucceedResponse());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Workflow Tcc Error");
                return Ok(TransResponse.BuildFailureResponse());
            }
        }


        private static readonly string wfNameForResume = "wfNameForResume";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost("wf-crash")]
        public async Task<IActionResult> Crash(CancellationToken cancellationToken)
        {
            if (!_globalTransaction.Exists(wfNameForResume))
            {
                _globalTransaction.Register(wfNameForResume, async (wf, data) =>
                {
                    var content = new ByteArrayContent(data);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                    var outClient = wf.NewBranch().NewRequest();
                    await outClient.PostAsync(_settings.BusiUrl + "/TransOut", content);

                    // the first branch succeed, then crashed, the dtm server will call back the flowing wf-call-back
                    // manual stop application
                    Environment.Exit(0);

                    var inClient = wf.NewBranch().NewRequest();
                    await inClient.PostAsync(_settings.BusiUrl + "/TransIn", content);

                    return null;
                });
            }
            
            var req = JsonSerializer.Serialize(new TransRequest("1", -30));
            await _globalTransaction.Execute(wfNameForResume, Guid.NewGuid().ToString("N"), Encoding.UTF8.GetBytes(req), true);

            return Ok(TransResponse.BuildSucceedResponse());
        }

        [HttpPost("wf-resume")]
        public async Task<IActionResult> WfResume(CancellationToken cancellationToken)
        {
            try
            {
                if (!_globalTransaction.Exists(wfNameForResume))
                {
                    // register again after manual crash by Environment.Exit(0);
                    _globalTransaction.Register(wfNameForResume, async (wf, data) =>
                    {
                        var content = new ByteArrayContent(data);
                        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                        var outClient = wf.NewBranch().NewRequest();
                        await outClient.PostAsync(_settings.BusiUrl + "/TransOut", content);

                        var inClient = wf.NewBranch().NewRequest();
                        await inClient.PostAsync(_settings.BusiUrl + "/TransIn", content);

                        return null;
                    });
                }

                // prepared call ExecuteByQS
                using var bodyMemoryStream = new MemoryStream();
                await Request.Body.CopyToAsync(bodyMemoryStream, cancellationToken);
                byte[] bytes = bodyMemoryStream.ToArray();
                string body = Encoding.UTF8.GetString(bytes);
                _logger.LogDebug($"body: {body}");

                await _globalTransaction.ExecuteByQS(Request.Query, bodyMemoryStream.ToArray());

                return Ok(TransResponse.BuildSucceedResponse());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Workflow Error");
                return Ok(TransResponse.BuildFailureResponse());
            }
        }
    }
}
