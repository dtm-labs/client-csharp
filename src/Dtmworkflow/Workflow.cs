using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dtmworkflow
{
    internal partial class Workflow
    {
        public string Name { get; set; }

        public WfOptions Options { get; set; }

        public DtmCommon.TransBase TransBase { get; set; }

        public WorkflowImp WorkflowImp { get; set; }

        public Workflow NewBranch()
        {
            this.WorkflowImp.IDGen.NewSubBranchID();
            this.WorkflowImp.CurrentBranch = this.WorkflowImp.IDGen.CurrentSubBranchID();
            this.WorkflowImp.CurrentActionAdded = false;
            this.WorkflowImp.CurrentActionAdded = false;
            this.WorkflowImp.CurrentRollbackAdded = false;

            return this;
        }

        public Workflow OnRollback(WfPhase2Func compensate)
        {
            var branchId = this.WorkflowImp.CurrentBranch;

            if (this.WorkflowImp.CurrentRollbackAdded)
            { 
            
            }

            this.WorkflowImp.CurrentRollbackAdded = true;
            this.WorkflowImp.FailedOps.Add(new WorkflowPhase2Item 
            {
                 BranchID = branchId,
                 Op = "commit",
                 Fn = compensate
            });

            return this;
        }

        public Workflow OnCommit(WfPhase2Func fn)
        {
            var branchId = this.WorkflowImp.CurrentBranch;

            if (this.WorkflowImp.CurrentRollbackAdded)
            {

            }

            this.WorkflowImp.CurrentCommitAdded = true;
            this.WorkflowImp.SucceededOps.Add(new WorkflowPhase2Item
            {
                BranchID = branchId,
                Op = "commit",
                Fn = fn
            });

            return this;
        }

        public Workflow OnFinish(Func<DtmCommon.BranchBarrier, bool, Exception> fn)
        {
            WfPhase2Func commit = (bb) => fn.Invoke(bb, false);
            WfPhase2Func rollback = (bb) => fn.Invoke(bb, true);

            this.OnCommit(commit).OnRollback(rollback);

            return this;
        }
    }

    public delegate Exception WfPhase2Func(DtmCommon.BranchBarrier bb);


    internal delegate Exception WfFunc(Workflow wf, byte[] data);

    internal delegate (byte[], Exception) WfFunc2(Workflow wf, byte[] data);

    internal class WfOptions
    { 
        public bool CompensateErrorBranch { get; set; }
    }

    internal class WorkflowImp
    { 
        public DtmCommon.BranchIDGen IDGen { get; set; }
        public string CurrentBranch { get; set; }
        public bool CurrentActionAdded { get; set; }
        public bool CurrentCommitAdded { get; set; }
        public bool CurrentRollbackAdded { get; set; }
        public Dictionary<string, StepResult> Progresses { get; set; }
        public string CurrentOp { get; set; }
        public List<WorkflowPhase2Item> SucceededOps { get; set; }
        public List<WorkflowPhase2Item> FailedOps { get; set; }
    }

    internal class WorkflowPhase2Item
    { 
        public string BranchID { get; set; }

        public string Op { get; set; }

        public WfPhase2Func Fn { get; set; }
    }

    internal class StepResult
    { 
        public Exception Error { get; set; }

        public string Status { get; set; }

        public byte[] Data { get; set; }
    }
}
