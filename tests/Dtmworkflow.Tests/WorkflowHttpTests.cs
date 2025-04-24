using Dtmcli;
using DtmCommon;
using Dtmgrpc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using static System.Net.WebRequestMethods;

namespace Dtmworkflow.Tests
{
    public class WorkflowHttpTests
    {
        [Fact]
        public async void Execute_Should_Succeed_When_PWF_Succeed()
        {
            var factory = new Mock<IWorkflowFactory>();
            var httpClient = new Mock<IDtmClient>();
            var grpcClient = new Mock<IDtmgRPCClient>();
            var httpBb = new Mock<Dtmcli.IBranchBarrierFactory>();

            SetupPrepareWorkflow(httpClient, DtmCommon.Constant.StatusSucceed, "123");
            var wf = SetupWorkFlow(httpClient, grpcClient, httpBb);

            factory.Setup(x => x.NewWorkflow(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<bool>())).Returns(wf.Object);

            var wfgt = new WorkflowGlobalTransaction(factory.Object, NullLoggerFactory.Instance);

            var wfName = nameof(Execute_Should_Succeed_When_PWF_Succeed);
            var gid = Guid.NewGuid().ToString("N");

            var handler = new Mock<WfFunc2>();
            handler.Setup(x => x.Invoke(It.IsAny<Workflow>(), It.IsAny<byte[]>())).Returns(Task.FromResult(Encoding.UTF8.GetBytes("123")));

            wfgt.Register(wfName, handler.Object);

            var req = JsonSerializer.Serialize(new { userId = "1", amount = 30 });
            var res = await wfgt.Execute(wfName, gid, Encoding.UTF8.GetBytes(req), true);

            Assert.Equal("123", Encoding.UTF8.GetString(res));
        }

        [Fact]
        public async void Execute_Should_Throw_DtmFailureException_When_PWF_Failed()
        {
            var factory = new Mock<IWorkflowFactory>();
            var httpClient = new Mock<IDtmClient>();
            var grpcClient = new Mock<IDtmgRPCClient>();
            var httpBb = new Mock<Dtmcli.IBranchBarrierFactory>();

            SetupPrepareWorkflow(httpClient, DtmCommon.Constant.StatusFailed, "123");
            var wf = SetupWorkFlow(httpClient, grpcClient, httpBb);

            factory.Setup(x => x.NewWorkflow(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<bool>())).Returns(wf.Object);

            var wfgt = new WorkflowGlobalTransaction(factory.Object, NullLoggerFactory.Instance);

            var wfName = nameof(Execute_Should_Throw_DtmFailureException_When_PWF_Failed);
            var gid = Guid.NewGuid().ToString("N");

            var handler = new Mock<WfFunc2>();
            handler.Setup(x => x.Invoke(It.IsAny<Workflow>(), It.IsAny<byte[]>())).Returns(Task.FromResult<byte[]>(null));

            wfgt.Register(wfName, handler.Object);

            var req = JsonSerializer.Serialize(new { userId = "1", amount = 30 });

            await Assert.ThrowsAsync<DtmFailureException>(async ()=> await wfgt.Execute(wfName, gid, Encoding.UTF8.GetBytes(req), true));
        }

