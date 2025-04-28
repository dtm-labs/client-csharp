using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace Dtmworkflow;

// [gRPC interceptors on .NET | Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/grpc/interceptors?view=aspnetcore-9.0)
public class WorkflowGrpcInterceptor(Workflow wf, ILogger<WorkflowGrpcInterceptor> logger) : Interceptor
{
    public WorkflowGrpcInterceptor(Workflow wf) : this(wf, null)
    {
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        logger?.LogDebug($"grpc client calling: {context.Host}{context.Method.FullName}");

        if (wf == null)
        {
            return base.AsyncUnaryCall(request, context, continuation);
        }

        async Task<(AsyncUnaryCall<TResponse>, TResponse, Status)> Origin()
        {
            var newContext = Dtmgimp.TransInfo2Ctx(context, wf.TransBase.Gid, wf.TransBase.TransType, wf.WorkflowImp.CurrentBranch, wf.WorkflowImp.CurrentOp, wf.TransBase.Dtm);

            var call = continuation(request, newContext);
            TResponse response;
            try
            {
                response = await call.ResponseAsync;
            }
            catch (Exception e)
            {
                logger?.LogDebug($"grpc client: {context.Host}{context.Method.FullName} ex: {e}");
                response = null;
            }

            Status status = call.GetStatus();
            return (
                new AsyncUnaryCall<TResponse>(
                    call.ResponseAsync,
                    call.ResponseHeadersAsync,
                    call.GetStatus,
                    call.GetTrailers,
                    call.Dispose),
                response,
                status
            );
        }

        // intercept phase1 only. CallPhase2 comes with RecordedDo
        if (wf.WorkflowImp.CurrentOp != DtmCommon.Constant.OpAction)
        {
            var (newCall, _, _) = Origin().GetAwaiter().GetResult();
            return newCall;
        }

        AsyncUnaryCall<TResponse> call = null;
        StepResult sr = wf.RecordedDo(bb =>
        {
            (call, TResponse data, Status status) = Origin().GetAwaiter().GetResult();
            RpcException err = status.StatusCode != StatusCode.OK ? new RpcException(status) : null;
            return Task.FromResult(wf.StepResultFromGrpc(data as IMessage, err));
        }).GetAwaiter().GetResult();
        Exception exception = wf.StepResultToGrpc(sr, null);

        return call;
    }

    private class Dtmgimp
    {
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
            // 包含原始的元数据
            if (ctx.Options.Headers != null)
            {
                foreach (Metadata.Entry entity in ctx.Options.Headers)
                {
                    headers.Add(entity.Key, entity.Value);
                }
            }

            // 添加自定义元数据
            const string dtmpre = "dtm-";
            headers.Add(dtmpre + "gid", gid);
            headers.Add(dtmpre + "trans_type", transType);
            headers.Add(dtmpre + "branch_id", branchID);
            headers.Add(dtmpre + "op", op);
            headers.Add(dtmpre + "dtm", dtm);

            // 增加唯一标识, 用于Response的配对
            headers.Add("sub-call-id", $"{op}-{Guid.NewGuid()}");

            // 修改上下文的元数据
            var nctx = new ClientInterceptorContext<TRequest, TResponse>(
                ctx.Method,
                ctx.Host,
                new CallOptions(headers: headers, deadline: ctx.Options.Deadline, cancellationToken: ctx.Options.CancellationToken));

            return nctx;
        }
    }
}