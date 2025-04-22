using System;
using System.Collections.Concurrent;
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

public class BusiApiServiceTest(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task StreamTransOutTcc_Try_Confirm()
    {
        var provider = ITTestHelper.AddDtmGrpc();
        Busi.BusiClient busiClient = GetBusiClientWithWf(null, provider);

        ConcurrentDictionary<OperateType, Grpc.Core.Status> progess = new ConcurrentDictionary<OperateType, Grpc.Core.Status>();

        using var call = busiClient.StreamTransOutTcc();
        testOutputHelper.WriteLine("Starting background task to receive messages");
        Task readTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var response in call.ResponseStream.ReadAllAsync())
                {
                    testOutputHelper.WriteLine($"{response.OperateType}: {response.Message}");
                    progess[response.OperateType] = new Status(StatusCode.OK, "");
                }
            }
            catch (RpcException ex)
            {
                testOutputHelper.WriteLine($"Exception caught: {ex.Status.StatusCode} - {ex.Status.Detail}");
                progess[OperateType.Try] = ex.Status; // how assess response.OperateType
            }
            catch (Exception ex)
            {
                throw;
            }
        });

        testOutputHelper.WriteLine("Starting to send messages");
        BusiReq busiRequest = ITTestHelper.GenBusiReq(false, false);

        // try
        await call.RequestStream.WriteAsync(new StreamRequest()
        {
            OperateType = OperateType.Try,
            BusiRequest = busiRequest,
        });
        // wait try
        while (!progess.ContainsKey(OperateType.Try))
            Thread.Sleep(1000);
        Assert.Equal(StatusCode.OK, progess[OperateType.Try].StatusCode);

        // confirm
        await call.RequestStream.WriteAsync(new StreamRequest()
        {
            OperateType = OperateType.Confirm,
            BusiRequest = busiRequest,
        });
        // wait Confirm
        while (!progess.ContainsKey(OperateType.Confirm))
            Thread.Sleep(1000);
        Assert.Equal(StatusCode.OK, progess[OperateType.Try].StatusCode);

        await call.RequestStream.CompleteAsync();
        await readTask;
    }

    [Fact]
    public async Task StreamTransOutTcc_Try_Failed()
    {
        var provider = ITTestHelper.AddDtmGrpc();
        Busi.BusiClient busiClient = GetBusiClientWithWf(null, provider);

        ConcurrentDictionary<OperateType, Grpc.Core.Status> progess = new ConcurrentDictionary<OperateType, Grpc.Core.Status>();

        using AsyncDuplexStreamingCall<StreamRequest, StreamReply> call = busiClient.StreamTransOutTcc();
        testOutputHelper.WriteLine("Starting background task to receive messages");
        Task readTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var response in call.ResponseStream.ReadAllAsync())
                {
                    testOutputHelper.WriteLine($"{response.OperateType}: {response.Message}");
                    progess[response.OperateType] = new Status(StatusCode.OK, "");
                }
            }
            catch (RpcException ex)
            {
                testOutputHelper.WriteLine($"Exception caught: {ex.Status.StatusCode} - {ex.Status.Detail}");
                progess[OperateType.Try] = ex.Status; // how assess response.OperateType
            }
        });

        testOutputHelper.WriteLine("Starting to send messages");
        BusiReq busiRequest = ITTestHelper.GenBusiReq(true, false);

        // try
        await call.RequestStream.WriteAsync(new StreamRequest()
        {
            OperateType = OperateType.Try,
            BusiRequest = busiRequest,
        });
        // wait try
        while (!progess.ContainsKey(OperateType.Try))
            Thread.Sleep(1000);
        Assert.Equal(StatusCode.Aborted, progess[OperateType.Try].StatusCode);
        Assert.Equal("FAILURE", progess[OperateType.Try].Detail);

        await call.RequestStream.CompleteAsync();
        await readTask;
    }


    [Description("try-cancel")]
    [Fact]
    public async Task StreamTransOutTcc_Try_Cancel()
    {
        var provider = ITTestHelper.AddDtmGrpc();
        Busi.BusiClient busiClient = GetBusiClientWithWf(null, provider);

        ConcurrentDictionary<OperateType, Grpc.Core.Status> progess = new ConcurrentDictionary<OperateType, Grpc.Core.Status>();

        using AsyncDuplexStreamingCall<StreamRequest, StreamReply> call = busiClient.StreamTransOutTcc();
        testOutputHelper.WriteLine("Starting background task to receive messages");
        Task readTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var response in call.ResponseStream.ReadAllAsync())
                {
                    testOutputHelper.WriteLine($"{response.OperateType}: {response.Message}");
                    progess[response.OperateType] = new Status(StatusCode.OK, "");
                }
            }
            catch (RpcException ex)
            {
                testOutputHelper.WriteLine($"Exception caught: {ex.Status.StatusCode} - {ex.Status.Detail}");
                progess[OperateType.Try] = ex.Status; // how assess response.OperateType
            }
        });

        testOutputHelper.WriteLine("Starting to send messages");
        BusiReq busiRequest = ITTestHelper.GenBusiReq(false, false);

        // try
        await call.RequestStream.WriteAsync(new StreamRequest()
        {
            OperateType = OperateType.Try,
            BusiRequest = busiRequest,
        });
        // wait try
        while (!progess.ContainsKey(OperateType.Try))
            Thread.Sleep(1000);
        Assert.Equal(StatusCode.OK, progess[OperateType.Try].StatusCode);

        // cancel
        await call.RequestStream.WriteAsync(new StreamRequest()
        {
            OperateType = OperateType.Cancel,
            BusiRequest = busiRequest,
        });
        // wait cancel
        while (!progess.ContainsKey(OperateType.Cancel))
            Thread.Sleep(1000);
        Assert.Equal(StatusCode.OK, progess[OperateType.Cancel].StatusCode);

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