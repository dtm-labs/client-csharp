using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Xunit;

namespace Dtmworkflow.IntegrationTests
{
    public class HTTPSageTest : IDisposable
    {
        private IWebHost _builder;



        [Fact]
        public async void Test_HTTP_SAGA_Succeed()
        {
            var provider = ITTestHelper.AddDtmworkflow();

            var wfgt = provider.GetRequiredService<WorlflowGlobalTransaction>();
            var wff = provider.GetRequiredService<IWorkflowFactory>();

            var gid = Guid.NewGuid().ToString("N");

            WfFunc2 handler = async (wf, data) =>
            {
                // trans out
                var content = new ByteArrayContent(data);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                wf.NewBranch().OnRollback(async bb =>
                {
                    var rbClient = wf.NewRequest();
                    var rbResp = await rbClient.PostAsync($"http://{ITTestHelper.BuisHttpUrl}/api/busi/TransOutRevert", content);
                    rbResp.EnsureSuccessStatusCode();
                });

                var outClient = wf.NewRequest();
                var outResp = await outClient.PostAsync($"http://{ITTestHelper.BuisHttpUrl}/api/busi/TransOut", content);
                outResp.EnsureSuccessStatusCode();

                var outRespBytes = await outResp.Content.ReadAsByteArrayAsync();
                Console.WriteLine(System.Text.Encoding.UTF8.GetString(outRespBytes));

                // trans in
                wf.NewBranch().OnRollback(async bb =>
                {
                    var rbClient = wf.NewRequest();
                    var rbResp = await rbClient.PostAsync($"http://{ITTestHelper.BuisHttpUrl}/api/busi/TransInRevert", content);
                    rbResp.EnsureSuccessStatusCode();
                });

                var inClient = wf.NewRequest();
                var inResp = await inClient.PostAsync($"http://{ITTestHelper.BuisHttpUrl}/api/busi/TransIn", content);
                inResp.EnsureSuccessStatusCode();

                var inRespBytes = await inResp.Content.ReadAsByteArrayAsync();
                Console.WriteLine(System.Text.Encoding.UTF8.GetString(inRespBytes));

                return null;
            };

            wfgt.Register(gid, handler);
            var req = System.Text.Json.JsonSerializer.Serialize(new { Amount = 30, TransOutResult = "", TransInResult = "" });
            var res = await wfgt.Execute(
                gid,
                gid,
                System.Text.Encoding.UTF8.GetBytes(req),
                true);

            Console.WriteLine(res == null ? "" : System.Text.Encoding.UTF8.GetString(res));

            await Task.Delay(2000);
            var status = await ITTestHelper.GetTranStatus(gid);
            Assert.Equal("succeed", status);
        }

        [Fact]
        public async void Test_HTTP_SAGA_Rollback()
        {
            GivenRunningOn("http://localhost:6003");

            var provider = _builder.Services;

            var wfgt = provider.GetRequiredService<WorlflowGlobalTransaction>();
            var wff = provider.GetRequiredService<IWorkflowFactory>();

            var gid = Guid.NewGuid().ToString("N");

            WfFunc2 handler = async (wf, data) =>
            {
                // trans out
                var content = new ByteArrayContent(data);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                wf.NewBranch().OnRollback(async bb =>
                {
                    var rbClient = wf.NewRequest();
                    var rbResp = await rbClient.PostAsync($"http://{ITTestHelper.BuisHttpUrl}/api/busi/TransOutRevert", content);
                    rbResp.EnsureSuccessStatusCode();
                });

                var outClient = wf.NewRequest();
                var outResp = await outClient.PostAsync($"http://{ITTestHelper.BuisHttpUrl}/api/busi/TransOut", content);
                outResp.EnsureSuccessStatusCode();

                var outRespBytes = await outResp.Content.ReadAsByteArrayAsync();
                Console.WriteLine(System.Text.Encoding.UTF8.GetString(outRespBytes));

                // trans in
                wf.NewBranch().OnRollback(async bb =>
                {
                    var rbClient = wf.NewRequest();
                    var rbResp = await rbClient.PostAsync($"http://{ITTestHelper.BuisHttpUrl}/api/busi/TransInRevert", content);
                    rbResp.EnsureSuccessStatusCode();
                });

                var inClient = wf.NewRequest();
                var inResp = await inClient.PostAsync($"http://{ITTestHelper.BuisHttpUrl}/api/busi/TransIn", content);
                inResp.EnsureSuccessStatusCode();

                var inRespBytes = await inResp.Content.ReadAsByteArrayAsync();
                Console.WriteLine(System.Text.Encoding.UTF8.GetString(inRespBytes));

                return null;
            };

            wfgt.Register(gid, handler);
            var req = System.Text.Json.JsonSerializer.Serialize(new { Amount = 30, TransOutResult = "", TransInResult = "ERROR" });

            try
            {
                var res = await wfgt.Execute(
                   gid,
                   gid,
                   System.Text.Encoding.UTF8.GetBytes(req),
                   true);

                Console.WriteLine(res == null ? "" : System.Text.Encoding.UTF8.GetString(res));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            await Task.Delay(30000);
            var status = await ITTestHelper.GetTranStatus(gid);
            Assert.NotEqual("succeed", status);
        }


        public void GivenRunningOn(string url)
        {
            _builder = new WebHostBuilder()
                .UseUrls(url)
                .UseKestrel()
                .UseContentRoot(System.IO.Directory.GetCurrentDirectory())
                .ConfigureServices(s => 
                {
                    s.AddDtmWorkflow(x => 
                    {
                        x.DtmUrl = "http://localhost:36789";
                        x.DtmGrpcUrl = "http://localhost:36790";
                    });
                })
                .Configure(app => 
                {
                    app.Run(async context => 
                    {
                        if (context.Request.Path.Value == $"/api/busi/workflow/resume")
                        {
                            var wfgt = app.ApplicationServices.GetRequiredService<Dtmworkflow.WorlflowGlobalTransaction>();

                            using (var reader = new StreamReader(context.Request.Body))
                            {
                                var body = await reader.ReadToEndAsync();
                                var bytes = System.Text.Encoding.UTF8.GetBytes(body);
                                await wfgt.ExecuteByQS(context.Request.Query, bytes, "");
                                await context.Response.WriteAsync("");
                            }
                        }
                    });
                })
                .Build();


            _builder.Start();
        }

        public void Dispose()
        {
            _builder?.Dispose();
        }
    }

}