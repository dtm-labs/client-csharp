﻿using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Dtmcli
{
    public class Tcc : DtmImp.AbstTrans
    {
        public Tcc(IDtmClient dtmHttpClient, DtmImp.TransBase transBase)
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
                Constant.BranchTry,
                tryUrl,
                cancellationToken).ConfigureAwait(false);

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            // call branch error should throw exception
            var isOldVerException = response.StatusCode == System.Net.HttpStatusCode.OK && content.Contains(Constant.ErrFailure);
            var isNewVerException = (int)response.StatusCode >= Constant.FailureStatusCode;

            if (isOldVerException || isNewVerException)
            {
                throw new DtmcliException("An exception occurred when CallBranch");
            }

            return content;
        }

        internal DtmImp.TransBase GetTransBase() => _transBase; 
    }
}
