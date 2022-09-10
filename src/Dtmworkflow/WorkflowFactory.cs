using System;
using System.Collections.Generic;
using Dtmcli;
using Dtmgrpc;

namespace Dtmworkflow
{

    public interface IWorkflowFactory
    {
        Workflow NewWorkflow(string name, string gid, byte[] data, string callback, bool isHttp = true);
    }

    public class WorkflowFactory : IWorkflowFactory
    {
        private readonly IDtmClient _httpClient;
        private readonly IDtmgRPCClient _grpcClient;

        public WorkflowFactory(IDtmClient httpClient, IDtmgRPCClient grpcClient)
        {
            this._httpClient = httpClient;
            this._grpcClient = grpcClient;
        }

        public Workflow NewWorkflow(string name, string gid, byte[] data, string callback, bool isHttp = true)
        {
            var wf = new Workflow(_httpClient, _grpcClient)
            { 
                TransBase = DtmCommon.TransBase.NewTransBase(gid, "workflow", "not inited", ""),
                Name = name,
                WorkflowImp = new WorkflowImp
                { 
                     IDGen = new DtmCommon.BranchIDGen(),
                     SucceededOps = new List<WorkflowPhase2Item>(),
                     FailedOps = new List<WorkflowPhase2Item>(),
                     CurrentOp = DtmCommon.Constant.OpAction,
                },
            };

            wf.TransBase.Protocol = isHttp ? DtmCommon.Constant.ProtocolHTTP : DtmCommon.Constant.ProtocolGRPC;            
            wf.TransBase.QueryPrepared = callback;

            wf.TransBase.CustomData = System.Text.Json.JsonSerializer.Serialize(new { name = wf.Name, data = data });

            return wf;
        }
    }

    internal class WfItem
    {
        public WfFunc2 Fn { get; set; }

        public List<Action<Workflow>> Custom { get; set; }
    }
}
