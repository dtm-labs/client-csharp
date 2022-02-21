using DtmCommon;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dtmcli
{
    public class TccGlobalTransaction
    {
        private readonly IDtmClient dtmClient;
        private readonly ILogger logger;

        public TccGlobalTransaction(IDtmClient dtmClient, ILoggerFactory factory)
        {
            this.dtmClient = dtmClient;
            this.logger = factory.CreateLogger<TccGlobalTransaction>();
        }

        public async Task<string> Excecute(Func<Tcc,Task> tcc_cb, CancellationToken cancellationToken =default)
        {
            var gid = await this.GenGid(cancellationToken);

            return await Excecute(gid, tcc_cb, cancellationToken);
        }

        public async Task<string> Excecute(string gid, Func<Tcc, Task> tcc_cb, CancellationToken cancellationToken = default)
        {
            return await Excecute(gid, x => { }, tcc_cb, cancellationToken);
        }

        public async Task<string> Excecute(string gid, Action<Tcc> custom, Func<Tcc, Task> tcc_cb, CancellationToken cancellationToken = default)
        {
            var tcc = new Tcc(this.dtmClient, TransBase.NewTransBase(gid, DtmCommon.Constant.TYPE_TCC, "", ""));
            custom(tcc);

            try
            {
                await dtmClient.TransCallDtm(tcc.GetTransBase(), tcc.GetTransBase(), Constant.Request.OPERATION_PREPARE, cancellationToken);

                await tcc_cb(tcc);

                await dtmClient.TransCallDtm(tcc.GetTransBase(), tcc.GetTransBase(), Constant.Request.OPERATION_SUBMIT, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "prepare or submitting global transaction error");
                await dtmClient.TransCallDtm(tcc.GetTransBase(), tcc.GetTransBase(), Constant.Request.OPERATION_ABORT, cancellationToken);
                return string.Empty;
            }
            return gid;
        }

        public async Task<string> GenGid(CancellationToken cancellationToken =default)
        {
            return await dtmClient.GenGid(cancellationToken);
        }       
    }
}
