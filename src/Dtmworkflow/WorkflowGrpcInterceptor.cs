using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace Dtmworkflow;

public class WorkflowGrpcInterceptor : Interceptor
{
    private Workflow _wf;
    private readonly ILogger<WorkflowGrpcInterceptor> _logger;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="wf">没找到可行的方法把wf调用时传进来(如UserState等)</param>
    /// <param name="logger"></param>
    public WorkflowGrpcInterceptor(Workflow wf, ILogger<WorkflowGrpcInterceptor> logger)
    {
        _wf = wf;
        _logger = logger;
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        _logger.LogDebug($"grpc client calling: {context.Host}{context.Method.FullName}"); //{dtmimp.MustMarshalString(request)}

        Workflow wf = _wf;
        if (context.Options.Headers != null)
        {
            // 这里需要根据实际情况从 context 中获取 Workflow 对象
            // 假设 Workflow 对象是通过某种方式存储在 Metadata 中
            // 这里只是占位，实际使用时要替换为真实的逻辑

            wf = (Workflow)(new object());
        }

        if (wf == null)
        {
            return base.AsyncUnaryCall(request, context, continuation);
        }

        // async Task<(TResponse response, Status status, Metadata trailers, Func<System.Threading.Tasks.Task> dispose)> Origin()
        async Task<TResponse> Origin()
        {
            var newContext = Dtmgimp.TransInfo2Ctx(context, wf.TransBase.Gid, wf.TransBase.TransType, wf.WorkflowImp.CurrentBranch, wf.WorkflowImp.CurrentOp, wf.TransBase.Dtm);
            TResponse response = await continuation(request, newContext);
            // var res =
            //     $"grpc client called: {context.Host}{context.Method.FullName} {dtmimp.MustMarshalString(request)} result: {dtmimp.MustMarshalString(call.response)} err: {call.status.StatusCode}";
            // if (response.status.StatusCode != StatusCode.OK)
            // {
            //     _logger.LogError(res);
            // }
            // else
            // {
            //     _logger.LogDebug(res);
            // }

            return response;
        }

        if (wf.WorkflowImp.CurrentOp != DtmCommon.Constant.OpAction)
        {
            var response = Origin();
            return new AsyncUnaryCall<TResponse>(response, null, null, null, null);
        }

        StepResult sr = wf.RecordedDo(bb =>
        {
            Task<TResponse> task = Origin();
            task.Wait();
            // var err = task.Result.status.StatusCode != StatusCode.OK ? new RpcException(task.Result.status) : null;
            // return wf.StepResultFromGrpc(task.Result.response, err);

            return Task.FromResult(new StepResult()
            {
                Error = null,
                Data = "my result"u8.ToArray(),
                Status = DtmCommon.Constant.StatusSucceed,
            });
        }).GetAwaiter().GetResult();

        var ex = wf.StepResultToGrpc(sr, null);
        if (ex != null)
        {
            throw ex;
        }

        return base.AsyncUnaryCall(request, context, continuation);
    }
}


public class Dtmgimp
{
// // TransInfo2Ctx add trans info to grpc context
// func TransInfo2Ctx(ctx context.Context, gid, transType, branchID, op, dtm string) context.Context {
//     nctx := ctx
//     if ctx == nil {
//         nctx = context.Background()
//     }
//     return metadata.AppendToOutgoingContext(
//         nctx,
//         dtmpre+"gid", gid,
//         dtmpre+"trans_type", transType,
//         dtmpre+"branch_id", branchID,
//         dtmpre+"op", op,
//         dtmpre+"dtm", dtm,
//     )
// }
    public static ClientInterceptorContext<TRequest, TResponse> TransInfo2Ctx<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> ctx,
        string gid,
        string transType,
        string branchID,
        string op,
        string dtm) where TRequest : class where TResponse : class
    {
        // 创建一个新的元数据对象
        var headers = new Metadata();
        // 添加自定义元数据
        const string dtmpre = "dtm-";
        headers.Add(dtmpre + "gid", gid);
        headers.Add(dtmpre + "trans_type", transType);
        headers.Add(dtmpre + "branch_id", branchID);
        headers.Add(dtmpre + "op", op);
        headers.Add(dtmpre + "dtm", dtm);
        // 修改上下文的元数据
        var nctx = new ClientInterceptorContext<TRequest, TResponse>(
            ctx.Method,
            ctx.Host,
            new CallOptions(headers: headers, deadline: ctx.Options.Deadline, cancellationToken: ctx.Options.CancellationToken));

        return nctx;
    }
}