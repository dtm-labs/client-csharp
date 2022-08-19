using Dtmgrpc.Driver;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dtmgrpc.Tests.Driver
{
    public class OtherDtmDriverTests
    {
        public class OtherDtmDriver : IDtmDriver
        {
            public string GetName() => "other";

            public (string server, string serviceName, string method, string error) ParseServerMethod(string url)
                => ("localhost:11111", "svc", "mdethod", "");

            public void RegisterGrpcResolver()
            {
            }

            public void RegisterGrpcService(string target, string endpoint)
            {
            }
        }

        [Fact]
        public void AddDtmGrpc_Should_Get_Driver()
        {
            var dtm = "http://localhost:36790";
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDtmGrpc(x =>
            {
                x.DtmGrpcUrl = dtm;
            });

            services.AddSingleton<IDtmDriver, OtherDtmDriver>();
            var provider = services.BuildServiceProvider();

            var driver = provider.GetRequiredService<IDtmDriver>();

            Assert.IsType<OtherDtmDriver>(driver);
        }
    }
}
