using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;

namespace Dtmworkflow.Tests
{
    public class UnitTest1
    {
        [Fact]
        public async void Test1()
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
                var content = new ByteArrayContent(data);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                wf.NewBranch().OnRollback(async bb =>
                {
                    var rbClient = wf.NewRequest();
                    var rbResp = await rbClient.PostAsync("http://localhost:9090/api/TransOutRevert", content);
                    rbResp.EnsureSuccessStatusCode();
                });

                var zxClient = wf.NewRequest();
                var zxResp = await zxClient.PostAsync("http://localhost:9090/api/TransOut", content);
                zxResp.EnsureSuccessStatusCode();

                var respBytes = await zxResp.Content.ReadAsByteArrayAsync();
                Console.WriteLine(System.Text.Encoding.UTF8.GetString(respBytes));
                return respBytes;
            };

            wfgt.Register(gid, handler);
            var req = System.Text.Json.JsonSerializer.Serialize(new { userId = "1", amount = 30 });
            var res = await wfgt.Execute(gid, gid, System.Text.Encoding.UTF8.GetBytes(req), "", true);
            Console.WriteLine(System.Text.Encoding.UTF8.GetString(res));
            Assert.True(true);
        }
    }
}