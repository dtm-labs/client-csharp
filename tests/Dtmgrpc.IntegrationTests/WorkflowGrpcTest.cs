using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using busi;
using Dtmcli;
using DtmCommon;
using Dtmworkflow;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Dtmgrpc.IntegrationTests
{
    public class WorkflowGrpcTest
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public WorkflowGrpcTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task Execute_Http_Should_Succeed()
        {
            var provider = ITTestHelper.AddDtmGrpc();
            var workflowFactory = provider.GetRequiredService<IWorkflowFactory>();
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            WorkflowGlobalTransaction workflowGlobalTransaction = new WorkflowGlobalTransaction(workflowFactory, loggerFactory);

            string wfName1 = $"wf-simple-{Guid.NewGuid().ToString("D")[..8]}";
            workflowGlobalTransaction.Register(wfName1, async (workflow, data) => await Task.FromResult("my result"u8.ToArray()));

            string gid = wfName1 + Guid.NewGuid().ToString()[..8];
            var req = ITTestHelper.GenBusiReq(false, false);
            await workflowGlobalTransaction.Execute(wfName1, gid, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)), isHttp: true);

            string status = await ITTestHelper.GetTranStatus(gid);
            Assert.Equal("succeed", status);
        }

        [Fact]
        public async Task Execute_gPRC_Should_Succeed()
        {
            var provider = ITTestHelper.AddDtmGrpc();
            var workflowFactory = provider.GetRequiredService<IWorkflowFactory>();
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            WorkflowGlobalTransaction workflowGlobalTransaction = new WorkflowGlobalTransaction(workflowFactory, loggerFactory);

            string wfName1 = $"wf-simple-{Guid.NewGuid().ToString("D")[..8]}";
            workflowGlobalTransaction.Register(wfName1, async (workflow, data) => await Task.FromResult("fmy result"u8.ToArray()));

            string gid = wfName1 + Guid.NewGuid().ToString()[..8];
            var req = ITTestHelper.GenBusiReq(false, false);
            await workflowGlobalTransaction.Execute(wfName1, gid, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)), isHttp: false);

            string status = await ITTestHelper.GetTranStatus(gid);
            Assert.Equal("succeed", status);
        }

        [Fact]
        public async Task Execute_DoAndHttp_ShouldSuccess()
        {
            var provider = ITTestHelper.AddDtmGrpc();
            var workflowFactory = provider.GetRequiredService<IWorkflowFactory>();
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            WorkflowGlobalTransaction workflowGlobalTransaction = new WorkflowGlobalTransaction(workflowFactory, loggerFactory);

            string wfName1 = $"wf-simple-{Guid.NewGuid().ToString("D")[..8]}";
            workflowGlobalTransaction.Register(wfName1, async (workflow, data) =>
            {
                BusiReq request = JsonConvert.DeserializeObject<BusiReq>(Encoding.UTF8.GetString(data));

                // 1. local
                workflow.NewBranch().OnRollback(async (barrier) =>
                {
                    _testOutputHelper.WriteLine("1. local rollback");
                    await Task.CompletedTask;
                }).Do(async (barrier) => { return await Task.FromResult<(byte[], Exception)>(("my result"u8.ToArray(), null)); });

                // 2. http1, SAGA
                HttpResponseMessage httpResult1 = await workflow.NewBranch().OnRollback(async (barrier) =>
                {
                    _testOutputHelper.WriteLine("4. http1 rollback");
                    await workflow.NewRequest().GetAsync("http://localhost:5006/test-http-ok1");
                }).NewRequest().GetAsync("http://localhost:5006/test-http-ok1");

                // 3. http2, TCC
                HttpResponseMessage httpResult2 = await workflow.NewBranch().OnRollback(async (barrier) =>
                {
                    _testOutputHelper.WriteLine("4. http2 cancel");
                    await workflow.NewRequest().GetAsync("http://localhost:5006/test-http-ok1");
                }).OnCommit(async (barrier) =>
                {
                    _testOutputHelper.WriteLine("4. http2 commit");
                    // NOT must use workflow.NewRequest()
                    await workflow.NewRequest().GetAsync("http://localhost:5006/test-http-ok1");
                }).NewRequest().GetAsync("http://localhost:5006/test-http-ok1");

                return await Task.FromResult("my result"u8.ToArray());
            });

            string gid = wfName1 + Guid.NewGuid().ToString()[..8];
            var req = ITTestHelper.GenBusiReq(false, false);

            DtmClient dtmClient = new DtmClient(provider.GetRequiredService<IHttpClientFactory>(), provider.GetRequiredService<IOptions<DtmOptions>>());
            TransGlobal trans;

            // BranchID	Op	Status
            // 01	action	succeed			
            // 02	action	succeed			
            // 03	action	succeed			
            // 03	commit	succeed
            // first
            byte[] result = await workflowGlobalTransaction.Execute(wfName1, gid, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)));
            Assert.Equal("my result", Encoding.UTF8.GetString(result));
            trans = await dtmClient.Query(gid, CancellationToken.None);
            Assert.Equal("succeed", trans.Transaction.Status);
            Assert.Equal(4, trans.Branches.Count); // 1.Do x1, 2.http, saga x1, 3.Http tcc x2
            Assert.Equal("action", trans.Branches[0].Op);
            Assert.Equal("succeed", trans.Branches[0].Status);
            Assert.Equal("action", trans.Branches[1].Op);
            Assert.Equal("succeed", trans.Branches[1].Status);
            Assert.Equal("action", trans.Branches[2].Op);
            Assert.Equal("succeed", trans.Branches[2].Status);
            Assert.Equal("commit", trans.Branches[3].Op);
            Assert.Equal("succeed", trans.Branches[3].Status);

            // same gid again
            result = await workflowGlobalTransaction.Execute(wfName1, gid, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)));
            Assert.Equal("my result", Encoding.UTF8.GetString(result));
            trans = await dtmClient.Query(gid, CancellationToken.None);
            Assert.Equal("succeed", trans.Transaction.Status);
            Assert.Equal(4, trans.Branches.Count); // 1.Do x1, 2.http, saga x1, 3.Http tcc x2
            Assert.Equal("action", trans.Branches[0].Op);
            Assert.Equal("succeed", trans.Branches[0].Status);
            Assert.Equal("action", trans.Branches[1].Op);
            Assert.Equal("succeed", trans.Branches[1].Status);
            Assert.Equal("action", trans.Branches[2].Op);
            Assert.Equal("succeed", trans.Branches[2].Status);
            Assert.Equal("commit", trans.Branches[3].Op);
            Assert.Equal("succeed", trans.Branches[3].Status);
        }

        [Fact]
        public async Task Execute_DoAndHttp_Failed()
        {
            var provider = ITTestHelper.AddDtmGrpc();
            var workflowFactory = provider.GetRequiredService<IWorkflowFactory>();
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            WorkflowGlobalTransaction workflowGlobalTransaction = new WorkflowGlobalTransaction(workflowFactory, loggerFactory);

            string wfName1 = $"wf-simple-{Guid.NewGuid().ToString("D")[..8]}";
            workflowGlobalTransaction.Register(wfName1, async (workflow, data) =>
            {
                // 1. local
                workflow.NewBranch().OnRollback(async (barrier) =>
                {
                    _testOutputHelper.WriteLine("1. local rollback");
                    await Task.CompletedTask;
                }).Do(async (barrier) => { return await Task.FromResult<(byte[], Exception)>(("my result"u8.ToArray(), null)); });

                // 2. http1
                HttpResponseMessage httpResult1 = await workflow.NewBranch().OnRollback(async (barrier) =>
                {
                    _testOutputHelper.WriteLine("4. http1 rollback");
                    await Task.CompletedTask;
                }).NewRequest().GetAsync("http://localhost:5006/test-http-ok1");

                // 3. http2
                HttpResponseMessage httpResult2 = await workflow.NewBranch().OnRollback(async (barrier) =>
                {
                    _testOutputHelper.WriteLine("4. http2 rollback");
                    await Task.CompletedTask;
                }).NewRequest().GetAsync("http://localhost:5006/409"); // 409

                return await Task.FromResult("my result"u8.ToArray());
            });

            string gid = wfName1 + Guid.NewGuid().ToString()[..8];
            var req = ITTestHelper.GenBusiReq(false, false);

            DtmClient dtmClient = new DtmClient(provider.GetRequiredService<IHttpClientFactory>(), provider.GetRequiredService<IOptions<DtmOptions>>());
            TransGlobal trans;

            await Assert.ThrowsAsync<DtmCommon.DtmFailureException>(async () =>
            {
                byte[] _ = await workflowGlobalTransaction.Execute(wfName1, gid, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)));
            });

            // same gid again
            await Assert.ThrowsAsync<DtmCommon.DtmFailureException>(async () => { await workflowGlobalTransaction.Execute(wfName1, gid, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req))); });
            trans = await dtmClient.Query(gid, CancellationToken.None);
            Assert.Equal("failed", trans.Transaction.Status);
            // BranchID	Op	Status	CreateTime	UpdateTime	Url
            // 01	action	succeed			
            // 02	action	succeed			
            // 03	action	failed			
            // 02	rollback	succeed			
            // 01	rollback	succeed
            Assert.Equal(5, trans.Branches.Count);
            Assert.Equal("action", trans.Branches[0].Op);
            Assert.Equal("succeed", trans.Branches[0].Status);
            Assert.Equal("action", trans.Branches[1].Op);
            Assert.Equal("succeed", trans.Branches[1].Status);
            Assert.Equal("action", trans.Branches[2].Op);
            Assert.Equal("failed", trans.Branches[2].Status);
            Assert.Equal("rollback", trans.Branches[3].Op);
            Assert.Equal("succeed", trans.Branches[3].Status);
            Assert.Equal("rollback", trans.Branches[4].Op);
            Assert.Equal("succeed", trans.Branches[4].Status);
        }

        [Fact]
        public async Task Execute_DoAndGrpcSAGA_Should_Success()
        {
            var provider = ITTestHelper.AddDtmGrpc();
            var workflowFactory = provider.GetRequiredService<IWorkflowFactory>();
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            WorkflowGlobalTransaction workflowGlobalTransaction = new WorkflowGlobalTransaction(workflowFactory, loggerFactory);

            string wfName1 = $"{nameof(this.Execute_DoAndGrpcSAGA_Should_Success)}-{Guid.NewGuid().ToString("D")[..8]}";
            workflowGlobalTransaction.Register(wfName1, async (workflow, data) =>
            {
                BusiReq request = JsonConvert.DeserializeObject<BusiReq>(Encoding.UTF8.GetString(data));

                // 1. local
                workflow.NewBranch().OnRollback(async (barrier) => { _testOutputHelper.WriteLine("1. local rollback"); }).Do(async (barrier) => { return ("my result"u8.ToArray(), null); });

                // 2. grpc1
                Busi.BusiClient busiClient = null;
                var wf = workflow.NewBranch().OnRollback(async (barrier) =>
                {
                    await busiClient.TransInRevertAsync(request);
                    _testOutputHelper.WriteLine("2. grpc1 rollback");
                });
                busiClient = GetBusiClientWithWf(wf, provider);
                await busiClient.TransOutAsync(request);

                // 3. grpc2
                wf = workflow.NewBranch().OnRollback(async (barrier) =>
                {
                    await busiClient.TransOutRevertAsync(request);
                    _testOutputHelper.WriteLine("3. grpc2 rollback");
                });
                await busiClient.TransInAsync(request);

                return await Task.FromResult("my result"u8.ToArray());
            });

            string gid = wfName1 + Guid.NewGuid().ToString()[..8];
            var req = ITTestHelper.GenBusiReq(false, false);

            DtmClient dtmClient = new DtmClient(provider.GetRequiredService<IHttpClientFactory>(), provider.GetRequiredService<IOptions<DtmOptions>>());
            TransGlobal trans;

            // first
            byte[] result = await workflowGlobalTransaction.Execute(wfName1, gid, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)));
            Assert.Equal("my result", Encoding.UTF8.GetString(result));
            trans = await dtmClient.Query(gid, CancellationToken.None);
            Assert.Equal("succeed", trans.Transaction.Status);
            Assert.Equal(3, trans.Branches.Count); // 1.Do 2.grpc 3.grpc
            Assert.Equal("succeed", trans.Branches[0].Status);
            Assert.Equal("succeed", trans.Branches[1].Status);
            Assert.Equal("succeed", trans.Branches[2].Status);

            // same gid again
            result = await workflowGlobalTransaction.Execute(wfName1, gid, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)));
            Assert.Equal("my result", Encoding.UTF8.GetString(result));
            trans = await dtmClient.Query(gid, CancellationToken.None);
            Assert.Equal("succeed", trans.Transaction.Status);
            Assert.Equal(3, trans.Branches.Count); // 1.Do 2.Http 3.Http
            Assert.Equal("succeed", trans.Branches[0].Status);
            Assert.Equal("succeed", trans.Branches[1].Status);
            Assert.Equal("succeed", trans.Branches[2].Status);
        }

        [Fact]
        public async Task Execute_DoAndGrpcSAGA_Should_Failed()
        {
            var provider = ITTestHelper.AddDtmGrpc();
            var workflowFactory = provider.GetRequiredService<IWorkflowFactory>();
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            WorkflowGlobalTransaction workflowGlobalTransaction = new WorkflowGlobalTransaction(workflowFactory, loggerFactory);

            string wfName1 = $"{nameof(this.Execute_DoAndGrpcSAGA_Should_Failed)}-{Guid.NewGuid().ToString("D")[..8]}";
            workflowGlobalTransaction.Register(wfName1, async (workflow, data) =>
            {
                BusiReq request = JsonConvert.DeserializeObject<BusiReq>(Encoding.UTF8.GetString(data));

                // 1. local
                workflow.NewBranch().OnRollback(async (barrier) => { _testOutputHelper.WriteLine("1. local rollback"); }).Do(async (barrier) =>
                {
                    return await Task.FromResult<(byte[], Exception)>(("my result"u8.ToArray(), null));
                });

                // 2. grpc1
                Busi.BusiClient busiClient = null;
                var wf = workflow.NewBranch().OnRollback(async (barrier) =>
                {
                    await busiClient.TransInRevertAsync(request);
                    _testOutputHelper.WriteLine("2. grpc1 rollback");
                });
                busiClient = GetBusiClientWithWf(wf, provider);
                Empty response1 = await busiClient.TransOutAsync(request);

                // 3. grpc2
                wf = workflow.NewBranch().OnRollback(async (barrier) =>
                {
                    await busiClient.TransOutRevertAsync(request);
                    _testOutputHelper.WriteLine("3. grpc2 rollback");
                });
                Empty response2 = await busiClient.TransInAsync(request);

                return await Task.FromResult("my result"u8.ToArray());
            });

            string gid = wfName1 + Guid.NewGuid().ToString()[..8];
            var req = ITTestHelper.GenBusiReq(outFailed: false, inFailed: true);

            DtmClient dtmClient = new DtmClient(provider.GetRequiredService<IHttpClientFactory>(), provider.GetRequiredService<IOptions<DtmOptions>>());
            TransGlobal trans;

            // first
            await Assert.ThrowsAsync<DtmCommon.DtmFailureException>(async () =>
            {
                byte[] _ = await workflowGlobalTransaction.Execute(wfName1, gid, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)));
            });

            trans = await dtmClient.Query(gid, CancellationToken.None);
            Assert.Equal("failed", trans.Transaction.Status);
            // BranchID	Op	Status	CreateTime	UpdateTime	Url
            // 01	action	succeed			
            // 02	action	succeed			
            // 03	action	failed			
            // 02	rollback	succeed			
            // 01	rollback	succeed
            Assert.Equal(5, trans.Branches.Count);
            Assert.Equal("action", trans.Branches[0].Op);
            Assert.Equal("succeed", trans.Branches[0].Status);
            Assert.Equal("action", trans.Branches[1].Op);
            Assert.Equal("succeed", trans.Branches[1].Status);
            Assert.Equal("action", trans.Branches[2].Op);
            Assert.Equal("failed", trans.Branches[2].Status);
            Assert.Equal("rollback", trans.Branches[3].Op);
            Assert.Equal("succeed", trans.Branches[3].Status);
            Assert.Equal("rollback", trans.Branches[4].Op);
            Assert.Equal("succeed", trans.Branches[4].Status);

            // same gid again
            await Assert.ThrowsAsync<DtmCommon.DtmFailureException>(async () =>
            {
                byte[] _ = await workflowGlobalTransaction.Execute(wfName1, gid, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)));
            });
        }


        [Fact]
        public async Task Execute_GrpcTccAndDo_Should_Success()
        {
            var provider = ITTestHelper.AddDtmGrpc();
            var workflowFactory = provider.GetRequiredService<IWorkflowFactory>();
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            WorkflowGlobalTransaction workflowGlobalTransaction = new WorkflowGlobalTransaction(workflowFactory, loggerFactory);

            string wfName1 = $"{nameof(this.Execute_GrpcTccAndDo_Should_Success)}-{Guid.NewGuid().ToString("D")[..8]}";
            workflowGlobalTransaction.Register(wfName1, async (workflow, data) =>
            {
                BusiReq request = JsonConvert.DeserializeObject<BusiReq>(Encoding.UTF8.GetString(data));

                // 1. grpc1 TCC
                Busi.BusiClient busiClient = null;
                Workflow wf = workflow.NewBranch()
                    .OnCommit(async (barrier) => // confirm
                    {
                        await busiClient.TransOutConfirmAsync(request);
                    })
                    .OnRollback(async (barrier) => // cancel
                    {
                        await busiClient.TransOutRevertAsync(request);
                        _testOutputHelper.WriteLine("1. grpc1 cancel");
                    });
                busiClient = GetBusiClientWithWf(wf, provider); // The construction of busiClient dependence on the Workflow instance, must ugly code
                // try
                await busiClient.TransOutTccAsync(request);

                // 2. local， maybe SAG, at the end, no need to write the reverse rollback.
                workflow.NewBranch()
                    // .OnRollback(async (barrier) =>
                    // {
                    //     _testOutputHelper.WriteLine("1. local rollback");
                    // })
                    .Do(async (barrier) => { return ("my result"u8.ToArray(), null); });

                return await Task.FromResult("my result"u8.ToArray());
            });

            string gid = wfName1 + Guid.NewGuid().ToString()[..8];
            var req = ITTestHelper.GenBusiReq(false, false);

            DtmClient dtmClient = new DtmClient(provider.GetRequiredService<IHttpClientFactory>(), provider.GetRequiredService<IOptions<DtmOptions>>());
            TransGlobal trans;

            // first
            byte[] result = await workflowGlobalTransaction.Execute(wfName1, gid, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)));
            Assert.Equal("my result", Encoding.UTF8.GetString(result));
            trans = await dtmClient.Query(gid, CancellationToken.None);
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
        public async Task Execute_GrpcTccAndDo_Should_TryFailed()
        {
            var provider = ITTestHelper.AddDtmGrpc();
            var workflowFactory = provider.GetRequiredService<IWorkflowFactory>();
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            WorkflowGlobalTransaction workflowGlobalTransaction = new WorkflowGlobalTransaction(workflowFactory, loggerFactory);

            string wfName1 = $"{nameof(this.Execute_GrpcTccAndDo_Should_Success)}-{Guid.NewGuid().ToString("D")[..8]}";
            workflowGlobalTransaction.Register(wfName1, async (workflow, data) =>
            {
                BusiReq request = JsonConvert.DeserializeObject<BusiReq>(Encoding.UTF8.GetString(data));

                // 1. grpc1 TCC
                Busi.BusiClient busiClient = null;
                Workflow wf = workflow.NewBranch()
                    .OnCommit(async (barrier) => // confirm
                    {
                        await busiClient.TransOutConfirmAsync(request);
                    })
                    .OnRollback(async (barrier) => // cancel
                    {
                        await busiClient.TransOutRevertAsync(request);
                        _testOutputHelper.WriteLine("1. grpc1 cancel");
                    });
                busiClient = GetBusiClientWithWf(wf, provider); // busiClient reference Workflow instance
                // try
                await busiClient.TransOutTccAsync(request);

                // 2. local， it's the tail, rollback is NOT necessary
                workflow.NewBranch()
                    // .OnRollback(async (barrier) => // rollback
                    // {
                    //     _testOutputHelper.WriteLine("1. local rollback");
                    // })
                    .Do(async (barrier) => { return ("my result"u8.ToArray(), null); });

                return await Task.FromResult("my result"u8.ToArray());
            });

            string gid = wfName1 + Guid.NewGuid().ToString()[..8];
            var req = ITTestHelper.GenBusiReq(outFailed: true, inFailed: false); // 1. trans out try failed

            DtmClient dtmClient = new DtmClient(provider.GetRequiredService<IHttpClientFactory>(), provider.GetRequiredService<IOptions<DtmOptions>>());
            TransGlobal trans;

            // first
            await Assert.ThrowsAsync<DtmCommon.DtmFailureException>(async () =>
            {
                byte[] _ = await workflowGlobalTransaction.Execute(wfName1, gid, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)));
            });
            trans = await dtmClient.Query(gid, CancellationToken.None);
            // BranchID	Op	Status
            // 01	action	failed
            Assert.Equal("failed", trans.Transaction.Status);
            Assert.Equal(1, trans.Branches.Count);
            Assert.Equal("failed", trans.Branches[0].Status);
            Assert.Equal("action", trans.Branches[0].Op);

            // same gid again
            await Assert.ThrowsAsync<DtmCommon.DtmFailureException>(async () =>
            {
                byte[] _ = await workflowGlobalTransaction.Execute(wfName1, gid, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)));
                // DtmCommon.DtmFailureException: Status(StatusCode="Aborted", Detail="FAILURE")
                //
                // DtmCommon.DtmFailureException
                // Status(StatusCode="Aborted", Detail="FAILURE")
                // at Dtmworkflow.Workflow.Process(WfFunc2 handler, Byte[] data) in src/Dtmworkflow/Workflow.Imp.cs
                // at Dtmworkflow.WorkflowGlobalTransaction.Execute(String name, String gid, Byte[] data, Boolean isHttp) in src/Dtmworkflow/WorkflowGlobalTransaction.cs
            });

            trans = await dtmClient.Query(gid, CancellationToken.None);
            // BranchID	Op	Status
            // 01	action	failed
            Assert.Equal("failed", trans.Transaction.Status);
            Assert.Equal(1, trans.Branches.Count);
            Assert.Equal("failed", trans.Branches[0].Status);
            Assert.Equal("action", trans.Branches[0].Op);
        }

        [Fact]
        public async Task Execute_GrpcTccAndDo_Should_DoFailed()
        {
            var provider = ITTestHelper.AddDtmGrpc();
            var workflowFactory = provider.GetRequiredService<IWorkflowFactory>();
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            WorkflowGlobalTransaction workflowGlobalTransaction = new WorkflowGlobalTransaction(workflowFactory, loggerFactory);

            string wfName1 = $"{nameof(this.Execute_GrpcTccAndDo_Should_Success)}-{Guid.NewGuid().ToString("D")[..8]}";
            workflowGlobalTransaction.Register(wfName1, async (workflow, data) =>
            {
                BusiReq request = JsonConvert.DeserializeObject<BusiReq>(Encoding.UTF8.GetString(data));

                // 1. grpc1 TCC
                Busi.BusiClient busiClient = null;
                Workflow wf = workflow.NewBranch()
                    .OnCommit(async (barrier) => // confirm
                    {
                        await busiClient.TransOutConfirmAsync(request);
                    })
                    .OnRollback(async (barrier) => // cancel
                    {
                        await busiClient.TransOutRevertAsync(request);
                        _testOutputHelper.WriteLine("1. grpc1 cancel");
                    });
                busiClient = GetBusiClientWithWf(wf, provider); // busiClient reference Workflow instance
                // try
                await busiClient.TransOutTccAsync(request);

                // 2. local， it's the tail, rollback is NOT necessary
                (byte[] doResult, Exception ex) = await workflow.NewBranch()
                    .OnRollback(async (barrier) => // rollback
                    {
                        _testOutputHelper.WriteLine("1. local rollback");
                    })
                    .Do(async (barrier) =>
                    {
                        // throw new DtmFailureException("db do failed"); // can't throw 
                        var ex = new DtmFailureException("db do failed");
                        return ("my result"u8.ToArray(), ex);
                    });
                if (ex != null)
                    throw ex;

                return await Task.FromResult("my result"u8.ToArray());
            });

            string gid = wfName1 + Guid.NewGuid().ToString()[..8];
            var req = ITTestHelper.GenBusiReq(outFailed: false, inFailed: false);

            DtmClient dtmClient = new DtmClient(provider.GetRequiredService<IHttpClientFactory>(), provider.GetRequiredService<IOptions<DtmOptions>>());
            TransGlobal trans;

            // first
            await Assert.ThrowsAsync<DtmCommon.DtmFailureException>(async () =>
            {
                byte[] _ = await workflowGlobalTransaction.Execute(wfName1, gid, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)));
            });

            trans = await dtmClient.Query(gid, CancellationToken.None);
            // BranchID	Op	Status
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
            await Assert.ThrowsAsync<DtmCommon.DtmFailureException>(async () =>
            {
                var result = await workflowGlobalTransaction.Execute(wfName1, gid, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)));
                // DtmCommon.DtmFailureException
                //     db do failed
                //     at Dtmworkflow.Workflow.Process(WfFunc2 handler, Byte[] data) in src/Dtmworkflow/Workflow.Imp.cs
                // at Dtmworkflow.WorkflowGlobalTransaction.Execute(String name, String gid, Byte[] data, Boolean isHttp) in src/Dtmworkflow/WorkflowGlobalTransaction.cs
                // at Dtmgrpc.IntegrationTests.WorkflowGrpcTest.Execute_GrpcTccAndDo_Should_DoFailed() in tests/Dtmgrpc.IntegrationTests/WorkflowGrpcTest.cs
            });
        }

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