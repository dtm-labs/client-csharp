using Dtmgrpc.Driver;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dtmgrpc.Tests.Driver
{
    public class DefaultDtmDriverTests
    {
        [Fact]
        public void ParseServerMethod_Should_Succeed()
        {
            var d = new DefaultDtmDriver();

            var (server, serviceName, method, error) = d.ParseServerMethod("localhost:9999/dtmgimp.Dtm/Prepare");

            Assert.Equal("localhost:9999", server);
            Assert.Equal("dtmgimp.Dtm", serviceName);
            Assert.Equal("Prepare", method);
            Assert.Empty(error);
        }

        [Fact]
        public void ParseServerMethod_Should_Fail()
        {
            var d = new DefaultDtmDriver();

            var (server, serviceName, method, error) = d.ParseServerMethod("http://localhost:9999/Prepare");

            Assert.Empty(server);
            Assert.Empty(serviceName);
            Assert.Empty(method);
            Assert.NotEmpty(error);

            (server, serviceName, method, error) = d.ParseServerMethod("localhost:9999/Prepare");

            Assert.Empty(server);
            Assert.Empty(serviceName);
            Assert.Empty(method);
            Assert.NotEmpty(error);
        }

        [Fact]
        public void ParseServerMethod_Should_Fail_When_Url_IsNull()
        {
            var d = new DefaultDtmDriver();

            var (server, serviceName, method, error) = d.ParseServerMethod(null);

            Assert.Empty(server);
            Assert.Empty(serviceName);
            Assert.Empty(method);
            Assert.NotEmpty(error);
        }

        [Fact]
        public void GetName_Should_Succeed()
        {
            var d = new DefaultDtmDriver();
            Assert.Equal("default", d.GetName());
        }

        [Fact]
        public void AddDtmGrpc_Should_Get_Default_Driver()
        {
            var dtm = "http://localhost:36790";
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDtmGrpc(x =>
            {
                x.DtmGrpcUrl = dtm;
            });

            var provider = services.BuildServiceProvider();

            var driver = provider.GetRequiredService<IDtmDriver>();

            Assert.IsType<DefaultDtmDriver>(driver);
        }
    }
}
