using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Dtmcli
{
    public class Msg
    {
        private readonly DtmImp.TransBase _transBase;
        private readonly IDtmClient _dtmClient;

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

        public Msg EnableWaitResult()
        {
            this._transBase.WaitResult = true;
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

        /// <summary>
        /// one method for the entire prepare->busi->submit
        /// </summary>
        /// <param name="queryPrepared">A url that dtm use to query the prepared status.</param>
        /// <param name="db"></param>
        /// <param name="busiCall"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="DtmcliException"></exception>
        public async Task<bool> PrepareAndSubmit(string queryPrepared, DbConnection db, Func<DbTransaction, Task> busiCall, CancellationToken cancellationToken = default)
        {
            var bb = new BranchBarrier(this._transBase.TransType, this._transBase.Gid, Constant.Barrier.MSG_BRANCHID, Constant.Request.TYPE_MSG);

            if (bb.IsInValid()) throw new DtmcliException($"invalid trans info: {bb.ToString()}");

            var flag = await this.Prepare(queryPrepared, cancellationToken);

            if (!flag) return false;

            using (db)
            {
                flag = await bb.Call(db, busiCall);

                var res = await bb.QueryPrepared(db);

                if (!flag && res.Equals(Constant.ErrFailure))
                {
                    await _dtmClient.TransCallDtm(_transBase, _transBase, Constant.Request.OPERATION_ABORT, default);
                }
            }

            if(flag)
            {
                flag = await this.Submit(cancellationToken);
            }

            return flag;
        }
    }
}