        [Fact]
        public async void Execute_Should_Succeed_When_PWF_Submitted_And_Progress_Not_Failed()
        {
            var factory = new Mock<IWorkflowFactory>();
            var httpClient = new Mock<IDtmClient>();
            var grpcClient = new Mock<IDtmgRPCClient>();
            var httpBb = new Mock<Dtmcli.IBranchBarrierFactory>();

            var progressDtos = new List<DtmProgressDto>
            {
                new DtmProgressDto { Status = Constant.StatusSucceed, BranchId = "01", Op = Constant.OpAction },
                new DtmProgressDto { Status = Constant.StatusSucceed, BranchId = "02", Op = Constant.OpAction }
            };

            SetupPrepareWorkflow(httpClient, Constant.StatusSubmitted, "123", progressDtos);
            var wf = SetupWorkFlow(httpClient, grpcClient, httpBb);

            factory.Setup(x => x.NewWorkflow(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<bool>())).Returns(wf.Object);

            var wfgt = new WorkflowGlobalTransaction(factory.Object, NullLoggerFactory.Instance);

            var wfName = nameof(Execute_Should_Succeed_When_PWF_Submitted_And_Progress_Not_Failed);
            var gid = Guid.NewGuid().ToString("N");

            var handler = new Mock<WfFunc2>();
            handler.Setup(x => x.Invoke(It.IsAny<Workflow>(), It.IsAny<byte[]>())).Returns(Task.FromResult(Encoding.UTF8.GetBytes("123")));

            wfgt.Register(wfName, handler.Object);
            var req = JsonSerializer.Serialize(new { userId = "1", amount = 30 });

            var res = await wfgt.Execute(wfName, gid, Encoding.UTF8.GetBytes(req), true);
            handler.Verify(x=>x.Invoke(It.IsAny<Workflow>(), It.IsAny<byte[]>()), Times.Once());
            Assert.Equal("123", Encoding.UTF8.GetString(res));
        }

        [Fact]
        public async void Execute_Should_ThrowException_When_WfFunc2_ThrowException()
        {
            var factory = new Mock<IWorkflowFactory>();
            var httpClient = new Mock<IDtmClient>();
            var grpcClient = new Mock<IDtmgRPCClient>();
            var httpBb = new Mock<Dtmcli.IBranchBarrierFactory>();

            var progressDtos = new List<DtmProgressDto>
            {
                new DtmProgressDto { Status = Constant.StatusSucceed, BranchId = "01", Op = Constant.OpAction },
                new DtmProgressDto { Status = Constant.StatusSucceed, BranchId = "02", Op = Constant.OpAction }
            };

            SetupPrepareWorkflow(httpClient, Constant.StatusSubmitted, "123", progressDtos);
            var wf = SetupWorkFlow(httpClient, grpcClient, httpBb);

            factory.Setup(x => x.NewWorkflow(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<bool>())).Returns(wf.Object);

            var wfgt = new WorkflowGlobalTransaction(factory.Object, NullLoggerFactory.Instance);

            var wfName = nameof(Execute_Should_ThrowException_When_WfFunc2_ThrowException);
            var gid = Guid.NewGuid().ToString("N");

            var handler = new Mock<WfFunc2>();
            handler.Setup(x => x.Invoke(It.IsAny<Workflow>(), It.IsAny<byte[]>())).Throws(new Exception("ex"));

            wfgt.Register(wfName, handler.Object);
            var req = JsonSerializer.Serialize(new { userId = "1", amount = 30 });

            var ex = await Assert.ThrowsAsync<Exception>(async () => await wfgt.Execute(wfName, gid, Encoding.UTF8.GetBytes(req), true));
            Assert.Equal("ex", ex.Message);
        }

        [Fact]
        public async void Execute_Should_ThrowDtmFailureException_When_WfFunc2_ThrowDtmFailureException()
        {
            var factory = new Mock<IWorkflowFactory>();
            var httpClient = new Mock<IDtmClient>();
            var grpcClient = new Mock<IDtmgRPCClient>();
            var httpBb = new Mock<Dtmcli.IBranchBarrierFactory>();

            var progressDtos = new List<DtmProgressDto>
            {
                new DtmProgressDto { Status = Constant.StatusSucceed, BranchId = "01", Op = Constant.OpAction },
                new DtmProgressDto { Status = Constant.StatusSucceed, BranchId = "02", Op = Constant.OpAction }
            };

            SetupPrepareWorkflow(httpClient, Constant.StatusSubmitted, "123", progressDtos);
            var wf = SetupWorkFlow(httpClient, grpcClient, httpBb);

            factory.Setup(x => x.NewWorkflow(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<bool>())).Returns(wf.Object);

            var wfgt = new WorkflowGlobalTransaction(factory.Object, NullLoggerFactory.Instance);

            var wfName = nameof(Execute_Should_ThrowDtmFailureException_When_WfFunc2_ThrowDtmFailureException);
            var gid = Guid.NewGuid().ToString("N");

            var handler = new Mock<WfFunc2>();
            handler.Setup(x => x.Invoke(It.IsAny<Workflow>(), It.IsAny<byte[]>())).Throws(new DtmFailureException());

            wfgt.Register(wfName, handler.Object);
            var req = JsonSerializer.Serialize(new { userId = "1", amount = 30 });

            await Assert.ThrowsAsync<DtmFailureException>(async () => await wfgt.Execute(wfName, gid, Encoding.UTF8.GetBytes(req), true));
        }

