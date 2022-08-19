using DtmCommon;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dtmgrpc
{
    public class TccGlobalTransaction
    {
        private readonly IDtmgRPCClient _dtmClient;
        private readonly ILogger _logger;
        private readonly IDtmTransFactory _transFactory;

        public TccGlobalTransaction(IDtmgRPCClient dtmClient, ILoggerFactory factory, IDtmTransFactory transFactory)
        {
            this._dtmClient = dtmClient;
            this._logger = factory.CreateLogger<TccGlobalTransaction>();
            this._transFactory = transFactory;
        }

        public async Task<string> Excecute(string gid, Func<TccGrpc, Task> tcc_cb, CancellationToken cancellationToken = default)
        {
            return await Excecute(gid, x => { }, tcc_cb, cancellationToken);
        }

        public async Task<string> Excecute(string gid, Action<TccGrpc> custom, Func<TccGrpc, Task> tcc_cb, CancellationToken cancellationToken = default)
        {
            var tcc = _transFactory.NewTccGrpc(gid);
            custom(tcc);

            try
            {
                await _dtmClient.DtmGrpcCall(tcc.GetTransBase(), Constant.Op.Prepare);

                await tcc_cb(tcc);

                await _dtmClient.DtmGrpcCall(tcc.GetTransBase(), Constant.Op.Submit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "submitting or abort global transaction error");
                await _dtmClient.DtmGrpcCall(tcc.GetTransBase(), Constant.Op.Abort);
                return string.Empty;
            }

            return gid;
        }
    }
}
