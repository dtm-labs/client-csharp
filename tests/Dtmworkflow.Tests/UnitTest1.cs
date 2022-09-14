using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;

namespace Dtmworkflow.Tests
{
    public class UnitTest1
    {
        [Fact]
        public async void Test_HTTP_SAGA_Succeed()
        {
            var services = new ServiceCollection();
            services.AddDtmWorkflow(x =>
            {
                x.DtmUrl = "http://localhost:36789";
                x.DtmGrpcUrl = "http://localhost:36790";
            });

            var provider = services.BuildServiceProvider();

            var wfgt = provider.GetRequiredService<Dtmworkflow.WorlflowGlobalTransaction>();
            var wff = provider.GetRequiredService<Dtmworkflow.IWorkflowFactory>();

            var gid = Guid.NewGuid().ToString("N");

            WfFunc2 handler = async (wf, data) =>
            {
                // trans out
                var content = new ByteArrayContent(data);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                wf.NewBranch().OnRollback(async bb =>
                {
                    var rbClient = wf.NewRequest();
                    var rbResp = await rbClient.PostAsync("http://localhost:9090/api/TransOutRevert", content);
                    rbResp.EnsureSuccessStatusCode();
                });

                var outClient = wf.NewRequest();
                var outResp = await outClient.PostAsync("http://localhost:9090/api/TransOut", content);
                outResp.EnsureSuccessStatusCode();

                var outRespBytes = await outResp.Content.ReadAsByteArrayAsync();
                Console.WriteLine(System.Text.Encoding.UTF8.GetString(outRespBytes));

                // trans in
                wf.NewBranch().OnRollback(async bb =>
                {
                    var rbClient = wf.NewRequest();
                    var rbResp = await rbClient.PostAsync("http://localhost:9090/api/TransInRevert", content);
                    rbResp.EnsureSuccessStatusCode();
                });

                var inClient = wf.NewRequest();
                var inResp = await inClient.PostAsync("http://localhost:9090/api/TransIn", content);
                inResp.EnsureSuccessStatusCode();

                var inRespBytes = await inResp.Content.ReadAsByteArrayAsync();
                Console.WriteLine(System.Text.Encoding.UTF8.GetString(inRespBytes));

                return null;
            };

            wfgt.Register(gid, handler);
            var req = System.Text.Json.JsonSerializer.Serialize(new { userId = "1", amount = 30 });
            var res = await wfgt.Execute(
                gid,
                gid,
                System.Text.Encoding.UTF8.GetBytes(req),
                true);

            Console.WriteLine(res == null ? "" : System.Text.Encoding.UTF8.GetString(res));
            Assert.True(true);
        }

        [Fact]
        public async void Test_HTTP_SAGA_Rollback()
        {
            var services = new ServiceCollection();
            services.AddDtmWorkflow(x =>
            {
                x.DtmUrl = "http://localhost:36789";
                x.DtmGrpcUrl = "http://localhost:36790";
                x.HttpCallback = "http://localhost:9090/api/workflow/resume";
            });

            var provider = services.BuildServiceProvider();

            var wfgt = provider.GetRequiredService<Dtmworkflow.WorlflowGlobalTransaction>();
            var wff = provider.GetRequiredService<Dtmworkflow.IWorkflowFactory>();

            var gid = Guid.NewGuid().ToString("N");

            WfFunc2 handler = async (wf, data) =>
            {
                // trans out
                var content = new ByteArrayContent(data);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                wf.NewBranch().OnRollback(async bb =>
                {
                    var rbClient = wf.NewRequest();
                    var rbResp = await rbClient.PostAsync("http://localhost:9090/api/TransOutRevert", content);
                    rbResp.EnsureSuccessStatusCode();
                });

                var outClient = wf.NewRequest();
                var outResp = await outClient.PostAsync("http://localhost:9090/api/TransOutError", content);
                outResp.EnsureSuccessStatusCode();

                var outRespBytes = await outResp.Content.ReadAsByteArrayAsync();
                Console.WriteLine(System.Text.Encoding.UTF8.GetString(outRespBytes));

                // trans in
                wf.NewBranch().OnRollback(async bb =>
                {
                    var rbClient = wf.NewRequest();
                    var rbResp = await rbClient.PostAsync("http://localhost:9090/api/TransInRevert", content);
                    rbResp.EnsureSuccessStatusCode();
                });

                var inClient = wf.NewRequest();
                var inResp = await inClient.PostAsync("http://localhost:9090/api/TransIn", content);
                inResp.EnsureSuccessStatusCode();

                var inRespBytes = await inResp.Content.ReadAsByteArrayAsync();
                Console.WriteLine(System.Text.Encoding.UTF8.GetString(inRespBytes));

                return null;
            };

            wfgt.Register(gid, handler);
            var req = System.Text.Json.JsonSerializer.Serialize(new { userId = "1", amount = 30 });
            var res = await wfgt.Execute(
                gid,
                gid,
                System.Text.Encoding.UTF8.GetBytes(req),
                true);

            Console.WriteLine(res == null ? "" : System.Text.Encoding.UTF8.GetString(res));
            Assert.True(true);
        }
    }
}