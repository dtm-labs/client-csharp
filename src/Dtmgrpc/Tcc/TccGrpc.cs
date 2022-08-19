using DtmCommon;
using Dtmgrpc.DtmGImp;
using Google.Protobuf;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dtmgrpc
{
    public class TccGrpc
    {
        private static readonly string Confirm = "confirm";
        private static readonly string Cancel = "cancel";
        private static readonly string Try = "try";

        private readonly TransBase _transBase;
        private readonly IDtmgRPCClient _dtmClient;

        public TccGrpc(IDtmgRPCClient dtmHttpClient, TransBase transBase)
        {
            this._dtmClient = dtmHttpClient;
            this._transBase = transBase;
        }

        /// <summary>
        /// Call TCC branch
        /// </summary>
        /// <typeparam name="TRequest">gRPC request</typeparam>
        /// <typeparam name="TResponse">gRPC response</typeparam>
        /// <param name="busiMsg">gRPC request</param>
        /// <param name="tryUrl">try url, don't contain http</param>
        /// <param name="confirmUrl">confirm url, don't contain http</param>
        /// <param name="cancelUrl">cancel url, don't contain http</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task CallBranch<TRequest, TResponse>(TRequest busiMsg, string tryUrl, string confirmUrl, string cancelUrl, CancellationToken cancellationToken = default)
            where TRequest : class, IMessage, new()
            where TResponse : class, IMessage, new()
        {
            var branchId = this._transBase.BranchIDGen.NewSubBranchID();

            var bd = busiMsg.ToByteString();

           var add = new Dictionary<string, string>
            {
                { Confirm, confirmUrl },
                { Cancel, cancelUrl },
            };

            await _dtmClient.RegisterBranch(this._transBase, branchId, bd, add, "");

            // NOTE: DTM server vilida gRPC url that not start with http or https, but Grpc.Net.Client should start with
            // Here will use the convention of DTM, when calling try, the client will add http.
            await _dtmClient.InvokeBranch<TRequest, TResponse>(this._transBase, busiMsg, tryUrl, branchId, Try);
        }

        internal TransBase GetTransBase() => _transBase;

        /// <summary>
        /// Enable wait result for trans
        /// </summary>
        /// <returns></returns>
        public TccGrpc EnableWaitResult()
        {
            this._transBase.WaitResult = true;
            return this;
        }

        /// <summary>
        /// Set timeout to fail for trans, unit is second
        /// </summary>
        /// <param name="timeoutToFail">timeout to fail</param>
        /// <returns></returns>
        public TccGrpc SetTimeoutToFail(long timeoutToFail)
        {
            this._transBase.TimeoutToFail = timeoutToFail;
            return this;
        }

        /// <summary>
        /// Set retry interval for trans, unit is second
        /// </summary>
        /// <param name="retryInterval"></param>
        /// <returns></returns>
        public TccGrpc SetRetryInterval(long retryInterval)
        {
            this._transBase.RetryInterval = retryInterval;
            return this;
        }

        /// <summary>
        /// Set branch headers for trans
        /// </summary>
        /// <param name="headers"></param>
        /// <returns></returns>
        public TccGrpc SetBranchHeaders(Dictionary<string, string> headers)
        {
            this._transBase.BranchHeaders = headers;
            return this;
        }

        public TccGrpc SetPassthroughHeaders(List<string> headers)
        {
            this._transBase.PassthroughHeaders = headers;
            return this;
        }
    }
}
