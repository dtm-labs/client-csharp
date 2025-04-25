using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using busi;
using Dtmworkflow;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Dtmgrpc.IntegrationTests;

// [Call gRPC services with the .NET client | Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/grpc/client?view=aspnetcore-9.0#bi-directional-streaming-call)

public class MyGrpcProcesser(AsyncDuplexStreamingCall<StreamRequest, StreamReply> call, ITestOutputHelper testOutputHelper)
{
    private TaskCompletionSource _callDisposed = new TaskCompletionSource();
    private readonly ConcurrentDictionary<OperateType, TaskCompletionSource<Status>> progress = new();

    public Task HandleResponse()
    {
        IAsyncEnumerable<StreamReply> asyncEnumerable = call.ResponseStream.ReadAllAsync();
        Task readTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var response in asyncEnumerable)
                {
                    testOutputHelper.WriteLine($"{response.OperateType}: {response.Message}");
                    if (progress.TryGetValue(response.OperateType, out var tcs))
                    {
                        tcs.TrySetResult(new Status(StatusCode.OK, ""));
                    }
                    else
                    {
                        progress[response.OperateType] = new TaskCompletionSource<Status>(new Status(StatusCode.OK, ""));
                    }
                }
            }
            catch (RpcException ex)
            {
                testOutputHelper.WriteLine($"Exception caught: {ex.Status.StatusCode} - {ex.Status.Detail}");
                if (progress.TryGetValue(OperateType.Try, out var tcs))
                {
                    bool _ = tcs.TrySetResult(ex.Status);
                }
                else
                    progress[OperateType.Try] = new TaskCompletionSource<Status>(ex.Status); // TODO 应答对应的哪个请求

                _callDisposed.SetResult();
                throw;
            }
            catch (Exception ex)
            {
                _callDisposed.SetResult();
                throw;
            }
        });
        return readTask;
    }

    public async Task<Status> GetResult(OperateType operateType)
    {
        if (!progress.TryGetValue(operateType, out var tcs))
        {
            tcs = new TaskCompletionSource<Status>();
            progress[operateType] = tcs;
        }

        Task.WaitAny(_callDisposed.Task, tcs.Task);
        return await tcs.Task;
    }
}

public class BusiApiServiceTest(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task StreamTransOutTcc_Try_Confirm()
    {
        var provider = ITTestHelper.AddDtmGrpc();
        Busi.BusiClient busiClient = GetBusiClientWithWf(null, provider);

        using AsyncDuplexStreamingCall<StreamRequest, StreamReply> call = busiClient.StreamTransOutTcc();
        testOutputHelper.WriteLine("Starting background task to receive messages");
        var myGrpcProcesser = new MyGrpcProcesser(call, testOutputHelper);
        var readTask = myGrpcProcesser.HandleResponse();

        testOutputHelper.WriteLine("Starting to send messages");
        BusiReq busiRequest = ITTestHelper.GenBusiReq(false, false);

        // try
        await call.RequestStream.WriteAsync(new StreamRequest()
        {
            OperateType = OperateType.Try,
            BusiRequest = busiRequest,
        });
        Grpc.Core.Status tryStatus = await myGrpcProcesser.GetResult(OperateType.Try);
        Assert.Equal(StatusCode.OK, tryStatus.StatusCode);

        // confirm
        await call.RequestStream.WriteAsync(new StreamRequest()
        {
            OperateType = OperateType.Confirm,
            BusiRequest = busiRequest,
        });
        // wait Confirm
        Grpc.Core.Status confirmStatus = await myGrpcProcesser.GetResult(OperateType.Confirm);
        Assert.Equal(StatusCode.OK, confirmStatus.StatusCode);

        await call.RequestStream.CompleteAsync();
        await readTask;
    }

    [Fact]
    public async Task StreamTransOutTcc_Try_Failed()
    {
        var provider = ITTestHelper.AddDtmGrpc();
        Busi.BusiClient busiClient = GetBusiClientWithWf(null, provider);

        using AsyncDuplexStreamingCall<StreamRequest, StreamReply> call = busiClient.StreamTransOutTcc();
        testOutputHelper.WriteLine("Starting background task to receive messages");
        var myGrpcProcesser = new MyGrpcProcesser(call, testOutputHelper);
        var readTask = myGrpcProcesser.HandleResponse();

        testOutputHelper.WriteLine("Starting to send messages");
        BusiReq busiRequest = ITTestHelper.GenBusiReq(true, false);

        // try
        await call.RequestStream.WriteAsync(new StreamRequest()
        {
            OperateType = OperateType.Try,
            BusiRequest = busiRequest,
        });
        // wait try
        var tryStatus = await myGrpcProcesser.GetResult(OperateType.Try);
        Assert.Equal(StatusCode.Aborted, tryStatus.StatusCode);
        Assert.Equal("FAILURE", tryStatus.Detail);

        await call.RequestStream.CompleteAsync();
        await Assert.ThrowsAsync<Grpc.Core.RpcException>(async () => { await readTask; }); // because try action aborted.
    }


    [Description("try-cancel")]
    [Fact]
    public async Task StreamTransOutTcc_Try_Cancel()
    {
        var provider = ITTestHelper.AddDtmGrpc();
        Busi.BusiClient busiClient = GetBusiClientWithWf(null, provider);

        using AsyncDuplexStreamingCall<StreamRequest, StreamReply> call = busiClient.StreamTransOutTcc();
        testOutputHelper.WriteLine("Starting background task to receive messages");
        var myGrpcProcesser = new MyGrpcProcesser(call, testOutputHelper);
        var readTask = myGrpcProcesser.HandleResponse();

        testOutputHelper.WriteLine("Starting to send messages");
        BusiReq busiRequest = ITTestHelper.GenBusiReq(false, false);

        // try
        await call.RequestStream.WriteAsync(new StreamRequest()
        {
            OperateType = OperateType.Try,
            BusiRequest = busiRequest,
        });
        // wait try
        var tryStatus = await myGrpcProcesser.GetResult(OperateType.Try);
        Assert.Equal(StatusCode.OK, tryStatus.StatusCode);

        // cancel
        await call.RequestStream.WriteAsync(new StreamRequest()
        {
            OperateType = OperateType.Cancel,
            BusiRequest = busiRequest,
        });
        // wait cancel
        var cancelStatus = await myGrpcProcesser.GetResult(OperateType.Cancel);
        Assert.Equal(StatusCode.OK, cancelStatus.StatusCode);

        await call.RequestStream.CompleteAsync();
        await readTask;
    }

    private static Busi.BusiClient GetBusiClientWithWf(Workflow wf, ServiceProvider provider)
    {
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        var channel = GrpcChannel.ForAddress(ITTestHelper.BuisgRPCUrlWithProtocol);
        var logger = loggerFactory.CreateLogger<WorkflowGrpcInterceptor>();

        Busi.BusiClient busiClient;
        if (wf != null)
        {
            var interceptor = new WorkflowGrpcInterceptor(wf, logger); // inject client interceptor, and workflow instance
            var callInvoker = channel.Intercept(interceptor);
            busiClient = new Busi.BusiClient(callInvoker);
        }
        else
        {
            busiClient = new Busi.BusiClient(channel);
        }

        return busiClient;
    }
}