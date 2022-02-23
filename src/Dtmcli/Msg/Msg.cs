using DtmCommon;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Dtmcli
{
    public class Msg
    {
        private readonly TransBase _transBase;
        private readonly IDtmClient _dtmClient;
        private readonly IBranchBarrierFactory _branchBarrierFactory;

        public Msg(IDtmClient dtmHttpClient, IBranchBarrierFactory branchBarrierFactory, string gid)
        {
            this._dtmClient = dtmHttpClient;
            this._branchBarrierFactory = branchBarrierFactory;
            this._transBase = TransBase.NewTransBase(gid, DtmCommon.Constant.TYPE_MSG, string.Empty, string.Empty);
        }

        public Msg Add(string action, object postData)
        {
            if (this._transBase.Steps == null) this._transBase.Steps = new List<Dictionary<string, string>>();
            if (this._transBase.Payloads == null) this._transBase.Payloads = new List<string>();

            this._transBase.Steps.Add(new Dictionary<string, string> { { Constant.Request.BRANCH_ACTION, action } });
            this._transBase.Payloads.Add(JsonSerializer.Serialize(postData));
            return this;
        }

        public async Task Prepare(string queryPrepared, CancellationToken cancellationToken = default)
        {
            this._transBase.QueryPrepared = !string.IsNullOrWhiteSpace(queryPrepared)? queryPrepared : this._transBase.QueryPrepared;

            await this._dtmClient.TransCallDtm(this._transBase, this._transBase, Constant.Request.OPERATION_PREPARE, cancellationToken);
        }

        public async Task Submit(CancellationToken cancellationToken = default)
        {
            await this._dtmClient.TransCallDtm(this._transBase, this._transBase, Constant.Request.OPERATION_SUBMIT, cancellationToken);
        }

        public async Task DoAndSubmitDB(string queryPrepared, DbConnection db, Func<DbTransaction, Task> busiCall, CancellationToken cancellationToken = default)
        {
            await this.DoAndSubmit(queryPrepared, async bb => 
            {
                await bb.Call(db, busiCall);
            }, cancellationToken);
        }

        public async Task DoAndSubmit(string queryPrepared, Func<BranchBarrier, Task> busiCall, CancellationToken cancellationToken = default)
        {
            var bb = _branchBarrierFactory.CreateBranchBarrier(this._transBase.TransType, this._transBase.Gid, DtmCommon.Constant.Barrier.MSG_BRANCHID, DtmCommon.Constant.TYPE_MSG);            

            if (bb.IsInValid()) throw new DtmException($"invalid trans info: {bb.ToString()}");

            await this.Prepare(queryPrepared, cancellationToken);

            Exception errb = null;

            try
            {
                await busiCall.Invoke(bb);
            }
            catch (Exception ex)
            {
                errb = ex;
            }

            Exception err = null;
            if (errb != null && !(errb is DtmFailureException))
            {
                // if busicall return an error other than failure, we will query the result
                var resp = await _dtmClient.TransRequestBranch(this._transBase, HttpMethod.Get, null, bb.BranchID, bb.Op, queryPrepared, cancellationToken);
                err = await DtmImp.Utils.RespAsErrorCompatible(resp);
            }

            if ((errb != null && errb is DtmFailureException) || (err != null && err is DtmFailureException))
            {
                await _dtmClient.TransCallDtm(_transBase, _transBase, Constant.Request.OPERATION_ABORT, default);
            }
            else if (err == null)
            {
                await this.Submit(cancellationToken);
            }

            if (errb != null) throw errb;
        }

        /// <summary>
        /// Enable wait result for trans
        /// </summary>
        /// <returns></returns>
        public Msg EnableWaitResult()
        {
            this._transBase.WaitResult = true;
            return this;
        }

        /// <summary>
        /// Set timeout to fail for trans, unit is second
        /// </summary>
        /// <param name="timeoutToFail">timeout to fail</param>
        /// <returns></returns>
        public Msg SetTimeoutToFail(long timeoutToFail)
        {
            this._transBase.TimeoutToFail = timeoutToFail;
            return this;
        }

        /// <summary>
        /// Set retry interval for trans, unit is second
        /// </summary>
        /// <param name="retryInterval"></param>
        /// <returns></returns>
        public Msg SetRetryInterval(long retryInterval)
        {
            this._transBase.RetryInterval = retryInterval;
            return this;
        }

        /// <summary>
        /// Set branch headers for trans
        /// </summary>
        /// <param name="headers"></param>
        /// <returns></returns>
        public Msg SetBranchHeaders(Dictionary<string, string> headers)
        {
            this._transBase.BranchHeaders = headers;
            return this;
        }

        public Msg SetPassthroughHeaders(List<string> headers)
        {
            this._transBase.PassthroughHeaders = headers;
            return this;
        }
    }
}
