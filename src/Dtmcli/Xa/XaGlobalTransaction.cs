using DtmCommon;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dtmcli
{
    public sealed class XaGlobalTransaction
    {
        private readonly IDtmClient _dtmClient;
        private readonly ILogger _logger;

        public XaGlobalTransaction(IDtmClient dtmClient, ILoggerFactory factory)
        {
            this._dtmClient = dtmClient;
            this._logger = factory.CreateLogger<XaGlobalTransaction>();
        }

        public async Task<string> ExcecuteAsync(Func<Xa, Task> xa_cb, CancellationToken cancellationToken = default)
        {
            var gid = await _dtmClient.GenGid(cancellationToken);
            await this.ExcecuteAsync(gid, xa_cb, cancellationToken);
            return gid;
        }

        public async Task ExcecuteAsync(string gid, Func<Xa, Task> xa_cb, CancellationToken cancellationToken = default)
        {
            await ExcecuteAsync(gid, null, xa_cb, cancellationToken);
        }

        public async Task ExcecuteAsync(string gid, Action<Xa> custom, Func<Xa, Task> xa_cb, CancellationToken cancellationToken = default)
        {
            Xa xa = new(this._dtmClient, gid);
            if (null != custom)
                custom(xa);

            try
            {
                await _dtmClient.TransCallDtm(null, xa, Constant.Request.OPERATION_PREPARE, cancellationToken);
                await xa_cb(xa);
                await _dtmClient.TransCallDtm(null, xa, Constant.Request.OPERATION_SUBMIT, cancellationToken);
            }
            catch (Exception ex)
            {
                xa.RollbackReason = ex.Message.Substring(0, ex.Message.Length > 1023 ? 1023 : ex.Message.Length);
                _logger.LogError(ex, "prepare or submitting global transaction error");
                await _dtmClient.TransCallDtm(null, xa, Constant.Request.OPERATION_ABORT, cancellationToken);
            }
        }
    }
}
