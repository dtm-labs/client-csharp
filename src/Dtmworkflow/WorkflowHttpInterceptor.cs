using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

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
            var uriBuilder = new UriBuilder(request.RequestUri);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query["branch_id"] = _wf.WorkflowImp.CurrentBranch;
            query["gid"] = _wf.TransBase.Gid;
            query["op"] = _wf.WorkflowImp.CurrentOp;
            query["trans_type"] = _wf.TransBase.TransType;
            query["dtm"] = _wf.TransBase.Dtm;
            uriBuilder.Query = query.ToString();
            request.RequestUri = uriBuilder.Uri;            

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