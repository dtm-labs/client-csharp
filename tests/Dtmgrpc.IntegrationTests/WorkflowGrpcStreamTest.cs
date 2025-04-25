using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using busi;
using Dtmcli;
using DtmCommon;
using Dtmworkflow;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Dtmgrpc.IntegrationTests
{
    public class WorkflowGrpcStreamTest
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public WorkflowGrpcStreamTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task Execute_StreamGrpcTccAndDo_TryConfirm()
        {
            var provider = ITTestHelper.AddDtmGrpc();
            var workflowFactory = provider.GetRequiredService<IWorkflowFactory>();
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            WorkflowGlobalTransaction workflowGlobalTransaction = new WorkflowGlobalTransaction(workflowFactory, loggerFactory);

            string wfName1 = $"{nameof(this.Execute_StreamGrpcTccAndDo_TryConfirm)}-{Guid.NewGuid().ToString("D")[..8]}";
            AsyncDuplexStreamingCall<StreamRequest, StreamReply> call = null;
            MyGrpcProcesser myGrpcProcesser = null;

            workflowGlobalTransaction.Register(wfName1, async (workflow, data) =>
            {
                BusiReq busiRequest = JsonConvert.DeserializeObject<BusiReq>(Encoding.UTF8.GetString(data));

                Busi.BusiClient busiClient = null;

                // 1. grpc1 TCC
                Workflow wf = workflow.NewBranch()
                    .OnCommit(async (barrier) => // confirm
                    {
                        await call.RequestStream.WriteAsync(new StreamRequest()
                        {
                            OperateType = OperateType.Confirm,
                            BusiRequest = busiRequest,
                        });
                        // wait Confirm
                        var result = await myGrpcProcesser.GetResult(OperateType.Confirm);
                        Assert.Equal(StatusCode.OK, result.StatusCode);
                    })
                    .OnRollback(async (barrier) => // cancel
                    {
                        await call.RequestStream.WriteAsync(new StreamRequest()
                        {
                            OperateType = OperateType.Confirm,
                            BusiRequest = busiRequest,
                        });
                        // wait Confirm
                        var result = await myGrpcProcesser.GetResult(OperateType.Cancel);
                        Assert.Equal(StatusCode.OK, result.StatusCode);
                    });

                busiClient = GetBusiClientWithWf(wf, provider);
                call = busiClient.StreamTransOutTcc();
                myGrpcProcesser = new MyGrpcProcesser(call, _testOutputHelper);
                Task readTask = myGrpcProcesser.HandleResponse();

                using var call2 = call;

                // try
                await call.RequestStream.WriteAsync(new StreamRequest()
                {
                    OperateType = OperateType.Try,
                    BusiRequest = busiRequest,
                });
                // wait try
                var result = await myGrpcProcesser.GetResult(OperateType.Try);
                Assert.Equal(StatusCode.OK, result.StatusCode);

                // 2. local， 可以是SAG, 因为排在最后，不必写反向的回滚
                (byte[] doResult, Exception ex) = await workflow.NewBranch()
                    // .OnRollback(async (barrier) => // 反向 rollback
                    // {
                    //     _testOutputHelper.WriteLine("1. local rollback");
                    // })
                    .Do(async (barrier) => { return ("my result"u8.ToArray(), null); }); // 正向
                if (ex != null)
                    throw ex;

                await call.RequestStream.CompleteAsync();
                await readTask;

                return await Task.FromResult("my result"u8.ToArray());
            });

            string gid = wfName1 + Guid.NewGuid().ToString()[..8];
            var req = ITTestHelper.GenBusiReq(false, false);

            DtmClient dtmClient = new DtmClient(provider.GetRequiredService<IHttpClientFactory>(), provider.GetRequiredService<IOptions<DtmOptions>>());

            // first
            byte[] result = await workflowGlobalTransaction.Execute(wfName1, gid, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)));

            Assert.Equal("my result", Encoding.UTF8.GetString(result));
            TransGlobal trans = await dtmClient.Query(gid, CancellationToken.None);
            

            // BranchID	Op	Status	
            // 01	action	succeed			
            // 02	action	succeed			
            // 01	commit	succeed
            Assert.Equal("succeed", trans.Transaction.Status);
            Assert.Equal(3, trans.Branches.Count);
            Assert.Equal("succeed", trans.Branches[0].Status);
            Assert.Equal("action", trans.Branches[0].Op);
            Assert.Equal("succeed", trans.Branches[1].Status);
            Assert.Equal("action", trans.Branches[1].Op);
            Assert.Equal("succeed", trans.Branches[2].Status);
            Assert.Equal("commit", trans.Branches[2].Op);

            // same gid again
            result = await workflowGlobalTransaction.Execute(wfName1, gid, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)));
            Assert.Equal("my result", Encoding.UTF8.GetString(result));
            trans = await dtmClient.Query(gid, CancellationToken.None);
            
            Assert.Equal("succeed", trans.Transaction.Status);
            Assert.Equal(3, trans.Branches.Count);
            Assert.Equal("succeed", trans.Branches[0].Status);
            Assert.Equal("action", trans.Branches[0].Op);
            Assert.Equal("succeed", trans.Branches[1].Status);
            Assert.Equal("action", trans.Branches[1].Op);
            Assert.Equal("succeed", trans.Branches[2].Status);
            Assert.Equal("commit", trans.Branches[2].Op);
        }

        // [Fact]
        // public async Task Execute_StreamGrpcTccAndDo_TryCancel()
        // {
        //     var provider = ITTestHelper.AddDtmGrpc();
        //     var workflowFactory = provider.GetRequiredService<IWorkflowFactory>();
        //     var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        //     WorkflowGlobalTransaction workflowGlobalTransaction = new WorkflowGlobalTransaction(workflowFactory, loggerFactory);
        //
        //     string wfName1 = $"{nameof(this.Execute_StreamGrpcTccAndDo_TryCancel)}-{Guid.NewGuid().ToString("D")[..8]}";
        //     AsyncDuplexStreamingCall<StreamRequest, StreamReply> call = null;
        //     workflowGlobalTransaction.Register(wfName1, async (workflow, data) =>
        //     {
        //         BusiReq busiRequest = JsonConvert.DeserializeObject<BusiReq>(Encoding.UTF8.GetString(data));
        //
        //         Busi.BusiClient busiClient = null;
        //
        //         ConcurrentDictionary<OperateType, Grpc.Core.Status> progress = new ConcurrentDictionary<OperateType, Grpc.Core.Status>();
        //
        //         // 1. grpc1 TCC
        //         Workflow wf = workflow.NewBranch()
        //             .OnCommit(async (barrier) => // confirm
        //             {
        //                 await call.RequestStream.WriteAsync(new StreamRequest()
        //                 {
        //                     OperateType = OperateType.Confirm,
        //                     BusiRequest = busiRequest,
        //                 });
        //                 // wait Confirm
        //                 while (!progress.ContainsKey(OperateType.Confirm))
        //                     Thread.Sleep(1000);
        //                 Assert.Equal(StatusCode.OK, progress[OperateType.Try].StatusCode);
        //             })
        //             .OnRollback(async (barrier) => // cancel
        //             {
        //                 await call.RequestStream.WriteAsync(new StreamRequest()
        //                 {
        //                     OperateType = OperateType.Confirm,
        //                     BusiRequest = busiRequest,
        //                 });
        //                 // wait Confirm
        //                 while (!progress.ContainsKey(OperateType.Confirm))
        //                     Thread.Sleep(1000);
        //                 Assert.Equal(StatusCode.OK, progress[OperateType.Try].StatusCode);
        //             });
        //         busiClient = GetBusiClientWithWf(wf, provider);
        //         call = busiClient.StreamTransOutTcc();
        //         using var call2 = call;
        //
        //         // try
        //         await call.RequestStream.WriteAsync(new StreamRequest()
        //         {
        //             OperateType = OperateType.Try,
        //             BusiRequest = busiRequest,
        //         });
        //         // wait try
        //         while (!progress.ContainsKey(OperateType.Try))
        //             Thread.Sleep(1000);
        //         Assert.Equal(StatusCode.OK, progress[OperateType.Try].StatusCode);
        //
        //         // 2. local， 可以是SAG, 因为排在最后，不必写反向的回滚
        //         (byte[] doResult, Exception ex) = await workflow.NewBranch()
        //             // .OnRollback(async (barrier) => // 反向 rollback
        //             // {
        //             //     _testOutputHelper.WriteLine("1. local rollback");
        //             // })
        //             .Do(async (barrier) =>
        //             {
        //                 // throw new DtmFailureException("db do failed"); // can't throw 
        //                 var ex = new DtmFailureException("db do failed");
        //                 return ("my result"u8.ToArray(), ex);
        //             }); // 正向
        //         if (ex != null)
        //             throw ex;
        //
        //         return await Task.FromResult("my result"u8.ToArray());
        //     });
        //
        //     string gid = wfName1 + Guid.NewGuid().ToString()[..8];
        //     var req = ITTestHelper.GenBusiReq(false, false);
        //
        //     DtmClient dtmClient = new DtmClient(provider.GetRequiredService<IHttpClientFactory>(), provider.GetRequiredService<IOptions<DtmOptions>>());
        //     TransGlobal trans;
        //
        //     // first
        //     byte[] result = await workflowGlobalTransaction.Execute(wfName1, gid, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)));
        //     await readTask;
        //     Assert.Null(result);
        //     trans = await dtmClient.Query(gid, CancellationToken.None);
        //     // BranchID	Op	Status	
        //     // 01	action	succeed			
        //     // 02	action	succeed			
        //     // 01	rollback	succeed
        //     Assert.Equal("failed", trans.Transaction.Status);
        //     Assert.Equal(3, trans.Branches.Count);
        //     Assert.Equal("succeed", trans.Branches[0].Status);
        //     Assert.Equal("action", trans.Branches[0].Op);
        //     Assert.Equal("succeed", trans.Branches[1].Status);
        //     Assert.Equal("action", trans.Branches[1].Op);
        //     Assert.Equal("succeed", trans.Branches[2].Status);
        //     Assert.Equal("rollback", trans.Branches[2].Op);
        //
        //     // same gid again
        //     Assert.ThrowsAsync<DtmCommon.DtmFailureException>(async () =>
        //     {
        //         var result = await workflowGlobalTransaction.Execute(wfName1, gid, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)));
        //         // DtmCommon.DtmFailureException
        //         //     db do failed
        //         //     at Dtmworkflow.Workflow.Process(WfFunc2 handler, Byte[] data) in src/Dtmworkflow/Workflow.Imp.cs
        //         // at Dtmworkflow.WorkflowGlobalTransaction.Execute(String name, String gid, Byte[] data, Boolean isHttp) in src/Dtmworkflow/WorkflowGlobalTransaction.cs
        //         // at Dtmgrpc.IntegrationTests.WorkflowGrpcTest.Execute_GrpcTccAndDo_Should_DoFailed() in tests/Dtmgrpc.IntegrationTests/WorkflowGrpcTest.cs
        //     });
        // }
        //
        //
        // [Fact]
        // public async Task Execute_StreamGrpcTccAndDo_TryFailed()
        // {
        //     var provider = ITTestHelper.AddDtmGrpc();
        //     var workflowFactory = provider.GetRequiredService<IWorkflowFactory>();
        //     var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        //     WorkflowGlobalTransaction workflowGlobalTransaction = new WorkflowGlobalTransaction(workflowFactory, loggerFactory);
        //
        //     string wfName1 = $"{nameof(this.Execute_StreamGrpcTccAndDo_TryFailed)}-{Guid.NewGuid().ToString("D")[..8]}";
        //     Task readTask = null;
        //     AsyncDuplexStreamingCall<StreamRequest, StreamReply> call = null;
        //     workflowGlobalTransaction.Register(wfName1, async (workflow, data) =>
        //     {
        //         BusiReq busiRequest = JsonConvert.DeserializeObject<BusiReq>(Encoding.UTF8.GetString(data));
        //
        //         Busi.BusiClient busiClient = null;
        //
        //         ConcurrentDictionary<OperateType, Grpc.Core.Status> progress = new ConcurrentDictionary<OperateType, Grpc.Core.Status>();
        //
        //         // 1. grpc1 TCC
        //         Workflow wf = workflow.NewBranch()
        //             .OnCommit(async (barrier) => // confirm
        //             {
        //                 await call.RequestStream.WriteAsync(new StreamRequest()
        //                 {
        //                     OperateType = OperateType.Confirm,
        //                     BusiRequest = busiRequest,
        //                 });
        //                 // wait Confirm
        //                 while (!progress.ContainsKey(OperateType.Confirm))
        //                     Thread.Sleep(1000);
        //                 Assert.Equal(StatusCode.OK, progress[OperateType.Try].StatusCode);
        //             })
        //             .OnRollback(async (barrier) => // cancel
        //             {
        //                 await call.RequestStream.WriteAsync(new StreamRequest()
        //                 {
        //                     OperateType = OperateType.Confirm,
        //                     BusiRequest = busiRequest,
        //                 });
        //                 // wait Confirm
        //                 while (!progress.ContainsKey(OperateType.Confirm))
        //                     Thread.Sleep(1000);
        //                 Assert.Equal(StatusCode.OK, progress[OperateType.Try].StatusCode);
        //             });
        //         busiClient = GetBusiClientWithWf(wf, provider);
        //         call = busiClient.StreamTransOutTcc();
        //         using var call2 = call;
        //         readTask = Task.Run(async () =>
        //         {
        //             try
        //             {
        //                 await foreach (var response in call.ResponseStream.ReadAllAsync())
        //                 {
        //                     _testOutputHelper.WriteLine($"{response.OperateType}: {response.Message}");
        //                     progress[response.OperateType] = new Status(StatusCode.OK, "");
        //                 }
        //             }
        //             catch (RpcException ex)
        //             {
        //                 _testOutputHelper.WriteLine($"Exception caught: {ex.Status.StatusCode} - {ex.Status.Detail}");
        //                 progress[OperateType.Try] = ex.Status; // how assess response.OperateType
        //             }
        //             catch (Exception ex)
        //             {
        //                 _testOutputHelper.WriteLine($"Exception caught: {ex}");
        //                 throw;
        //             }
        //         });
        //
        //         // try failed
        //         await call.RequestStream.WriteAsync(new StreamRequest()
        //         {
        //             OperateType = OperateType.Try,
        //             BusiRequest = busiRequest,
        //         });
        //         // wait try
        //         while (!progress.ContainsKey(OperateType.Try))
        //             Thread.Sleep(1000);
        //         Assert.Equal(StatusCode.Aborted, progress[OperateType.Try].StatusCode);
        //         Assert.Equal("FAILURE", progress[OperateType.Try].Detail);
        //         throw new DtmFailureException($"sub trans1 try failed(grpc): {progress[OperateType.Try].Detail}");
        //         // throw new Exception($"sub trans1 try failed(grpc): {progress[OperateType.Try].Detail}");
        //     });
        //
        //     string gid = wfName1 + Guid.NewGuid().ToString()[..8];
        //     var req = ITTestHelper.GenBusiReq(outFailed: true, false);
        //
        //     DtmClient dtmClient = new DtmClient(provider.GetRequiredService<IHttpClientFactory>(), provider.GetRequiredService<IOptions<DtmOptions>>());
        //     TransGlobal trans;
        //
        //     // first
        //     // await Assert.ThrowsAsync<DtmCommon.DtmFailureException>(async () =>
        //     {
        //         byte[] result = await workflowGlobalTransaction.Execute(wfName1, gid, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)));
        //     }
        //     // );
        //     await readTask;
        //     trans = await dtmClient.Query(gid, CancellationToken.None);
        //     Assert.Equal("failed", trans.Transaction.Status);
        //     Assert.Equal(0, trans.Branches.Count);
        //
        //
        //     // same gid again
        //     await Assert.ThrowsAsync<DtmCommon.DtmFailureException>(async () =>
        //         {
        //             var result = await workflowGlobalTransaction.Execute(wfName1, gid, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)));
        //             // DtmCommon.DtmFailureException
        //             //     sub trans1 try failed(grpc): FAILURE
        //             //     at Dtmworkflow.Workflow.Process(WfFunc2 handler, Byte[] data) in src/Dtmworkflow/Workflow.Imp.cs
        //             // at Dtmworkflow.WorkflowGlobalTransaction.Execute(String name, String gid, Byte[] data, Boolean isHttp) in src/Dtmworkflow/WorkflowGlobalTransaction.cs
        //             // at Dtmgrpc.IntegrationTests.WorkflowGrpcTest.Execute_GrpcTccAndDo_Should_DoFailed() in tests/Dtmgrpc.IntegrationTests/WorkflowGrpcTest.cs
        //         }
        //     );
        // }

        private static Busi.BusiClient GetBusiClientWithWf(Workflow wf, ServiceProvider provider)
        {
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            var channel = GrpcChannel.ForAddress(ITTestHelper.BuisgRPCUrlWithProtocol);
            var logger = loggerFactory.CreateLogger<WorkflowGrpcInterceptor>();
            var interceptor = new WorkflowGrpcInterceptor(wf, logger); // inject client interceptor, and workflow instance
            var callInvoker = channel.Intercept(interceptor);
            Busi.BusiClient busiClient = new Busi.BusiClient(callInvoker);
            return busiClient;
        }
    }
}