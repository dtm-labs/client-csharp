using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Dtmcli
{
    public class Msg : DtmImp.AbstTrans
    {
        public Msg(IDtmClient dtmHttpClient, string gid)
        {
            this._dtmClient = dtmHttpClient;
            this._transBase = DtmImp.TransBase.NewTransBase(gid, Constant.Request.TYPE_MSG, string.Empty);
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

        public async Task<bool> DoAndSubmitDB(string queryPrepared, DbConnection db, Func<DbTransaction, Task> busiCall, CancellationToken cancellationToken = default)
        {
            return await this.DoAndSubmit(queryPrepared, async bb => 
            {
                await bb.Call(db, busiCall);
            }, cancellationToken);
        }

        public async Task<bool> DoAndSubmit(string queryPrepared, Func<BranchBarrier, Task> busiCall, CancellationToken cancellationToken = default)
        {
            var bb = new BranchBarrier(this._transBase.TransType, this._transBase.Gid, Constant.Barrier.MSG_BRANCHID, Constant.Request.TYPE_MSG);

            if (bb.IsInValid()) throw new DtmcliException($"invalid trans info: {bb.ToString()}");

            var flag = await this.Prepare(queryPrepared, cancellationToken);

            if (!flag) return false;

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
                flag = await this.Submit(cancellationToken);
            }

            if (errb != null) return false;

            return flag;
        }

        private async Task<Exception> RespAsErrorCompatible(HttpResponseMessage resp)
        {
            var str = await resp.Content?.ReadAsStringAsync() ?? string.Empty;

            // System.Net.HttpStatusCode do not contain StatusTooEarly
            if ((int)resp.StatusCode == 425 || str.Contains(Constant.ResultOngoing))
            {
                return new DtmcliException(Constant.ResultOngoing);
            }
            else if (resp.StatusCode == System.Net.HttpStatusCode.Conflict || str.Contains(Constant.ResultFailure))
            {
                return new DtmcliException(Constant.ResultFailure);
            }
            else if (resp.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return new Exception(str);
            }

            return null;
        }
    }
}