        [Fact]
        public async void Rollback_Should_Be_Executed()
        {
            var factory = new Mock<IWorkflowFactory>();
            var httpClient = new Mock<IDtmClient>();
            var grpcClient = new Mock<IDtmgRPCClient>();
            var httpBb = new Mock<Dtmcli.IBranchBarrierFactory>();

            var progressDtos = new List<DtmProgressDto>
            {
                new DtmProgressDto { Status = Constant.StatusSucceed, BranchId = "01", Op = Constant.OpAction }
            };

            SetupPrepareWorkflow(httpClient, Constant.StatusSubmitted, "123", progressDtos);
            var wf = SetupWorkFlow(httpClient, grpcClient, httpBb);
           
            factory.Setup(x => x.NewWorkflow(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<bool>())).Returns(wf.Object);

            var wfgt = new WorkflowGlobalTransaction(factory.Object, NullLoggerFactory.Instance);

            var wfName = nameof(Rollback_Should_Be_Executed);
            var gid = Guid.NewGuid().ToString("N");

            var func = new Mock<WfPhase2Func>();

            WfFunc2 handler = (wf, data) =>
            {
                var client = wf.NewBranch().OnRollback(func.Object).NewRequest();

                // Do request and Failure 
                throw new DtmFailureException();
            };
            wfgt.Register(wfName, handler);
            var req = JsonSerializer.Serialize(new { userId = "1", amount = 30 });

            var res = await Assert.ThrowsAsync<DtmFailureException>(async () => await wfgt.Execute(wfName, gid, Encoding.UTF8.GetBytes(req), true));

            func.Verify(x => x.Invoke(It.IsAny<BranchBarrier>()), Times.Once);
        }

        [Fact]
        public async void Commit_Should_Be_Executed()
        {
            var factory = new Mock<IWorkflowFactory>();
            var httpClient = new Mock<IDtmClient>();
            var grpcClient = new Mock<IDtmgRPCClient>();
            var httpBb = new Mock<Dtmcli.IBranchBarrierFactory>();

            var progressDtos = new List<DtmProgressDto>
            {
                new DtmProgressDto { Status = Constant.StatusSucceed, BranchId = "01", Op = Constant.OpAction }
            };

            SetupPrepareWorkflow(httpClient, Constant.StatusSubmitted, "123", progressDtos);
            var wf = SetupWorkFlow(httpClient, grpcClient, httpBb);

            factory.Setup(x => x.NewWorkflow(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<bool>())).Returns(wf.Object);

            var wfgt = new WorkflowGlobalTransaction(factory.Object, NullLoggerFactory.Instance);

            var wfName = nameof(Commit_Should_Be_Executed);
            var gid = Guid.NewGuid().ToString("N");

            var rollBackFunc = new Mock<WfPhase2Func>();
            var commitFunc = new Mock<WfPhase2Func>();

            WfFunc2 handler = async (wf, data) =>
            {
                var client = wf.NewBranch().OnRollback(rollBackFunc.Object).OnCommit(commitFunc.Object).NewRequest();
                
                // Do Request without error
                return await Task.FromResult(Encoding.UTF8.GetBytes("123"));
            };
            wfgt.Register(wfName, handler);
            var req = JsonSerializer.Serialize(new { userId = "1", amount = 30 });

            await wfgt.Execute(wfName, gid, Encoding.UTF8.GetBytes(req), true);

            rollBackFunc.Verify(x => x.Invoke(It.IsAny<BranchBarrier>()), Times.Never);
            commitFunc.Verify(x => x.Invoke(It.IsAny<BranchBarrier>()), Times.Once);
        }
        
