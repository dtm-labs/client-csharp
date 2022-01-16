using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Dtmcli
{
    public class Tcc
    {
        private readonly DtmImp.TransBase _transBase;
        private readonly IDtmClient _dtmClient;

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
                body,
                branchId,
                Constant.BranchTry,
                tryUrl,
                cancellationToken).ConfigureAwait(false);
          
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        public DtmImp.TransBase GetTransBase() => _transBase; 
    }
}
