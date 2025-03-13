using Google.Protobuf;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Dtmworkflow
{
    public partial class Workflow
    {
        private void InitProgress(DtmProgressDto[] progresses)
        {
            this.WorkflowImp.Progresses = new Dictionary<string, StepResult>();

            foreach (var p in progresses)
            {
                var sr = new StepResult
                {
                    Status = p.Status,
                    Data = p.BinData,
                };

                if (sr.Status == DtmCommon.Constant.StatusFailed)
                {
                    sr.Error = new DtmCommon.DtmFailureException(Encoding.UTF8.GetString(p.BinData));
                }

                this.WorkflowImp.Progresses[$"{p.BranchId}-{p.Op}"] = sr;
            }
        }

        internal async Task<byte[]> Process(WfFunc2 handler, byte[] data)
        {
            var reply = await this.GetProgress();

            var status = reply.Transaction.Status;
            if (status == DtmCommon.Constant.StatusSucceed)
            {
                var sRes = reply.Transaction.Result != null
                    ? Convert.FromBase64String(reply.Transaction.Result)
                    : null;
                return sRes;
            }
            else if (status == DtmCommon.Constant.StatusFailed)
            {
                throw new DtmCommon.DtmFailureException(reply.Transaction.RollbackReason);
            }

            this.InitProgress(reply.Progresses.ToArray());

            byte[] res = null;
            Exception err = null;

            try
            {
                res = await handler(this, data);
            }
            catch (Exception ex)
            {
                err = ex;
            }

            err = Utils.GrpcError2DtmError(err);
            
            if (err != null && err is not DtmCommon.DtmFailureException) throw err;

            try
            {
                await this.ProcessPhase2(err);
            }
            catch (Exception ex)
            {
                err = ex;
            }

            if (err == null || err is DtmCommon.DtmFailureException)
            {
                await this.Submit(res, err, default);
            }

            return res;
        }

        private async Task SaveResult(string branchId, string op, StepResult sr)
        {
            if (!string.IsNullOrWhiteSpace(sr.Status))
            {
                await this.RegisterBranch(sr.Data, branchId, op, sr.Status, default);
                return;
            }

            if (sr.Error != null) throw sr.Error;
        }

        private async Task ProcessPhase2(Exception err)
        {
            var ops = this.WorkflowImp.SucceededOps;
            if (err == null)
            {
                this.WorkflowImp.CurrentOp = DtmCommon.Constant.OpCommit;
            }
            else
            {
                this.WorkflowImp.CurrentOp = DtmCommon.Constant.OpRollback;
                ops = this.WorkflowImp.FailedOps;
            }

            for (int i = ops.Count - 1; i >= 0; i--)
            {
                var op = ops[i];

                await this.CallPhase2(op.BranchID, op.Fn);
            }

            if (err != null) throw err;
        }

        private async Task CallPhase2(string branchId, WfPhase2Func fn)
        {
            this.WorkflowImp.CurrentBranch = branchId;

            var r = await this.RecordedDo(async bb =>
            {
                Exception err = null;

                try
                {
                    await fn.Invoke(bb);
                }
                catch (Exception ex)
                {
                    err = ex;
                }

                if (err is DtmCommon.DtmFailureException)
                {
                    throw new DtmCommon.DtmException("should not return ErrFail in phase2");
                }

                return this.StepResultFromLocal(null, err);
            });

            this.StepResultToLocal(r);
        }

        private (byte[], Exception) StepResultToLocal(StepResult r)
        {
            return (r.Data, r.Error);
        }

        private StepResult StepResultFromLocal(byte[] data, Exception err)
        {
            return new StepResult
            {
                Error = err,
                Status = WfErrorToStatus(err),
                Data = data
            };
        }

        internal Exception StepResultToGrpc(StepResult r, IMessage reply)
        {
            if (r.Error == null && r.Status == DtmCommon.Constant.StatusSucceed)
            {
                // TODO Check 
                // dtmgimp.MustProtoUnmarshal(s.Data, reply.(protoreflect.ProtoMessage));
            }

            return r.Error;
        }

        internal StepResult StepResultFromGrpc(IMessage reply, Exception err)
        {
            var sr = new StepResult
            {
                // TODO GRPCError2DtmError
                Error = Utils.GrpcError2DtmError(err),
            };

            sr.Status = WfErrorToStatus(sr.Error);
            if (sr.Error == null)
            {
                sr.Data = reply.ToByteArray();
            }
            else if (sr.Status == DtmCommon.Constant.StatusFailed)
            {
                sr.Data = Encoding.UTF8.GetBytes(err.Message);
            }

            return sr;
        }

        internal HttpResponseMessage StepResultToHttp(StepResult r)
        {
            if (r.Error != null)
            {
                throw r.Error;
            }

            return Utils.NewJSONResponse(HttpStatusCode.OK, r.Data);
        }

        internal StepResult StepResultFromHTTP(HttpResponseMessage resp, Exception err)
        {
            var sr = new StepResult
            {
                Error = err,
            };

            if (err == null)
            {
                (sr.Data, sr.Error) = Utils.HTTPResp2DtmError(resp); // TODO go 使用了 this.Options.HTTPResp2DtmError(resp), 方便定制
                sr.Status = WfErrorToStatus(sr.Error);
            }

            return sr;
        }

        private string WfErrorToStatus(Exception err)
        {
            if (err == null)
            {
                return DtmCommon.Constant.StatusSucceed;
            }
            else if (err is DtmCommon.DtmFailureException)
            {
                return DtmCommon.Constant.StatusFailed;
            }

            return string.Empty;
        }


        internal async Task<StepResult> RecordedDo(Func<DtmCommon.BranchBarrier, Task<StepResult>> fn)
        {
            StepResult sr = await this.RecordedDoInner(fn);

            // do not compensate the failed branch if !CompensateErrorBranch
            if (this.Options.CompensateErrorBranch && sr.Status == DtmCommon.Constant.StatusFailed)
            {
                var lastFailed = this.WorkflowImp.FailedOps.Count - 1;
                if (lastFailed >= 0 && this.WorkflowImp.FailedOps[lastFailed].BranchID == this.WorkflowImp.CurrentBranch)
                {
                    this.WorkflowImp.FailedOps = this.WorkflowImp.FailedOps.GetRange(0, lastFailed);
                }
            }

            return sr;
        }

        private async Task<StepResult> RecordedDoInner(Func<DtmCommon.BranchBarrier, Task<StepResult>> fn)
        {
            var branchId = this.WorkflowImp.CurrentBranch;
            if (this.WorkflowImp.CurrentOp == DtmCommon.Constant.OpAction)
            {
                if (this.WorkflowImp.CurrentActionAdded)
                {
                    throw new DtmCommon.DtmException("one branch can have only on action");
                }

                this.WorkflowImp.CurrentActionAdded = true;
            }

            var r = this.GetStepResult();
            if (r != null)
            {
                _logger.LogDebug("progress restored: '{0}' '{1}' '{2}' '{3}' '{4}'", branchId, this.WorkflowImp.CurrentOp, r.Error, r.Status, r.Data);
                return r;
            }

            var bb = _bbFactory.CreateBranchBarrier(
                transType: this.TransBase.TransType,
                gid: this.TransBase.Gid,
                branchID: branchId,
                op: this.WorkflowImp.CurrentOp);

            r = await fn(bb);

            try
            {
                await this.SaveResult(branchId, this.WorkflowImp.CurrentOp, r);
            }
            catch (Exception ex)
            {
                r = this.StepResultFromLocal(null, ex);
            }

            return r;
        }

        private StepResult GetStepResult()
        {
            var key = $"{this.WorkflowImp.CurrentBranch}-{this.WorkflowImp.CurrentOp}";
            StepResult res = null;
            this.WorkflowImp.Progresses.TryGetValue(key, out res);

            _logger.LogDebug("getStepResult: {0} {1}", key, res);

            return res;
        }
    }
}