        [Fact]
        public async Task Execute_Result_Should_Be_WfFunc2()
        {
            var factory = new Mock<IWorkflowFactory>();
            var httpClient = new Mock<IDtmClient>();
            var grpcClient = new Mock<IDtmgRPCClient>();
            var httpBb = new Mock<Dtmcli.IBranchBarrierFactory>();

            SetupPrepareWorkflow(httpClient, DtmCommon.Constant.StatusPrepared, null);
            var wf = SetupWorkFlow(httpClient, grpcClient, httpBb);

            factory.Setup(x => x.NewWorkflow(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<bool>())).Returns(wf.Object);

            var wfgt = new WorkflowGlobalTransaction(factory.Object, NullLoggerFactory.Instance);

            var wfName = nameof(Execute_Result_Should_Be_WfFunc2);
            var gid = Guid.NewGuid().ToString("N");

            wfgt.Register(wfName, (workflow, data) => Task.FromResult(Encoding.UTF8.GetBytes("return value from WfFunc2")));

            var req = JsonSerializer.Serialize(new { userId = "1", amount = 30 });
            var res = await wfgt.Execute(wfName, gid, Encoding.UTF8.GetBytes(req), true);

            Assert.Equal("return value from WfFunc2", Encoding.UTF8.GetString(res));
        }
        
        [Fact]
        public async Task Execute_Result_Should_Be_Previous()
        {
            var factory = new Mock<IWorkflowFactory>();
            var httpClient = new Mock<IDtmClient>();
            var grpcClient = new Mock<IDtmgRPCClient>();
            var httpBb = new Mock<Dtmcli.IBranchBarrierFactory>();

            SetupPrepareWorkflow(httpClient, DtmCommon.Constant.StatusSucceed, "return value from previous");
            var wf = SetupWorkFlow(httpClient, grpcClient, httpBb);

            factory.Setup(x => x.NewWorkflow(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<bool>())).Returns(wf.Object);

            var wfgt = new WorkflowGlobalTransaction(factory.Object, NullLoggerFactory.Instance);

            var wfName = nameof(Execute_Result_Should_Be_Previous);
            var gid = Guid.NewGuid().ToString("N");

            wfgt.Register(wfName, (workflow, data) => Task.FromResult(Encoding.UTF8.GetBytes("return value from WfFunc2")));

            var req = JsonSerializer.Serialize(new { userId = "1", amount = 30 });
            var res = await wfgt.Execute(wfName, gid, Encoding.UTF8.GetBytes(req), true);

            Assert.Equal("return value from previous", Encoding.UTF8.GetString(res));
        }
        
        [Fact]
        public async Task Execute_Again_Result_Should_Be_Previous()
        {
            var factory = new Mock<IWorkflowFactory>();
            var httpClient1 = new Mock<IDtmClient>();
            var httpClient2 = new Mock<IDtmClient>();
            var grpcClient = new Mock<IDtmgRPCClient>();
            var httpBb = new Mock<Dtmcli.IBranchBarrierFactory>();
            
            // first
            SetupPrepareWorkflow(httpClient1, DtmCommon.Constant.StatusPrepared, null);
            var wf = SetupWorkFlow(httpClient1, grpcClient, httpBb);
            factory.Setup(x => x.NewWorkflow(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<bool>())).Returns(wf.Object);
            var wfgt = new WorkflowGlobalTransaction(factory.Object, NullLoggerFactory.Instance);
            var wfName = nameof(Execute_Again_Result_Should_Be_Previous);
            var gid = Guid.NewGuid().ToString("N");
            wfgt.Register(wfName, (workflow, data) => Task.FromResult(Encoding.UTF8.GetBytes("return value from WfFunc2")));
            var req = JsonSerializer.Serialize(new { userId = "1", amount = 30 });
            var res = await wfgt.Execute(wfName, gid, Encoding.UTF8.GetBytes(req), true);
            Assert.Equal("return value from WfFunc2", Encoding.UTF8.GetString(res));
            
            // again
            SetupPrepareWorkflow(httpClient2, DtmCommon.Constant.StatusSucceed, "return value from previous");
            wf = SetupWorkFlow(httpClient2, grpcClient, httpBb);
            factory.Setup(x => x.NewWorkflow(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<bool>())).Returns(wf.Object);
            wfgt = new WorkflowGlobalTransaction(factory.Object, NullLoggerFactory.Instance);
            gid = Guid.NewGuid().ToString("N");
            wfgt.Register(wfName, (workflow, data) => Task.FromResult(Encoding.UTF8.GetBytes("return value from WfFunc2")));
            req = JsonSerializer.Serialize(new { userId = "1", amount = 30 });
            res = await wfgt.Execute(wfName, gid, Encoding.UTF8.GetBytes(req), true);
            Assert.Equal("return value from previous", Encoding.UTF8.GetString(res));
        }
        
