using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Dtmworkflow
{
    public partial class Workflow
    {
        private async Task<DtmProgressesReplyDto> GetProgress()
        {
            DtmProgressesReplyDto reply;
            if (this.TransBase.Protocol == DtmCommon.Constant.ProtocolGRPC)
            {
                var req = Dtmgrpc.DtmGImp.Utils.BuildDtmRequest(this.TransBase);
                var tmpReply = await _grpcClient.PrepareWorkflow(req);
                reply = DtmProgressesReplyDto.FromGrpcReply(tmpReply);
                return reply;
            }

            var resp = await _httpClient.PrepareWorkflow(this.TransBase, default);
            var res = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            reply = System.Text.Json.JsonSerializer.Deserialize<DtmProgressesReplyDto>(res);
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
                { "result", result == null ? "" : Convert.ToBase64String(result) },
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
                    { "data", res == null ? "" : Encoding.UTF8.GetString(res) },
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

    public class DtmProgressesReplyDto
    {
        public DtmTransactionDto Transaction { get; set; }
        public List<DtmProgressDto> Progresses { get; set; }

        public static DtmProgressesReplyDto FromGrpcReply(dtmgpb.DtmProgressesReply reply)
        {
            var trans = new DtmTransactionDto
            {
                Gid = reply.Transaction.Gid,
                Status = reply.Transaction.Status,
                RollbackReason = reply.Transaction.RollbackReason,
                Result = reply.Transaction.Result,
            };

            var processes = new List<DtmProgressDto>();

            foreach (var item in reply.Progresses)
            {
                processes.Add(new DtmProgressDto
                {
                    BranchId = item.BranchID,
                    Op = item.Op,
                    Status = item.Status,
                    BinData = item.BinData.ToByteArray()
                });
            }

            return new DtmProgressesReplyDto
            {
                Progresses = processes,
                Transaction = trans
            };
        }
    }

    public class DtmTransactionDto
    {
        [JsonPropertyName("gid")]
        public string Gid { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("rollback_reason")]
        public string RollbackReason { get; set; }

        [JsonPropertyName("result")]
        public string Result { get; set; }
    }

    public class DtmProgressDto
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonIgnore]
        public byte[] BinData { get; set; }

        [JsonPropertyName("branch_id")]
        public string BranchId { get; set; }

        [JsonPropertyName("op")]
        public string Op { get; set; }
    }
}
