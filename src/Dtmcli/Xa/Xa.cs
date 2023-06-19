using Dtmcli.DtmImp;
using DtmCommon;
using System;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Dtmcli
{
    public sealed class Xa : TransBase
    {
        private readonly IDtmClient _dtmClient;

        internal Xa(IDtmClient dtmHttpClient, string gid)
        {
            this._dtmClient = dtmHttpClient;
            this.Gid = gid;
            this.TransType = DtmCommon.Constant.TYPE_XA;
            this.BranchIDGen = new BranchIDGen();
        }

        internal Xa(IDtmClient dtmHttpClient)
        {
            this._dtmClient = dtmHttpClient;
        }

        public async Task<string> CallBranch(object body, string url, CancellationToken cancellationToken = default)
        {
            using var response = await _dtmClient.TransRequestBranch(
                         this,
                         HttpMethod.Post,
                         body,
                         this.BranchIDGen.NewSubBranchID(),
                         Constant.Request.BRANCH_ACTION,
                         url,
                         cancellationToken).ConfigureAwait(false);

            Exception ex = await Utils.RespAsErrorCompatible(response);
            if (null != ex)
                throw ex;

            return await response.Content.ReadAsStringAsync();
        }

        [JsonIgnore]
        public string Phase2Url { get; set; }
    }
}
