using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dtmworkflow
{
    public class WorkflowGlobalTransaction
    {
        private readonly Dictionary<string, WfItem> _handlers;
        private readonly IWorkflowFactory _workflowFactory;
        private readonly ILogger _logger;

        public WorkflowGlobalTransaction(IWorkflowFactory workflowFactory, ILoggerFactory loggerFactory)
        {
            this._handlers = new Dictionary<string, WfItem>();
            this._workflowFactory = workflowFactory;
            this._logger = loggerFactory.CreateLogger<WorkflowGlobalTransaction>();
        }

        public async Task<byte[]> Execute(string name, string gid, byte[] data, bool isHttp = true)
        {
            return await this.Execute(name, gid, data, null, isHttp);
        }

        public async Task<byte[]> Execute(string name, string gid, byte[] data, Action<Workflow> wfAction, bool isHttp = true)
        {
            if (!this._handlers.TryGetValue(name, out var handler))
            {
                throw new DtmCommon.DtmException($"workflow '{name}' not registered. please register at startup");
            }

            var wf = _workflowFactory.NewWorkflow(name, gid, data, isHttp);
            if (wfAction != null)
                wfAction(wf);

            foreach (var fn in handler.Custom)
            {
                fn(wf);
            }

            return await wf.Process(handler.Fn, data);
        }

        public void Register(string name, WfFunc2 handler, params Action<Workflow>[] custom)
        {
            if (this._handlers.TryGetValue(name, out _))
            {
                throw new DtmCommon.DtmException($"a handler already exists for {name}");
            }

            _logger.LogDebug("workflow '{0}' registered.", name);

            this._handlers.Add(name, new WfItem
            {
                Fn = handler,
                Custom = custom.ToList()
            });
        }
        
#if NET5_0_OR_GREATER
        public async Task ExecuteByQS(Microsoft.AspNetCore.Http.IQueryCollection query, byte[] body)
        {
            _ = query.TryGetValue("gid", out var gid);
            _ = query.TryGetValue("op", out var op);

            await Execute(op, gid, body, true);
        }
#endif
        
        public bool Exists(string name)
        {
            return this._handlers.ContainsKey(name);
        }
    }
}
