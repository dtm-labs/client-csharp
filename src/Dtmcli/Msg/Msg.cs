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
            this._transBase = TransBase.NewTransBase(gid, Constant.Request.TYPE_MSG, string.Empty, string.Empty);
        }

        public Msg Add(string action, object postData)
        {
            if (this._transBase.Steps == null) this._transBase.Steps = new List<Dictionary<string, string>>();
            if (this._transBase.Payloads == null) this._transBase.Payloads = new List<string>();

            this._transBase.Steps.Add(new Dictionary<string, string> { { Constant.Request.BRANCH_ACTION, action } });
            this._transBase.Payloads.Add(JsonSerializer.Serialize(postData));
            return this;
        }

        public async Task<bool> Prepare(string queryPrepared, CancellationToken cancellationToken = default)
        {
            this._transBase.QueryPrepared = !string.IsNullOrWhiteSpace(queryPrepared)? queryPrepared : this._transBase.QueryPrepared;

            return await this._dtmClient.TransCallDtm(this._transBase, this._transBase, Constant.Request.OPERATION_PREPARE, cancellationToken);
        }

        public async Task<bool> Submit(CancellationToken cancellationToken = default)
        {
            return await this._dtmClient.TransCallDtm(this._transBase, this._transBase, Constant.Request.OPERATION_SUBMIT, cancellationToken);
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
            var bb = _branchBarrierFactory.CreateBranchBarrier(this._transBase.TransType, this._transBase.Gid, Constant.Barrier.MSG_BRANCHID, Constant.Request.TYPE_MSG);            

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
            if (errb != null && !errb.Message.Contains(Constant.ResultFailure))
            {
                // if busicall return an error other than failure, we will query the result
                var resp = await _dtmClient.TransRequestBranch(this._transBase, HttpMethod.Get, null, bb.BranchID, bb.Op, queryPrepared, cancellationToken);
                err = await RespAsErrorCompatible(resp);
            }

            if ((errb != null && errb.Message.Equals(Constant.ResultFailure)) || (err != null && err.Message.Equals(Constant.ResultFailure)))
            {
                await _dtmClient.TransCallDtm(_transBase, _transBase, Constant.Request.OPERATION_ABORT, default);
            }
            else if (err == null)
            {
                await this.Submit(cancellationToken);
            }

            if (errb != null) throw errb;
        }

        private async Task<Exception> RespAsErrorCompatible(HttpResponseMessage resp)
        {
            var str = await resp.Content?.ReadAsStringAsync() ?? string.Empty;

            // System.Net.HttpStatusCode do not contain StatusTooEarly
            if ((int)resp.StatusCode == 425 || str.Contains(Constant.ResultOngoing))
            {
                return new DtmException(Constant.ResultOngoing);
            }
            else if (resp.StatusCode == System.Net.HttpStatusCode.Conflict || str.Contains(Constant.ResultFailure))
            {
                return new DtmException(Constant.ResultFailure);
            }
            else if (resp.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return new Exception(str);
            }

            return null;
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
    }
}
