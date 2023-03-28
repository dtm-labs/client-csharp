using Dtmcli;
using Dtmgrpc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dtmworkflow
{
    public partial class Workflow
    {
        public string Name { get; set; }

        public virtual WfOptions Options { get; set; }

        public virtual DtmCommon.TransBase TransBase { get; set; }

        public virtual WorkflowImp WorkflowImp { get; set; } = new WorkflowImp();

        private readonly IDtmClient _httpClient;
        private readonly IDtmgRPCClient _grpcClient;
        private readonly Dtmcli.IBranchBarrierFactory _bbFactory;
        private readonly ILogger _logger;

        public Workflow(IDtmClient httpClient, IDtmgRPCClient grpcClient, Dtmcli.IBranchBarrierFactory bbFactory, ILogger logger)
        {
            this._httpClient = httpClient;
            this._grpcClient = grpcClient;
            this._bbFactory = bbFactory;
            this._logger = logger;
        }

        public System.Net.Http.HttpClient NewRequest()
        {
            return _httpClient.GetHttpClient("WF");
        }

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
            this.WorkflowImp.CurrentCommitAdded = false;
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

            if (this.WorkflowImp.CurrentCommitAdded)
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
        public Workflow OnFinish(Action<DtmCommon.BranchBarrier, bool> fn)
        {
            WfPhase2Func commit = (bb) => { fn.Invoke(bb, false); return Task.CompletedTask; };
            WfPhase2Func rollback = (bb) => { fn.Invoke(bb, true); return Task.CompletedTask; };

            this.OnCommit(commit).OnRollback(rollback);

            return this;
        }

        /// <summary>
        /// Do will do an action which will be recored
        /// </summary>
        /// <param name="fn"></param>
        /// <returns></returns>
        public async Task<(byte[], Exception)> Do(Func<DtmCommon.BranchBarrier, Task<(byte[], Exception)>> fn)
        {
            var res = await this.RecordedDo(async bb =>
            {
                var (r, e) = await fn.Invoke(bb);
                return this.StepResultFromLocal(r, e);
            });

            return this.StepResultToLocal(res);
        }
    }

    public class WfOptions
    {
        public bool CompensateErrorBranch { get; set; }
    }

    public class WorkflowImp
    {
        public DtmCommon.BranchIDGen IDGen { get; set; }
        public string CurrentBranch { get; set; }
        public bool CurrentActionAdded { get; set; }
        public bool CurrentCommitAdded { get; set; }
        public bool CurrentRollbackAdded { get; set; }
        public Dictionary<string, StepResult> Progresses { get; set; }
        public string CurrentOp { get; set; }
        public List<WorkflowPhase2Item> SucceededOps { get; set; } = new List<WorkflowPhase2Item>();
        public List<WorkflowPhase2Item> FailedOps { get; set; } = new List<WorkflowPhase2Item>();
    }

    public class WorkflowPhase2Item
    {
        public string BranchID { get; set; }

        public string Op { get; set; }

        public WfPhase2Func Fn { get; set; }
    }

    public class StepResult
    {
        public Exception Error { get; set; }

        public string Status { get; set; }

        public byte[] Data { get; set; }
    }
}