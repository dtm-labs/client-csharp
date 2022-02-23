using DtmCommon;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Dtmcli
{
    public class Tcc
    {
        private static readonly int FailureStatusCode = 400;

        private readonly TransBase _transBase;
        private readonly IDtmClient _dtmClient;

        public Tcc(IDtmClient dtmHttpClient, TransBase transBase)
        {
            this._dtmClient = dtmHttpClient;
            this._transBase = transBase;
        }

        public async Task<string> CallBranch(object body, string tryUrl, string confirmUrl, string cancelUrl, CancellationToken cancellationToken = default)
        {
            var branchId = this._transBase.BranchIDGen.NewSubBranchID();

            await _dtmClient.TransRegisterBranch(_transBase, new Dictionary<string, string> 
            {
                { Constant.Request.DATA, JsonSerializer.Serialize(body) },
                { Constant.Request.BRANCH_ID, branchId },
                { Constant.Request.CONFIRM, confirmUrl },
                { Constant.Request.CANCEL, cancelUrl },
            }, Constant.Request.OPERATION_REGISTERBRANCH, cancellationToken);

            var response = await _dtmClient.TransRequestBranch(
                _transBase,
                System.Net.Http.HttpMethod.Post,
                body,
                branchId,
                Constant.Request.TRY,
                tryUrl,
                cancellationToken).ConfigureAwait(false);

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            // call branch error should throw exception
            var isOldVerException = response.StatusCode == System.Net.HttpStatusCode.OK && content.Contains(DtmCommon.Constant.ResultFailure);
            var isNewVerException = (int)response.StatusCode >= FailureStatusCode;

            if (isOldVerException || isNewVerException)
            {
                throw new DtmException($"An exception occurred when CallBranch, status={response.StatusCode}, content={content}, ");
            }

            return content;
        }

        internal TransBase GetTransBase() => _transBase;

        /// <summary>
        /// Enable wait result for trans
        /// </summary>
        /// <returns></returns>
        public Tcc EnableWaitResult()
        {
            this._transBase.WaitResult = true;
            return this;
        }

        /// <summary>
        /// Set timeout to fail for trans, unit is second
        /// </summary>
        /// <param name="timeoutToFail">timeout to fail</param>
        /// <returns></returns>
        public Tcc SetTimeoutToFail(long timeoutToFail)
        {
            this._transBase.TimeoutToFail = timeoutToFail;
            return this;
        }

        /// <summary>
        /// Set retry interval for trans, unit is second
        /// </summary>
        /// <param name="retryInterval"></param>
        /// <returns></returns>
        public Tcc SetRetryInterval(long retryInterval)
        {
            this._transBase.RetryInterval = retryInterval;
            return this;
        }

        /// <summary>
        /// Set branch headers for trans
        /// </summary>
        /// <param name="headers"></param>
        /// <returns></returns>
        public Tcc SetBranchHeaders(Dictionary<string, string> headers)
        {
            this._transBase.BranchHeaders = headers;
            return this;
        }

        public Tcc SetPassthroughHeaders(List<string> headers)
        {
            this._transBase.PassthroughHeaders = headers;
            return this;
        }
    }
}
