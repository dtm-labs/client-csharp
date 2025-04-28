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

        var headers = new Metadata();
        headers.Add("sub-call-id", "init id"); // 主调用里放上, 用于跟后续response的配对
        
        using AsyncDuplexStreamingCall<StreamRequest, StreamReply> call = busiClient.StreamTransOutTcc(headers);
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