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
            var tcc = new Tcc(this.dtmClient, DtmImp.TransBase.NewTransBase(gid, Constant.Request.TYPE_TCC, ""));
          
            try
            {
                var prepare = await dtmClient.TransCallDtm(tcc.GetTransBase(), tcc.GetTransBase(), Constant.Request.OPERATION_PREPARE, cancellationToken);             
                logger.LogDebug("prepare result gid={gid}, res={res}", gid, prepare);

                await tcc_cb(tcc);

                var submit = await dtmClient.TransCallDtm(tcc.GetTransBase(), tcc.GetTransBase(), Constant.Request.OPERATION_SUBMIT, cancellationToken);
                logger.LogDebug("submit result gid={gid}, res={res}", gid, submit);
            }
            catch(Exception ex)
            {
                logger.LogError(ex,"submitting or abort global transaction error");
                var abort = await dtmClient.TransCallDtm(tcc.GetTransBase(), tcc.GetTransBase(), Constant.Request.OPERATION_ABORT, cancellationToken);
                logger.LogDebug("abort result gid={gid}, res={res}", gid, abort);
                return string.Empty;
            }
            return gid;
        }

        public async Task<string> Excecute(string gid, Func<Tcc, Task> tcc_cb, CancellationToken cancellationToken = default)
        {
            return await Excecute(gid, x => { }, tcc_cb, cancellationToken);
        }

        public async Task<string> Excecute(string gid, Action<Tcc> custom, Func<Tcc, Task> tcc_cb, CancellationToken cancellationToken = default)
        {
            var tcc = new Tcc(this.dtmClient, DtmImp.TransBase.NewTransBase(gid, Constant.Request.TYPE_TCC, ""));
            custom(tcc);

            try
            {
                var prepare = await dtmClient.TransCallDtm(tcc.GetTransBase(), tcc.GetTransBase(), Constant.Request.OPERATION_PREPARE, cancellationToken);
                logger.LogDebug("prepare result gid={gid}, res={res}", gid, prepare);

                await tcc_cb(tcc);

                var submit = await dtmClient.TransCallDtm(tcc.GetTransBase(), tcc.GetTransBase(), Constant.Request.OPERATION_SUBMIT, cancellationToken);
                logger.LogDebug("submit result gid={gid}, res={res}", gid, submit);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "submitting or abort global transaction error");
                var abort = await dtmClient.TransCallDtm(tcc.GetTransBase(), tcc.GetTransBase(), Constant.Request.OPERATION_ABORT, cancellationToken);
                logger.LogDebug("abort result gid={gid}, res={res}", gid, abort);
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
