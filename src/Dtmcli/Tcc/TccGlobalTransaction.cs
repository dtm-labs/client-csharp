using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Dtmcli
{
    public class TccGlobalTransaction
    {
        private IDtmClient dtmClient;
        private ILogger logger;

        public TccGlobalTransaction(IDtmClient dtmClient, ILoggerFactory factory)
        {
            this.dtmClient = dtmClient;
            logger = factory.CreateLogger<TccGlobalTransaction>();
        }

        public async Task<string> Excecute(Action<Tcc> tcc_cb, CancellationToken cancellationToken =default)
        {
            var tcc = new Tcc(this.dtmClient, await this.GenGid());

            var tbody = new TccBody
            { 
                Gid = tcc.Gid,
                Trans_Type ="tcc"
            };
 
            try
            {
                await this.dtmClient.TccPrepare(tbody, cancellationToken);
                tcc_cb(tcc);
                await this.dtmClient.TccSubmit(tbody, cancellationToken);
            }
            catch(Exception ex)
            {
                logger.LogError(ex,"submitting or abort global transaction error");
                await this.dtmClient.TccAbort(tbody, cancellationToken);
                return string.Empty;
            }
            return tcc.Gid;
        }

        public async Task<string> GenGid(CancellationToken cancellationToken =default)
        {
            return await dtmClient.GenGid(cancellationToken);
        }       
    }
}
