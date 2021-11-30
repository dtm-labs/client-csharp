using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dtmcli
{
    public class TccGlobalTransaction
    {
        private IDtmClient dtmClient;
        private ILogger logger;
        private TransactionConfig transactionConfig;

        public TccGlobalTransaction(IDtmClient dtmClient, ILoggerFactory factory)
        {
            this.dtmClient = dtmClient;
            logger = factory.CreateLogger<TccGlobalTransaction>();
        }

        public async Task<DtmResult> Excecute(Func<Tcc,Task> tcc_cb,CancellationToken cancellationToken =default)
        {
            var tcc = new Tcc(this.dtmClient, await this.GenGid());

            var tbody = new TccBody
            {
                Gid = tcc.Gid,
                Trans_Type = "tcc",
                WaitResult = transactionConfig?.WaitResult,
                RetryInterval = transactionConfig?.RetryInterval,
                TimeoutToFail = transactionConfig?.TimeoutToFail
            };



            try
            {
                await dtmClient.TccPrepare(tbody, cancellationToken);
 
                await tcc_cb(tcc);
                
                var dtmResult= await dtmClient.TccSubmit(tbody, cancellationToken);
                dtmResult.Gid = tcc.Gid;
                return dtmResult;
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Sub-transaction try phase or dtm server error");
                await this.dtmClient.TccAbort(tbody, cancellationToken);
                return new DtmResult { Gid = tcc.Gid,Dtm_Result = string.Empty,Message = ex.Message};
            }
        }

        public async Task<string> GenGid(CancellationToken cancellationToken =default)
        {
            return await dtmClient.GenGid(cancellationToken);
        }
        /// <summary>
        /// 配置事务选项
        /// </summary>
        /// <param name="waitResult">是否等待事务结果</param>
        /// <param name="retryInterval">失败后子事务重试间隔</param>
        /// <param name="timeoutToFail">全局事务 超时失败时间</param>
        /// <returns></returns>
        public TccGlobalTransaction Config(bool? waitResult=null,int? retryInterval = null, int? timeoutToFail = null)
        {
            transactionConfig = new TransactionConfig
            {
                WaitResult = waitResult,
                RetryInterval = retryInterval,
                TimeoutToFail = timeoutToFail
            };
            return this;
        }
    }
    public class TransactionConfig
    {
        /// <summary>
        /// 是否等待事务结果
        /// </summary>
        public bool? WaitResult { get; set; }
        /// <summary>
        /// 失败后子事务重试间隔
        /// </summary>
        public int? RetryInterval { get; set; }
        /// <summary>
        /// 全局事务 超时失败时间
        /// </summary>
        public int? TimeoutToFail { get; set; }
    }
}
