using Dtmcli.DtmImp;
using DtmCommon;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using static Dtmcli.Constant;

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


#if NET5_0_OR_GREATER
        public static Xa FromQuery(IDtmClient dtmClient, Microsoft.AspNetCore.Http.IQueryCollection quersy)
        {
            if (quersy.TryGetValue(Request.GID, out var gid) == false || string.IsNullOrEmpty(gid))
                throw new ArgumentNullException(Request.GID);

            if (quersy.TryGetValue(Request.TRANS_TYPE, out var transType) == false || string.IsNullOrEmpty(transType))
                throw new ArgumentNullException(Request.TRANS_TYPE);

            if (quersy.TryGetValue(Request.OP, out var op) == false || string.IsNullOrEmpty(op))
                throw new ArgumentNullException(Request.OP);

            if (quersy.TryGetValue(Request.BRANCH_ID, out var branchID) == false || string.IsNullOrEmpty(branchID))
                throw new ArgumentNullException(Request.BRANCH_ID);

            quersy.TryGetValue(Request.DTM, out var dtm);
            quersy.TryGetValue(Request.PHASE2_URL, out var phase2Url);

            return new(dtmClient)
            {
                Gid = gid,
                Dtm = dtm,
                Op = op,
                TransType = transType,
                Phase2Url = phase2Url,
                BranchIDGen = new BranchIDGen(branchID),
            };
        }
#else
        public static Xa FromQuery(IDtmClient dtmClient, IDictionary<string, string> quersy)
        {
            if (!quersy.TryGetValue(Request.GID, out var gid) == false || string.IsNullOrEmpty(gid))
                throw new ArgumentNullException(Request.GID);

            if (quersy.TryGetValue(Request.TRANS_TYPE, out var transType) == false || string.IsNullOrEmpty(transType))
                throw new ArgumentNullException(Request.TRANS_TYPE);

            if (quersy.TryGetValue(Request.OP, out var op) == false || string.IsNullOrEmpty(op))
                throw new ArgumentNullException(Request.OP);

            if (quersy.TryGetValue(Request.BRANCH_ID, out var branchID) == false || string.IsNullOrEmpty(branchID))
                throw new ArgumentNullException(Request.BRANCH_ID);

            quersy.TryGetValue(Request.DTM, out var dtm);
            quersy.TryGetValue(Request.PHASE2_URL, out var phase2Url);

            return new(dtmClient)
            {
                Gid = gid,
                Dtm = dtm,
                Op = op,
                TransType = transType,
                Phase2Url = phase2Url,
                BranchIDGen = new BranchIDGen(branchID),
            };
        }
#endif
    }
}
