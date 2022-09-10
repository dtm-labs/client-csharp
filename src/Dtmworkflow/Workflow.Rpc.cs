using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dtmworkflow
{
    public partial class Workflow
    {
        private async Task<dtmgpb.DtmProgressesReply> GetProgress()
        {
            dtmgpb.DtmProgressesReply reply;
            if (this.TransBase.Protocol == DtmCommon.Constant.ProtocolGRPC)
            {
                var req = Dtmgrpc.DtmGImp.Utils.BuildDtmRequest(this.TransBase);
                reply = await _grpcClient.PrepareWorkflow(req);
                return reply;
            }

            var resp = await _httpClient.PrepareWorkflow(this.TransBase, default);
            var res = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            reply = System.Text.Json.JsonSerializer.Deserialize<dtmgpb.DtmProgressesReply>(res);
            return reply;
        }

        private async Task Submit(byte[] result, Exception err, CancellationToken cancellationToken)
        {
            var status = WfErrorToStatus(err);
            var reason = string.Empty;

            if (err != null) reason = err.Message;

            var extra = new Dictionary<string, string>
            {
                { "status", status },
                { "rollback_reason", reason },
                { "result", Convert.ToBase64String(result) },
            };

            if (this.TransBase.Protocol == DtmCommon.Constant.ProtocolHTTP)
            {
                var m = new Dictionary<string, object>
                {
                    { "gid", this.TransBase.Gid },
                    { "trans_type", this.TransBase.TransType },
                    { "req_extra", extra },
                };

                await _httpClient.TransCallDtm(this.TransBase, m, Dtmcli.Constant.Request.OPERATION_SUBMIT, cancellationToken);
                return;
            }

            var req = Dtmgrpc.DtmGImp.Utils.BuildDtmRequest(this.TransBase);

            foreach (var item in extra)
            {
                req.ReqExtra.Add(item.Key, item.Value);
            }

            await _grpcClient.Submit(req);
        }

        private async Task RegisterBranch(byte[] res, string branchId, string op, string status, CancellationToken cancellationToken)
        {
            if (this.TransBase.Protocol == DtmCommon.Constant.ProtocolHTTP)
            {
                var m = new Dictionary<string, string>
                {
                    { "data", Encoding.UTF8.GetString(res) },
                    { "branch_id", branchId },
                    { "op", op },
                    { "status", status },
                };

                await _httpClient.TransRegisterBranch(this.TransBase, m, Dtmcli.Constant.Request.OPERATION_REGISTERBRANCH, cancellationToken);
                return;
            }

            var bd = Google.Protobuf.ByteString.CopyFrom(res);

            var added = new Dictionary<string, string>
            {
                { "op", op },
                { "status", status },
            };

            await _grpcClient.RegisterBranch(this.TransBase, branchId, bd, added, "");
        }
    }
}
