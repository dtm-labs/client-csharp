using DtmCommon;
using Dtmgrpc.DtmGImp;
using Google.Protobuf;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dtmgrpc
{
    public class SagaGrpc
    {
        private static readonly string Action = "action";
        private static readonly string Compensate = "compensate";

        private bool _concurrent = false;
        private Dictionary<int, List<int>> _orders = new Dictionary<int, List<int>>();

        private readonly TransBase _transBase;
        private readonly IDtmgRPCClient _dtmClient;

        public SagaGrpc(IDtmgRPCClient dtmHttpClient, string server, string gid)
        {
            this._dtmClient = dtmHttpClient;
            this._transBase = TransBase.NewTransBase(gid, Constant.TYPE_SAGA, server, string.Empty);
        }

        public SagaGrpc Add(string action, string compensate, IMessage payload)
        {
            if (this._transBase.Steps == null) this._transBase.Steps = new List<Dictionary<string, string>>();
            if (this._transBase.BinPayloads == null) this._transBase.BinPayloads = new List<byte[]>();

            this._transBase.Steps.Add(new Dictionary<string, string> { { Action, action }, { Compensate, compensate } });
            this._transBase.BinPayloads.Add(payload.ToByteArray());
            return this;
        }

        public SagaGrpc AddBranchOrder(int branch, List<int> preBranches)
        {
            this._orders[branch] = preBranches;
            return this;
        }

        public SagaGrpc EnableConcurrent()
        {
            this._concurrent = true;
            return this;
        }

        public async Task Submit()
        {
            if (this._concurrent)
            {
                this._transBase.CustomData = Utils.ToJsonString(new { orders = this._orders, concurrent = this._concurrent });
            }

            await _dtmClient.DtmGrpcCall(this._transBase, Constant.Op.Submit).ConfigureAwait(false);
        }

        internal TransBase GetTransBase() => this._transBase;

        /// <summary>
        /// Enable wait result for trans
        /// </summary>
        /// <returns></returns>
        public SagaGrpc EnableWaitResult()
        {
            this._transBase.WaitResult = true;
            return this;
        }

        /// <summary>
        /// Set timeout to fail for trans, unit is second
        /// </summary>
        /// <param name="timeoutToFail">timeout to fail</param>
        /// <returns></returns>
        public SagaGrpc SetTimeoutToFail(long timeoutToFail)
        {
            this._transBase.TimeoutToFail = timeoutToFail;
            return this;
        }

        /// <summary>
        /// Set retry interval for trans, unit is second
        /// </summary>
        /// <param name="retryInterval"></param>
        /// <returns></returns>
        public SagaGrpc SetRetryInterval(long retryInterval)
        {
            this._transBase.RetryInterval = retryInterval;
            return this;
        }

        /// <summary>
        /// Set branch headers for trans
        /// </summary>
        /// <param name="headers"></param>
        /// <returns></returns>
        public SagaGrpc SetBranchHeaders(Dictionary<string, string> headers)
        {
            this._transBase.BranchHeaders = headers;
            return this;
        }

        public SagaGrpc SetPassthroughHeaders(List<string> headers)
        {
            this._transBase.PassthroughHeaders = headers;
            return this;
        }
    }
}
