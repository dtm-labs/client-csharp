using System;
using System.Collections.Generic;
using System.Linq;

namespace Dtmworkflow
{
    internal class WorkflowFactory
    {
        public string Protocol { get; set; }
        public string HttpDtm { get; set; }
        public string HttpCallback { get; set; }
        public string GrpcDtm { get; set; }
        public string GrpcCallback { get; set; }

        public Dictionary<string, WfItem> Handlers { get; set; }

        public byte[] Execute(string name, string gid, byte[] data)
        {
            if (!this.Handlers.TryGetValue(name, out var handler))
            {
                throw new DtmCommon.DtmException($"workflow '{name}' not registered. please register at startup");
            }

            return null;
        }

        public void Register(string name, WfFunc2 handler, params Action<Workflow>[] custom)
        {
            if (this.Handlers.TryGetValue(name, out _))
            {
                throw new DtmCommon.DtmException($"a handler already exists for {name}");
            }

            this.Handlers.Add(name, new WfItem 
            {
                 Fn = handler,
                 Custom = custom.ToList()
            });
        }

        private Workflow NewWorkflow(string name, string gid, byte[] data)
        {
            var wf = new Workflow
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

            wf.TransBase.Protocol = this.Protocol;

            if (this.Protocol == DtmCommon.Constant.ProtocolGRPC)
            {
                wf.TransBase.Dtm = this.GrpcDtm;
                wf.TransBase.QueryPrepared = this.GrpcCallback;
            }
            else
            {
                wf.TransBase.Dtm = this.HttpDtm;
                wf.TransBase.QueryPrepared = this.HttpCallback;
            }

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
