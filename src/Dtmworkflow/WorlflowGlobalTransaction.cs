﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dtmworkflow
{
    public class WorlflowGlobalTransaction
    {
        private readonly Dictionary<string, WfItem> _handlers;
        private readonly IWorkflowFactory _workflowFactory;

        public WorlflowGlobalTransaction(IWorkflowFactory workflowFactory)
        {
            this._handlers = new Dictionary<string, WfItem>();
            this._workflowFactory = workflowFactory;
        }

        public async Task<byte[]> Execute(string name, string gid, byte[] data)
        {
            if (!this._handlers.TryGetValue(name, out var handler))
            {
                throw new DtmCommon.DtmException($"workflow '{name}' not registered. please register at startup");
            }

            var wf = _workflowFactory.NewWorkflow(name, gid, data, "");

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

            this._handlers.Add(name, new WfItem
            {
                Fn = handler,
                Custom = custom.ToList()
            });
        }
    }
}
