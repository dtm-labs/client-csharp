using DtmCommon;
using Dtmgrpc.DtmGImp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Dtmgrpc
{
    public class MsgGrpc
    {
        private static readonly string Action = "action";

        private readonly TransBase _transBase;
        private readonly IDtmgRPCClient _dtmClient;
        private readonly IBranchBarrierFactory _branchBarrierFactory;

        public MsgGrpc(IDtmgRPCClient dtmHttpClient, IBranchBarrierFactory branchBarrierFactory, string server, string gid)
        {
            this._dtmClient = dtmHttpClient;
            this._branchBarrierFactory = branchBarrierFactory;
            this._transBase = TransBase.NewTransBase(gid, Constant.TYPE_MSG, server, string.Empty);
        }

        public MsgGrpc Add(string action, IMessage payload)
        {
            if (this._transBase.Steps == null) this._transBase.Steps = new List<Dictionary<string, string>>();
            if (this._transBase.BinPayloads == null) this._transBase.BinPayloads = new List<byte[]>();

            this._transBase.Steps.Add(new Dictionary<string, string> { { Action, action } });
            this._transBase.BinPayloads.Add(payload.ToByteArray());
            return this;
        }

        public async Task Prepare(string queryPrepared, CancellationToken cancellationToken = default)
        {
            this._transBase.QueryPrepared = !string.IsNullOrWhiteSpace(queryPrepared) ? queryPrepared : this._transBase.QueryPrepared;

            await this._dtmClient.DtmGrpcCall(this._transBase, Constant.Op.Prepare);
        }

        public async Task Submit(CancellationToken cancellationToken = default)
        {
            await this._dtmClient.DtmGrpcCall(this._transBase, Constant.Op.Submit);
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
            var bb = _branchBarrierFactory.CreateBranchBarrier(this._transBase.TransType, this._transBase.Gid, Constant.Barrier.MSG_BRANCHID, Constant.TYPE_MSG);

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
                try
                {
                    // call queryPrepared to get the result
                    await _dtmClient.InvokeBranch<Empty, Empty>(_transBase, new Empty(), queryPrepared, bb.BranchID, bb.Op);
                }
                catch (Exception ex)
                {
                    err = Utils.GrpcError2DtmError(ex);
                }
            }

            if ((errb != null && errb is DtmFailureException) || (err != null && err is DtmFailureException))
            {
                await _dtmClient.DtmGrpcCall(this._transBase, Constant.Op.Abort);
            }
            else if (err == null)
            {
                await this.Submit(cancellationToken);
            }

            // busiCall error
            if (errb != null) throw errb;
        }

        /// <summary>
        /// Enable wait result for trans
        /// </summary>
        /// <returns></returns>
        public MsgGrpc EnableWaitResult()
        {
            this._transBase.WaitResult = true;
            return this;
        }

        /// <summary>
        /// Set timeout to fail for trans, unit is second
        /// </summary>
        /// <param name="timeoutToFail">timeout to fail</param>
        /// <returns></returns>
        public MsgGrpc SetTimeoutToFail(long timeoutToFail)
        {
            this._transBase.TimeoutToFail = timeoutToFail;
            return this;
        }

        /// <summary>
        /// Set retry interval for trans, unit is second
        /// </summary>
        /// <param name="retryInterval"></param>
        /// <returns></returns>
        public MsgGrpc SetRetryInterval(long retryInterval)
        {
            this._transBase.RetryInterval = retryInterval;
            return this;
        }

        /// <summary>
        /// Set branch headers for trans
        /// </summary>
        /// <param name="headers"></param>
        /// <returns></returns>
        public MsgGrpc SetBranchHeaders(Dictionary<string, string> headers)
        {
            this._transBase.BranchHeaders = headers;
            return this;
        }

        public MsgGrpc SetPassthroughHeaders(List<string> headers)
        {
            this._transBase.PassthroughHeaders = headers;
            return this;
        }
    }
}
