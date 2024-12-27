using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using busi;
using Dtmworkflow;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Xunit;

namespace Dtmgrpc.IntegrationTests
{
    public class WorkflowGrpcTest
    {
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
            workflowGlobalTransaction.Register(wfName1, async (workflow, data) => await Task.FromResult("my result"u8.ToArray()));

            string gid = wfName1 + Guid.NewGuid().ToString()[..8];
            var req = ITTestHelper.GenBusiReq(false, false);
            await workflowGlobalTransaction.Execute(wfName1, gid, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)), isHttp: false);

            string status = await ITTestHelper.GetTranStatus(gid);
            Assert.Equal("succeed", status);
        }

        [Fact]
        public async Task Execute_Success()
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
                workflow.NewBranch().OnRollback(async (barrier) =>
                {
                    await busiClient.TransInRevertAsync(request);
                });
                await busiClient.TransInAsync(request);

                workflow.NewBranch().OnRollback(async (barrier) =>
                {
                    await busiClient.TransOutRevertAsync(request);
                });
                await busiClient.TransOutAsync(request);

                return await Task.FromResult("my result"u8.ToArray());
            });

            string gid = wfName1 + Guid.NewGuid().ToString()[..8];
            var req = ITTestHelper.GenBusiReq(false, false);
            byte[] result = await workflowGlobalTransaction.Execute(wfName1, gid, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)));
            Assert.Equal("my result", Encoding.UTF8.GetString(result));
            string status = await ITTestHelper.GetTranStatus(gid);
            Assert.Equal("succeed", status);
            
            // again
            result = await workflowGlobalTransaction.Execute(wfName1, gid, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)));
            Assert.Equal("my result", Encoding.UTF8.GetString(result));
            status = await ITTestHelper.GetTranStatus(gid);
            Assert.Equal("succeed", status);
        }
    }
}