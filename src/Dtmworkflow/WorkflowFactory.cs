using System;
using System.Collections.Generic;
using Dtmcli;
using DtmCommon;
using Dtmgrpc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dtmworkflow
{
    public interface IWorkflowFactory
    {
        Workflow NewWorkflow(string name, string gid, byte[] data, bool isHttp = true);
    }

    public class WorkflowFactory : IWorkflowFactory
    {
        private readonly DtmOptions _dtmOptions;
        private readonly IDtmClient _httpClient;
        private readonly IDtmgRPCClient _grpcClient;
        private readonly Dtmcli.IBranchBarrierFactory _bbFactory;
        private readonly ILogger _logger;

        public WorkflowFactory(IDtmClient httpClient, IDtmgRPCClient grpcClient, Dtmcli.IBranchBarrierFactory bbFactory, IOptions<DtmOptions> optionsAccs, ILoggerFactory loggerFactory)
        {
            this._httpClient = httpClient;
            this._grpcClient = grpcClient;
            this._bbFactory = bbFactory;
            this._dtmOptions = optionsAccs.Value;
            this._logger = loggerFactory.CreateLogger<WorkflowFactory>();
        }

        public Workflow NewWorkflow(string name, string gid, byte[] data, bool isHttp = true)
        {
            var wf = new Workflow(_httpClient, _grpcClient, _bbFactory, _logger)
            {
                TransBase = TransBase.NewTransBase(gid, "workflow", "not inited", ""),
                Name = name,
                WorkflowImp = new WorkflowImp
                {
                    IDGen = new BranchIDGen(),
                    SucceededOps = new List<WorkflowPhase2Item>(),
                    FailedOps = new List<WorkflowPhase2Item>(),
                    CurrentOp = DtmCommon.Constant.OpAction,
                },
                Options = new WfOptions
                {
                    CompensateErrorBranch = true
                }
            };

            wf.TransBase.Protocol = isHttp ? DtmCommon.Constant.ProtocolHTTP : DtmCommon.Constant.ProtocolGRPC;
            wf.TransBase.QueryPrepared = isHttp ? _dtmOptions.HttpCallback : _dtmOptions.GrpcCallback;

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
