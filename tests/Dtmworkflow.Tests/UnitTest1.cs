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
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                wf.NewBranch().OnRollback(async bb =>
                {
                    var rbClient = wf.NewRequest();
                    var rbResp = await rbClient.PostAsync("TransOutRevert url", content);
                    rbResp.EnsureSuccessStatusCode();
                });

                var zxClient = wf.NewRequest();
                var zxResp = await zxClient.PostAsync("TransOut url", content);
                zxResp.EnsureSuccessStatusCode();

                var respBytes = await zxResp.Content.ReadAsByteArrayAsync();
                Console.WriteLine(System.Text.Encoding.UTF8.GetString(respBytes));
                return respBytes;
            };

            wfgt.Register("wf", handler);

            await wfgt.Execute(gid, gid, System.Text.Encoding.UTF8.GetBytes(""));
        }
    }
}