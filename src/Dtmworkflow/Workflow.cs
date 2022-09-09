using Dtmcli;
using Dtmgrpc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dtmworkflow
{
    internal partial class Workflow
    {
        public string Name { get; set; }

        public WfOptions Options { get; set; }

        public DtmCommon.TransBase TransBase { get; set; }

        public WorkflowImp WorkflowImp { get; set; }

        private readonly IDtmClient _httpClient;
        private readonly IDtmgRPCClient _grpcClient;

        /// <summary>
        /// NewBranch will start a new branch transaction
        /// </summary>
        /// <returns></returns>
        public Workflow NewBranch()
        {
            if (this.WorkflowImp.CurrentOp != DtmCommon.Constant.OpAction)
            {
                throw new DtmCommon.DtmException("should not call NewBranch() in Branch callbacks");
            }

            this.WorkflowImp.IDGen.NewSubBranchID();
            this.WorkflowImp.CurrentBranch = this.WorkflowImp.IDGen.CurrentSubBranchID();
            this.WorkflowImp.CurrentActionAdded = false;
            this.WorkflowImp.CurrentActionAdded = false;
            this.WorkflowImp.CurrentRollbackAdded = false;

            return this;
        }

        /// <summary>
        /// OnRollback will set the callback for current branch when rollback happen.
        /// If you are writing a saga transaction, then you should write the compensation here
        /// If you are writing a tcc transaction, then you should write the cancel operation here
        /// </summary>
        /// <param name="compensate"></param>
        /// <returns></returns>
        public Workflow OnRollback(WfPhase2Func compensate)
        {
            var branchId = this.WorkflowImp.CurrentBranch;

            if (this.WorkflowImp.CurrentRollbackAdded)
            {
                throw new DtmCommon.DtmException("one branch can only add one rollback callback");
            }

            this.WorkflowImp.CurrentRollbackAdded = true;
            this.WorkflowImp.FailedOps.Add(new WorkflowPhase2Item 
            {
                 BranchID = branchId,
                 Op = DtmCommon.Constant.OpCommit,
                 Fn = compensate
            });

            return this;
        }

        /// <summary>
        /// OnCommit will will set the callback for current branch when commit happen.
        /// If you are writing a tcc transaction, then you should write the confirm operation here
        /// </summary>
        /// <param name="fn"></param>
        /// <returns></returns>
        public Workflow OnCommit(WfPhase2Func fn)
        {
            var branchId = this.WorkflowImp.CurrentBranch;

            if (this.WorkflowImp.CurrentRollbackAdded)
            {
                throw new DtmCommon.DtmException("one branch can only add one commit callback");
            }

            this.WorkflowImp.CurrentCommitAdded = true;
            this.WorkflowImp.SucceededOps.Add(new WorkflowPhase2Item
            {
                BranchID = branchId,
                Op = DtmCommon.Constant.OpCommit,
                Fn = fn
            });

            return this;
        }

        /// <summary>
        /// OnFinish will both set the callback for OnCommit and OnRollback
        /// </summary>
        /// <param name="fn"></param>
        /// <returns></returns>
        public Workflow OnFinish(Func<DtmCommon.BranchBarrier, bool, Exception> fn)
        {
            WfPhase2Func commit = (bb) => fn.Invoke(bb, false);
            WfPhase2Func rollback = (bb) => fn.Invoke(bb, true);

            this.OnCommit(commit).OnRollback(rollback);

            return this;
        }

        /// <summary>
        /// Do will do an action which will be recored
        /// </summary>
        /// <param name="fn"></param>
        /// <returns></returns>
        public async Task<(byte[], Exception)> Do(Func<DtmCommon.BranchBarrier, (byte[], Exception)> fn)
        {
            var res = await this.RecordedDo(bb =>
            {
                var (r, e) = fn.Invoke(bb);
                return this.StepResultFromLocal(r, e);
            });

            return this.StepResultToLocal(res);
        }
    }

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
