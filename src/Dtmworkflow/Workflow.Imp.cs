using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dtmworkflow
{
    internal partial class Workflow
    {

        private (byte[] Res, Exception Err) Process(WfFunc2 handler, byte[] data)
        {
            var (reply, err2) = this.GetProgress();
            if (err2 != null)
            { 
                return (null, err2);
            }

            var status = reply.Transaction.Status;
            if (status == DtmCommon.Constant.StatusSucceed)
            {

            }
            else if (status == DtmCommon.Constant.StatusFailed) 
            {
            
            }

            return (null, null);
        }

        private Exception SaveResult(string branchId, string op, StepResult sr)
        {
            if (!string.IsNullOrWhiteSpace(sr.Status))
            {
                var err = this.RegisterBranch(sr.Data, branchId, op, sr.Status);
                if (err != null)
                {
                    return err;
                }
            }

            return sr.Error;
        }

        private Exception ProcessPhase2(Exception err)
        {
            var ops = this.WorkflowImp.SucceededOps;
            if (err == null)
            {
                this.WorkflowImp.CurrentOp = "";
            }
            else
            {
                this.WorkflowImp.CurrentOp = "";
                ops = this.WorkflowImp.FailedOps;
            }

            for (int i = ops.Count - 1; i >= 0 ; i--)
            {
                var op = ops[i];

                var err1 = this.CallPhase2(op.BranchID, op.Fn);
                if (err1 != null) return err1;
            }

            return err;
        }

        private Exception CallPhase2(string branchId, WfPhase2Func fn)
        {
            this.WorkflowImp.CurrentBranch = branchId;

            var r = this.RecordedDo(bb => 
            {
                var err = fn.Invoke(bb);

                return this.StepResultFromLocal(null, err);
            });

            var item = this.StepResultToLocal(r);

            return null;
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

        private string WfErrorToStatus(Exception err)
        {
            if (err == null)
            {
                return "";
            }
            else if (err is DtmCommon.DtmFailureException)
            {
                return "";
            }

            return "";
        }


        private StepResult RecordedDo(Func<DtmCommon.BranchBarrier, StepResult> fn)
        {
            return null;
        }

        private StepResult RecordedDoInner(Func<DtmCommon.BranchBarrier, StepResult> fn)
        {
            var branchId = this.WorkflowImp.CurrentBranch;
            if (this.WorkflowImp.CurrentOp == "")
            {

                this.WorkflowImp.CurrentActionAdded = true;
            }

            var r = this.GetStepResult();
            if (r != null)
            {
                // log
                return r;
            }

            // TODO
            DtmCommon.BranchBarrier bb = null;

            r = fn(bb);

            return null;
        }

        private StepResult GetStepResult()
        {
            var key = $"{this.WorkflowImp.CurrentBranch}-{this.WorkflowImp.CurrentOp}";

            return this.WorkflowImp.Progresses.TryGetValue(key, out var res) ? res : null;
        }
    }
}
