using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Dtmworkflow;

internal class WorkflowHttpInterceptor : DelegatingHandler
{
    private readonly Workflow _wf;

    public WorkflowHttpInterceptor(Workflow wf)
    {
        this._wf = wf;
        InnerHandler = new HttpClientHandler();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Func<DtmCommon.BranchBarrier, Task<StepResult>> origin = async (barrier) =>
        {
            var response = await base.SendAsync(request, cancellationToken);
            return _wf.StepResultFromHTTP(response, null);
        };

        StepResult sr;
        // in phase 2, do not save, because it is saved outer
        if (_wf.WorkflowImp.CurrentOp != DtmCommon.Constant.OpAction)
        {
            sr = await origin(null);
        }
        else
        {
            sr = await _wf.RecordedDo(origin);
        }

        return _wf.StepResultToHttp(sr);
    }
}