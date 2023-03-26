using Dtmcli;
using DtmCommon;
using Dtmgrpc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

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

            var wf = new Mock<Workflow>(httpClient.Object, grpcClient.Object, httpBb.Object);
            wf.SetupProperty(x => x.TransBase, TransBase.NewTransBase("1", "workflow", "not inited", ""));

            factory.Setup(x => x.NewWorkflow(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<bool>())).Returns(wf.Object);

            var wfgt = new WorlflowGlobalTransaction(factory.Object, NullLoggerFactory.Instance);

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

            var wf = new Mock<Workflow>(httpClient.Object, grpcClient.Object, httpBb.Object);
            wf.SetupProperty(x => x.TransBase, TransBase.NewTransBase("1", "workflow", "not inited", ""));

            factory.Setup(x => x.NewWorkflow(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<bool>())).Returns(wf.Object);

            var wfgt = new WorlflowGlobalTransaction(factory.Object, NullLoggerFactory.Instance);

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

            var wf = new Mock<Workflow>(httpClient.Object, grpcClient.Object, httpBb.Object);
            var tb = TransBase.NewTransBase("1", "workflow", "not inited", "");
            tb.Protocol = Constant.ProtocolHTTP;
            wf.SetupProperty(x => x.TransBase, tb);

            factory.Setup(x => x.NewWorkflow(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<bool>())).Returns(wf.Object);

            var wfgt = new WorlflowGlobalTransaction(factory.Object, NullLoggerFactory.Instance);

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

            var wf = new Mock<Workflow>(httpClient.Object, grpcClient.Object, httpBb.Object);
            var tb = TransBase.NewTransBase("1", "workflow", "not inited", "");
            tb.Protocol = Constant.ProtocolHTTP;
            wf.SetupProperty(x => x.TransBase, tb);

            factory.Setup(x => x.NewWorkflow(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<bool>())).Returns(wf.Object);

            var wfgt = new WorlflowGlobalTransaction(factory.Object, NullLoggerFactory.Instance);

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
        public async void Execute_Should_Return_Null_When_WfFunc2_ThrowDtmFailureException()
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

            var wf = new Mock<Workflow>(httpClient.Object, grpcClient.Object, httpBb.Object);
            var tb = TransBase.NewTransBase("1", "workflow", "not inited", "");
            tb.Protocol = Constant.ProtocolHTTP;
            wf.SetupProperty(x => x.TransBase, tb);

            factory.Setup(x => x.NewWorkflow(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<bool>())).Returns(wf.Object);

            var wfgt = new WorlflowGlobalTransaction(factory.Object, NullLoggerFactory.Instance);

            var wfName = nameof(Execute_Should_Return_Null_When_WfFunc2_ThrowDtmFailureException);
            var gid = Guid.NewGuid().ToString("N");

            var handler = new Mock<WfFunc2>();
            handler.Setup(x => x.Invoke(It.IsAny<Workflow>(), It.IsAny<byte[]>())).Throws(new DtmFailureException());

            wfgt.Register(wfName, handler.Object);
            var req = JsonSerializer.Serialize(new { userId = "1", amount = 30 });

            var res = await wfgt.Execute(wfName, gid, Encoding.UTF8.GetBytes(req), true);
            Assert.Null(res);
        }

        //[Fact]
        //public async void Rollback()
        //{
        //    var factory = new Mock<IWorkflowFactory>();
        //    var httpClient = new Mock<IDtmClient>();
        //    var grpcClient = new Mock<IDtmgRPCClient>();
        //    var httpBb = new Mock<Dtmcli.IBranchBarrierFactory>();

        //    var progressDtos = new List<DtmProgressDto>
        //    {
        //        new DtmProgressDto { Status = Constant.StatusSucceed, BranchId = "01", Op = Constant.OpAction },
        //        new DtmProgressDto { Status = Constant.StatusSucceed, BranchId = "02", Op = Constant.OpAction }
        //    };

        //    SetupPrepareWorkflow(httpClient, Constant.StatusSubmitted, "123", progressDtos);

        //    var wf = new Mock<Workflow>(httpClient.Object, grpcClient.Object, httpBb.Object);
        //    var tb = TransBase.NewTransBase("1", "workflow", "not inited", "");
        //    tb.Protocol = Constant.ProtocolHTTP;
        //    wf.SetupProperty(x => x.TransBase, tb);

        //    factory.Setup(x => x.NewWorkflow(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<bool>())).Returns(wf.Object);

        //    var wfgt = new WorlflowGlobalTransaction(factory.Object, NullLoggerFactory.Instance);

        //    var wfName = nameof(Execute_Should_Return_Null_When_WfFunc2_ThrowDtmFailureException);
        //    var gid = Guid.NewGuid().ToString("N");
            
        //    var func = new Mock<WfPhase2Func>();

        //    WfFunc2 handler = async (wf, data) => 
        //    {
        //        var handler = new WfMockHttpMessageHandler(HttpStatusCode.Conflict, "123");
        //        var content = new ByteArrayContent(data);
        //        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        //        var client = wf.NewBranch().OnRollback(func.Object).NewRequest(handler);

        //        var outResp = await client.PostAsync("URL", content);
        //        outResp.EnsureSuccessStatusCode();

        //        return await outResp.Content.ReadAsByteArrayAsync();
        //    };
        //    wfgt.Register(wfName, handler);
        //    var req = JsonSerializer.Serialize(new { userId = "1", amount = 30 });

        //    var res = await wfgt.Execute(wfName, gid, Encoding.UTF8.GetBytes(req), true);

        //    func.Verify(x => x.Invoke(It.IsAny<BranchBarrier>()), Times.Once);
        //}

        private async Task DoNewBranchRequest(Workflow wf, byte[] data, string url, HttpStatusCode statusCode, string resp)
        {
            var handler = new WfMockHttpMessageHandler(statusCode, resp);

            var content = new ByteArrayContent(data);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var client = wf.NewBranch().NewRequest(handler);

            var outResp = await client.PostAsync(url, content);
            outResp.EnsureSuccessStatusCode();
        }


        private void SetupPrepareWorkflow(Mock<IDtmClient> httpClient, string status, string result, List<DtmProgressDto> progressDtos = null)
        {
            var httpResp = new HttpResponseMessage(HttpStatusCode.OK);
            httpResp.Content = new StringContent(JsonSerializer.Serialize(
                new DtmProgressesReplyDto() 
                { 
                    Transaction = new DtmTransactionDto 
                    { 
                        Status = status, 
                        Result = Convert.ToBase64String(Encoding.UTF8.GetBytes(result)) 
                    },
                    Progresses = progressDtos
                }));
            httpClient.Setup(x => x.PrepareWorkflow(It.IsAny<DtmCommon.TransBase>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(httpResp));
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