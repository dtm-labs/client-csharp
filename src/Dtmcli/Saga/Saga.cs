using DtmCommon;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Dtmcli
{
    public class Saga
    {
        private bool _concurrent = false;
        private Dictionary<int, List<int>> _orders = new Dictionary<int, List<int>>();

        private readonly TransBase _transBase;
        private readonly IDtmClient _dtmClient;

        public Saga(IDtmClient dtmHttpClient, string gid)
        {
            this._dtmClient = dtmHttpClient;
            this._transBase = TransBase.NewTransBase(gid, DtmCommon.Constant.TYPE_SAGA, string.Empty, string.Empty);
        }

        public Saga Add(string action, string compensate, object postData)
        {
            if (this._transBase.Steps == null) this._transBase.Steps = new List<Dictionary<string, string>>();
            if (this._transBase.Payloads == null) this._transBase.Payloads = new List<string>();

            this._transBase.Steps.Add(new Dictionary<string, string> { { Constant.Request.BRANCH_ACTION, action }, { Constant.Request.BRANCH_COMPENSATE, compensate } });
            this._transBase.Payloads.Add(JsonSerializer.Serialize(postData));
            return this;
        }

        public Saga AddBranchOrder(int branch, List<int> preBranches)
        { 
            this._orders[branch] = preBranches;
            return this;
        }

        public Saga EnableConcurrent()
        {
            this._concurrent = true;
            return this;
        }

        public async Task Submit(CancellationToken cancellationToken = default)
        {
            if (this._concurrent)
            {
                this._transBase.CustomData = JsonSerializer.Serialize(new { orders = this._orders, concurrent = this._concurrent });
            }

            await _dtmClient.TransCallDtm(this._transBase, this._transBase, Constant.Request.OPERATION_SUBMIT, cancellationToken).ConfigureAwait(false);
        }

        internal TransBase GetTransBase() => this._transBase;

        /// <summary>
        /// Enable wait result for trans
        /// </summary>
        /// <returns></returns>
        public Saga EnableWaitResult()
        {
            this._transBase.WaitResult = true;
            return this;
        }

        /// <summary>
        /// Set timeout to fail for trans, unit is second
        /// </summary>
        /// <param name="timeoutToFail">timeout to fail</param>
        /// <returns></returns>
        public Saga SetTimeoutToFail(long timeoutToFail)
        {
            this._transBase.TimeoutToFail = timeoutToFail;
            return this;
        }

        /// <summary>
        /// Set retry interval for trans, unit is second
        /// </summary>
        /// <param name="retryInterval"></param>
        /// <returns></returns>
        public Saga SetRetryInterval(long retryInterval)
        {
            this._transBase.RetryInterval = retryInterval;
            return this;
        }

        /// <summary>
        /// Set branch headers for trans
        /// </summary>
        /// <param name="headers"></param>
        /// <returns></returns>
        public Saga SetBranchHeaders(Dictionary<string, string> headers)
        {
            this._transBase.BranchHeaders = headers;
            return this;
        }

        public Saga SetPassthroughHeaders(List<string> headers)
        {
            this._transBase.PassthroughHeaders = headers;
            return this;
        }
    }
}