        [Fact]
        public async Task Execute_Again_Result_StringEmpty()
        {
            var factory = new Mock<IWorkflowFactory>();
            var httpClient = new Mock<IDtmClient>();
            var grpcClient = new Mock<IDtmgRPCClient>();
            var httpBb = new Mock<Dtmcli.IBranchBarrierFactory>();
            
            // again
            SetupPrepareWorkflow(httpClient, DtmCommon.Constant.StatusSucceed, null); 
            var wf = SetupWorkFlow(httpClient, grpcClient, httpBb);
            factory.Setup(x => x.NewWorkflow(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<bool>())).Returns(wf.Object);
            var wfgt = new WorkflowGlobalTransaction(factory.Object, NullLoggerFactory.Instance);
            var wfName = nameof(Execute_Again_Result_StringEmpty);
            var gid = Guid.NewGuid().ToString("N");
            wfgt.Register(wfName, (workflow, data) => Task.FromResult(Encoding.UTF8.GetBytes("return value from WfFunc2")));
            var req = JsonSerializer.Serialize(new { userId = "1", amount = 30 });
            var res = await wfgt.Execute(wfName, gid, Encoding.UTF8.GetBytes(req), true);
            Assert.Null(res);
        }

        private void SetupPrepareWorkflow(Mock<IDtmClient> httpClient, string status, string? result, List<DtmProgressDto> progressDtos = null)
        {
            var httpResp = new HttpResponseMessage(HttpStatusCode.OK);
            httpResp.Content = new StringContent(JsonSerializer.Serialize(
                new DtmProgressesReplyDto() 
                { 
                    Transaction = new DtmTransactionDto 
                    { 
                        Status = status, 
                        Result = result == null ? null : Convert.ToBase64String(Encoding.UTF8.GetBytes(result)) 
                    },
                    Progresses = progressDtos ?? []
                }));
            httpClient.Setup(x => x.PrepareWorkflow(It.IsAny<DtmCommon.TransBase>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(httpResp));
        }

        private Mock<Workflow> SetupWorkFlow(Mock<IDtmClient> httpClient, Mock<IDtmgRPCClient> grpcClient, Mock<Dtmcli.IBranchBarrierFactory> httpBb)
        {
            var wf = new Mock<Workflow>(httpClient.Object, grpcClient.Object, httpBb.Object, NullLogger.Instance);
            var tb = TransBase.NewTransBase("1", "workflow", "not inited", "");
            tb.Protocol = Constant.ProtocolHTTP;
            wf.SetupProperty(x => x.TransBase, tb);
            var wfImp = new WorkflowImp
            {
                IDGen = new BranchIDGen(),
                SucceededOps = new List<WorkflowPhase2Item>(),
                FailedOps = new List<WorkflowPhase2Item>(),
                CurrentOp = DtmCommon.Constant.OpAction,
            };
            wf.SetupProperty(x => x.WorkflowImp, wfImp);
            var wfOption = new WfOptions
            {
                CompensateErrorBranch = true,
            };
            wf.SetupProperty(x => x.Options, wfOption);

            return wf;
        }

        public class WfMockHttpMessageHandler : DelegatingHandler
        {
            private readonly HttpStatusCode _statusCode;
            private readonly string _content;

            public WfMockHttpMessageHandler(HttpStatusCode statusCode, string content)
            {
                this._statusCode = statusCode;
                this._content = content;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var content = new StringContent(_content);
                var resp = new HttpResponseMessage(_statusCode);
                resp.Content = content;
                return Task.FromResult(resp);
            }
        }
    }
}