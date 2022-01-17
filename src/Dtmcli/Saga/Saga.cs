using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Dtmcli.Tests")]

namespace Dtmcli
{
    public class Saga
    {
        private bool _concurrent = false;
        private Dictionary<int, List<int>> _orders = new Dictionary<int, List<int>>();

        private readonly DtmImp.TransBase _transBase;
        private readonly IDtmClient _dtmClient;

        public Saga(IDtmClient dtmHttpClient, string gid)
        {
            this._dtmClient = dtmHttpClient;
            this._transBase = DtmImp.TransBase.NewTransBase(gid, Constant.Request.TYPE_SAGA, string.Empty);
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

        public async Task<bool> Submit(CancellationToken cancellationToken = default)
        {
            if (this._concurrent)
            {
                this._transBase.CustomData = JsonSerializer.Serialize(new { orders = this._orders, concurrent = this._concurrent });
            }

            return await _dtmClient.TransCallDtm(this._transBase, this._transBase, Constant.Request.OPERATION_SUBMIT, cancellationToken).ConfigureAwait(false);
        }

        public Saga EnableWaitResult()
        { 
            this._transBase.WaitResult = true;
            return this;
        }

        internal DtmImp.TransBase GetTransBase() => this._transBase;
    }
}
