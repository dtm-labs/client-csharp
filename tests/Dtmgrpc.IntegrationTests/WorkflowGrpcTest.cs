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
        public async Task Execute_DoAndHttpSuccess()
        {
            var provider = ITTestHelper.AddDtmGrpc();
            var workflowFactory = provider.GetRequiredService<IWorkflowFactory>();
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            WorkflowGlobalTransaction workflowGlobalTransaction = new WorkflowGlobalTransaction(workflowFactory, loggerFactory);

            Busi.BusiClient busiClient = new Busi.BusiClient(GrpcChannel.ForAddress(ITTestHelper.BuisgRPCUrlWithProtocol));

            string wfName1 = $"wf-simple-{Guid.NewGuid().ToString("D")[..8]}";
            workflowGlobalTransaction.Register(wfName1, async (workflow, data) =>
            {
                BusiReq request = JsonConvert.DeserializeObject<BusiReq>(Encoding.UTF8.GetString(data));

                // 1. local
                workflow.NewBranch().OnRollback(async (barrier) =>
                {
                    _testOutputHelper.WriteLine("1. local rollback");
                }).Do(async (barrier) =>
                {
                    return ("my result"u8.ToArray(), null);
                });
                
                // 2. http1
                HttpResponseMessage httpResult1 =  await workflow.NewBranch().OnRollback(async (barrier) =>
                {
                    _testOutputHelper.WriteLine("4. http1 rollback");
                }).NewRequest().GetAsync("http://localhost:5006/test-http-ok1");
                
                // 3. http2
                HttpResponseMessage httpResult2 =  await workflow.NewBranch().OnRollback(async (barrier) =>
                {
                    _testOutputHelper.WriteLine("4. http2 rollback");
                }).NewRequest().GetAsync("http://localhost:5006/test-http-ok2");
                
                await busiClient.TransOutAsync(request);
                
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
            Assert.Equal(3, trans.Branches.Count); // 1.Do 2.Http 3.Http
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
        public async Task Execute_DoAndHttp_Failed()
        {
            var provider = ITTestHelper.AddDtmGrpc();
            var workflowFactory = provider.GetRequiredService<IWorkflowFactory>();
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            WorkflowGlobalTransaction workflowGlobalTransaction = new WorkflowGlobalTransaction(workflowFactory, loggerFactory);

            Busi.BusiClient busiClient = new Busi.BusiClient(GrpcChannel.ForAddress(ITTestHelper.BuisgRPCUrlWithProtocol));

            string wfName1 = $"wf-simple-{Guid.NewGuid().ToString("D")[..8]}";
            workflowGlobalTransaction.Register(wfName1, async (workflow, data) =>
            {
                BusiReq request = JsonConvert.DeserializeObject<BusiReq>(Encoding.UTF8.GetString(data));

                // 1. local
                workflow.NewBranch().OnRollback(async (barrier) =>
                {
                    _testOutputHelper.WriteLine("1. local rollback");
                }).Do(async (barrier) =>
                {
                    return ("my result"u8.ToArray(), null);
                });
                
                // 2. http1
                HttpResponseMessage httpResult1 =  await workflow.NewBranch().OnRollback(async (barrier) =>
                {
                    _testOutputHelper.WriteLine("4. http1 rollback");
                }).NewRequest().GetAsync("http://localhost:5006/test-http-ok1");
                
                // 3. http2
                HttpResponseMessage httpResult2 =  await workflow.NewBranch().OnRollback(async (barrier) =>
                {
                    _testOutputHelper.WriteLine("4. http2 rollback");
                }).NewRequest().GetAsync("http://localhost:5006/409"); // 409
              
                return await Task.FromResult("my result"u8.ToArray());
            });

            string gid = wfName1 + Guid.NewGuid().ToString()[..8];
            var req = ITTestHelper.GenBusiReq(false, false);
            
            DtmClient dtmClient = new DtmClient(provider.GetRequiredService<IHttpClientFactory>(), provider.GetRequiredService<IOptions<DtmOptions>>());
            TransGlobal trans;
            
            byte[] result = await workflowGlobalTransaction.Execute(wfName1, gid, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)));
            Assert.Null(result);
            // same gid again
            await Assert.ThrowsAsync<DtmCommon.DtmFailureException>( async () =>
            {
                await workflowGlobalTransaction.Execute(wfName1, gid, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)));
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
        }
    }
}