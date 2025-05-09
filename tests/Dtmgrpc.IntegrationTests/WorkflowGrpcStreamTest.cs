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
            Task readTask = null;
            workflowGlobalTransaction.Register(wfName1, async (workflow, data) =>
            {
                BusiReq busiRequest = JsonConvert.DeserializeObject<BusiReq>(Encoding.UTF8.GetString(data));

                // 1. grpc1 TCC
                workflow.NewBranch()
                    .OnCommit(async (barrier) => // confirm
                    {
                        await call.RequestStream.WriteAsync(new StreamRequest()
                        {
                            OperateType = OperateType.Confirm,
                            DtmBranchTransInfo = this.CurrentBranchTransInfo(workflow),
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
                            DtmBranchTransInfo = this.CurrentBranchTransInfo(workflow),
                            BusiRequest = busiRequest,
                        });
                        // wait Confirm
                        var result = await myGrpcProcesser.GetResult(OperateType.Cancel);
                        Assert.Equal(StatusCode.OK, result.StatusCode);
                    });
                Busi.BusiClient busiClient = GetBusiClient();
                call = busiClient.StreamTransOutTcc();
                myGrpcProcesser = new MyGrpcProcesser(call, _testOutputHelper);
                readTask = myGrpcProcesser.HandleResponse();
                // try
                var (_, stepEx) = await workflow.Do(async (barrier) =>
                {
                    await call.RequestStream.WriteAsync(new StreamRequest()
                    {
                        OperateType = OperateType.Try,
                        DtmBranchTransInfo = this.CurrentBranchTransInfo(workflow),
                        BusiRequest = busiRequest,
                    });
                    // wait try
                    var result = await myGrpcProcesser.GetResult(OperateType.Try);
                    Assert.Equal(StatusCode.OK, result.StatusCode);
                    return (""u8.ToArray(), null);
                });
                if (stepEx != null)
                    throw stepEx;

                // 2. local， maybe SAG, at the end, no need to write the reverse rollback.
                (_, stepEx) = await workflow.NewBranch()
                    // .OnRollback(async (barrier) =>
                    // {
                    //     _testOutputHelper.WriteLine("1. local rollback");
                    // })
                    .Do(async (barrier) =>
                    {
                        _testOutputHelper.WriteLine("2. local do");
                        return ("my result"u8.ToArray(), null);
                    });
                if (stepEx != null)
                    throw stepEx;

                return await Task.FromResult("my result"u8.ToArray());
            });

            string gid = wfName1 + Guid.NewGuid().ToString()[..8];
            var req = ITTestHelper.GenBusiReq(false, false);

            DtmClient dtmClient = new DtmClient(provider.GetRequiredService<IHttpClientFactory>(), provider.GetRequiredService<IOptions<DtmOptions>>());

            using var call2 = call;
            // first
            byte[] result = await workflowGlobalTransaction.Execute(wfName1, gid, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)));

            await call.RequestStream.CompleteAsync();
            await readTask;

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


        [Fact]
        public async Task Execute_StreamGrpcTccAndDo_TryCancel()
        {
            var provider = ITTestHelper.AddDtmGrpc();
            var workflowFactory = provider.GetRequiredService<IWorkflowFactory>();
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            WorkflowGlobalTransaction workflowGlobalTransaction = new WorkflowGlobalTransaction(workflowFactory, loggerFactory);

            string wfName1 = $"{nameof(this.Execute_StreamGrpcTccAndDo_TryConfirm)}-{Guid.NewGuid().ToString("D")[..8]}";
            AsyncDuplexStreamingCall<StreamRequest, StreamReply> call = null;
            MyGrpcProcesser myGrpcProcesser = null;
            Task readTask = null;
            workflowGlobalTransaction.Register(wfName1, async (workflow, data) =>
            {
                BusiReq busiRequest = JsonConvert.DeserializeObject<BusiReq>(Encoding.UTF8.GetString(data));

                // 1. grpc1 TCC
                workflow.NewBranch()
                    .OnCommit(async (barrier) => // confirm
                    {
                        await call.RequestStream.WriteAsync(new StreamRequest()
                        {
                            OperateType = OperateType.Confirm,
                            DtmBranchTransInfo = this.CurrentBranchTransInfo(workflow),
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
                            OperateType = OperateType.Cancel,
                            DtmBranchTransInfo = this.CurrentBranchTransInfo(workflow),
                            BusiRequest = busiRequest,
                        });
                        // wait Confirm
                        var result = await myGrpcProcesser.GetResult(OperateType.Cancel);
                        Assert.Equal(StatusCode.OK, result.StatusCode);
                    });
                Busi.BusiClient busiClient = GetBusiClient();
                call = busiClient.StreamTransOutTcc();
                myGrpcProcesser = new MyGrpcProcesser(call, _testOutputHelper);
                readTask = myGrpcProcesser.HandleResponse();
                // try
                var (_, stepEx) = await workflow.Do(async (barrier) =>
                {
                    await call.RequestStream.WriteAsync(new StreamRequest()
                    {
                        OperateType = OperateType.Try,
                        DtmBranchTransInfo = this.CurrentBranchTransInfo(workflow),
                        BusiRequest = busiRequest,
                    });
                    // wait try
                    var result = await myGrpcProcesser.GetResult(OperateType.Try);
                    Assert.Equal(StatusCode.OK, result.StatusCode);
                    return (""u8.ToArray(), null);
                });
                if (stepEx != null)
                    throw stepEx;

                // 2. local， maybe SAG, at the end, no need to write the reverse rollback.
                (_, stepEx) = await workflow.NewBranch()
                    // .OnRollback(async (barrier) =>
                    // {
                    //     _testOutputHelper.WriteLine("1. local rollback");
                    // })
                    .Do(async (barrier) =>
                    {
                        _testOutputHelper.WriteLine("2. db do with throw failed");
                        // throw new DtmFailureException("db do failed"); // can't throw 
                        var ex = new DtmFailureException("db do failed");
                        return ("my result"u8.ToArray(), ex);
                    });
                if (stepEx != null)
                    throw stepEx;

                return await Task.FromResult("my result"u8.ToArray());
            });

            string gid = wfName1 + Guid.NewGuid().ToString()[..8];
            var req = ITTestHelper.GenBusiReq(false, false);

            DtmClient dtmClient = new DtmClient(provider.GetRequiredService<IHttpClientFactory>(), provider.GetRequiredService<IOptions<DtmOptions>>());

            using var call2 = call;
            // first

            // same gid again
            await Assert.ThrowsAsync<DtmCommon.DtmFailureException>(async () =>
            {
                byte[] result = await workflowGlobalTransaction.Execute(wfName1, gid, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)));
            });

            await call.RequestStream.CompleteAsync();
            await readTask;

            TransGlobal trans = await dtmClient.Query(gid, CancellationToken.None);

            // BranchID	Op	Status	CreateTime	UpdateTime	Url
            // 01	action	succeed			
            // 02	action	failed			
            // 01	rollback	succeed
            Assert.Equal("failed", trans.Transaction.Status);
            Assert.Equal(3, trans.Branches.Count);
            Assert.Equal("succeed", trans.Branches[0].Status);
            Assert.Equal("action", trans.Branches[0].Op);
            Assert.Equal("failed", trans.Branches[1].Status);
            Assert.Equal("action", trans.Branches[1].Op);
            Assert.Equal("succeed", trans.Branches[2].Status);
            Assert.Equal("rollback", trans.Branches[2].Op);

            // same gid again
            Assert.ThrowsAsync<DtmCommon.DtmFailureException>(async () =>
            {
                var result = await workflowGlobalTransaction.Execute(wfName1, gid, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)));
                // DtmCommon.DtmFailureException
                //     db do failed
                //     at Dtmworkflow.Workflow.Process(WfFunc2 handler, Byte[] data) in src/Dtmworkflow/Workflow.Imp.cs
                // at Dtmworkflow.WorkflowGlobalTransaction.Execute(String name, String gid, Byte[] data, Boolean isHttp) in src/Dtmworkflow/WorkflowGlobalTransaction.cs
                // at Dtmgrpc.IntegrationTests.WorkflowGrpcTest.Execute_GrpcTccAndDo_Should_DoFailed() in tests/Dtmgrpc.IntegrationTests/WorkflowGrpcTest.cs
            });
            trans = await dtmClient.Query(gid, CancellationToken.None);
            Assert.Equal("failed", trans.Transaction.Status);
            Assert.Equal(3, trans.Branches.Count);
            Assert.Equal("succeed", trans.Branches[0].Status);
            Assert.Equal("action", trans.Branches[0].Op);
            Assert.Equal("failed", trans.Branches[1].Status);
            Assert.Equal("action", trans.Branches[1].Op);
            Assert.Equal("succeed", trans.Branches[2].Status);
            Assert.Equal("rollback", trans.Branches[2].Op);
        }

        [Fact]
        public async Task Execute_StreamGrpcTccAndDo_TryFailed()
        {
            var provider = ITTestHelper.AddDtmGrpc();
            var workflowFactory = provider.GetRequiredService<IWorkflowFactory>();
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            WorkflowGlobalTransaction workflowGlobalTransaction = new WorkflowGlobalTransaction(workflowFactory, loggerFactory);

            string wfName1 = $"{nameof(this.Execute_StreamGrpcTccAndDo_TryConfirm)}-{Guid.NewGuid().ToString("D")[..8]}";
            AsyncDuplexStreamingCall<StreamRequest, StreamReply> call = null;
            MyGrpcProcesser myGrpcProcesser = null;
            Task readTask = null;
            workflowGlobalTransaction.Register(wfName1, async (workflow, data) =>
            {
                BusiReq busiRequest = JsonConvert.DeserializeObject<BusiReq>(Encoding.UTF8.GetString(data));

                // 1. grpc1 TCC
                workflow.NewBranch()
                    .OnCommit(async (barrier) => // confirm
                    {
                        await call.RequestStream.WriteAsync(new StreamRequest()
                        {
                            OperateType = OperateType.Confirm,
                            DtmBranchTransInfo = this.CurrentBranchTransInfo(workflow),
                            BusiRequest = busiRequest,
                        });
                        // wait Confirm
                        var result = await myGrpcProcesser.GetResult(OperateType.Confirm);
                        Assert.Equal(StatusCode.Aborted, result.StatusCode);
                        Assert.Equal("FAILURE", result.Detail);
                    })
                    .OnRollback(async (barrier) => // cancel
                    {
                        await call.RequestStream.WriteAsync(new StreamRequest()
                        {
                            OperateType = OperateType.Confirm,
                            DtmBranchTransInfo = this.CurrentBranchTransInfo(workflow),
                            BusiRequest = busiRequest,
                        });
                        // wait Confirm
                        var result = await myGrpcProcesser.GetResult(OperateType.Cancel);
                        Assert.Equal(StatusCode.OK, result.StatusCode);
                    });
                Busi.BusiClient busiClient = GetBusiClient();
                call = busiClient.StreamTransOutTcc();
                myGrpcProcesser = new MyGrpcProcesser(call, _testOutputHelper);
                readTask = myGrpcProcesser.HandleResponse();
                // try
                var (_, stepEx) = await workflow.Do(async (barrier) =>
                {
                    await call.RequestStream.WriteAsync(new StreamRequest()
                    {
                        OperateType = OperateType.Try,
                        DtmBranchTransInfo = this.CurrentBranchTransInfo(workflow),
                        BusiRequest = busiRequest,
                    });
                    // wait try
                    var result = await myGrpcProcesser.GetResult(OperateType.Try);
                    Assert.Equal(StatusCode.Aborted, result.StatusCode);
                    Assert.Equal("FAILURE", result.Detail);

                    return (""u8.ToArray(), new DtmFailureException("Try grpc error"));
                });
                if (stepEx != null)
                    throw stepEx;

                // 2. local， maybe SAG, at the end, no need to write the reverse rollback.
                (_, stepEx) = await workflow.NewBranch()
                    // .OnRollback(async (barrier) =>
                    // {
                    //     _testOutputHelper.WriteLine("1. local rollback");
                    // })
                    .Do(async (barrier) =>
                    {
                        _testOutputHelper.WriteLine("2. local do");
                        return ("my result"u8.ToArray(), null);
                    });
                if (stepEx != null)
                    throw stepEx;

                return await Task.FromResult("my result"u8.ToArray());
            });

            string gid = wfName1 + Guid.NewGuid().ToString()[..8];
            var req = ITTestHelper.GenBusiReq(true, false);

            DtmClient dtmClient = new DtmClient(provider.GetRequiredService<IHttpClientFactory>(), provider.GetRequiredService<IOptions<DtmOptions>>());

            using var call2 = call;
            // first
            await Assert.ThrowsAsync<DtmCommon.DtmFailureException>(async () =>
                {
                    byte[] result = await workflowGlobalTransaction.Execute(wfName1, gid, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)));
                }
            );
            await call.RequestStream.CompleteAsync();
            await Assert.ThrowsAsync<Grpc.Core.RpcException>(async () => { await readTask; }); // grpc aborted by server try method

            var trans = await dtmClient.Query(gid, CancellationToken.None);
            Assert.Equal("failed", trans.Transaction.Status);
            // BranchID	Op	Status	CreateTime	UpdateTime	Url
            // 01	action	failed
            Assert.Equal(1, trans.Branches.Count);
            Assert.Equal("01", trans.Branches[0].BranchId);
            Assert.Equal("failed", trans.Branches[0].Status);
            Assert.Equal("action", trans.Branches[0].Op);

            // same gid again
            await Assert.ThrowsAsync<DtmCommon.DtmFailureException>(async () =>
            {
                var result = await workflowGlobalTransaction.Execute(wfName1, gid, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)));
            });

            await call.RequestStream.CompleteAsync();
            await Assert.ThrowsAsync<Grpc.Core.RpcException>(async () => { await readTask; }); // grpc aborted by server try method

            trans = await dtmClient.Query(gid, CancellationToken.None);
            Assert.Equal("failed", trans.Transaction.Status);
            // BranchID	Op	Status	CreateTime	UpdateTime	Url
            // 01	action	failed
            Assert.Equal(1, trans.Branches.Count);
            Assert.Equal("01", trans.Branches[0].BranchId);
            Assert.Equal("failed", trans.Branches[0].Status);
            Assert.Equal("action", trans.Branches[0].Op);
        }

        private static Busi.BusiClient GetBusiClient()
        {
            var channel = GrpcChannel.ForAddress(ITTestHelper.BuisgRPCUrlWithProtocol);
            return new Busi.BusiClient(channel);
        }

        private DtmBranchTransInfo CurrentBranchTransInfo(Workflow wf)
        {
            return new DtmBranchTransInfo()
            {
                Gid = wf.TransBase.Gid,
                TransType = wf.TransBase.TransType,
                BranchId = wf.WorkflowImp.CurrentBranch,
                Op = wf.WorkflowImp.CurrentOp,
                Dtm = wf.TransBase.Dtm,
            };
        }
    }
}