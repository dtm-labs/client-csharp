using DtmCommon;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using Xunit;

namespace Dtmgrpc.Tests
{
    public class ServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddDtmGrpc_With_Action_Should_Succeed()
        {
            var dtmUrl = "http://localhost:36789";

            var services = new ServiceCollection();
            services.AddDtmGrpc(x =>
            {
                x.DtmUrl = dtmUrl;
                x.DtmGrpcUrl = dtmUrl;
                x.DtmTimeout = 8000;
                x.BranchTimeout = 8000;
            });

            var provider = services.BuildServiceProvider();

            var dtmOptionsAccs = provider.GetService<IOptions<DtmOptions>>();
            var dtmOptions = dtmOptionsAccs.Value;
            Assert.Equal(dtmUrl, dtmOptions.DtmGrpcUrl);

            var dtmClient = provider.GetRequiredService<IDtmgRPCClient>();
            Assert.NotNull(dtmClient);
        }

        [Fact]
        public void AddDtmGrpc_Without_Action_Should_Throw_Exception()
        {
            var services = new ServiceCollection();

            Assert.Throws<System.ArgumentNullException>(() => services.AddDtmGrpc(null));
        }

        [Fact]
        public void AddDtmGrpc_With_IConfiguration_Should_Succeed()
        {
            var dtmUrl = "http://localhost:36790";

            var dict = new Dictionary<string, string>
            {
               { "dtm:DtmUrl", dtmUrl },
               { "dtm:DtmGrpcUrl", dtmUrl },
               { "dtm:DtmTimeout", "1000" },
               { "dtm:BranchTimeout", "8000" },
            };

            var config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();

            var services = new ServiceCollection();
            services.AddDtmGrpc(config, "dtm");

            var provider = services.BuildServiceProvider();

            var dtmOptionsAccs = provider.GetService<IOptions<DtmOptions>>();
            var dtmOptions = dtmOptionsAccs.Value;
            Assert.Equal(dtmUrl, dtmOptions.DtmUrl);
            Assert.Equal(dtmUrl, dtmOptions.DtmGrpcUrl);

            var dtmClient = provider.GetRequiredService<IDtmgRPCClient>();
            Assert.NotNull(dtmClient);
        }

        [Fact]
        public void AddDtmGrpc_With_IConfiguration_And_Empty_Option_Should_Succeed()
        {
            var dtmUrl = "http://localhost:36790";

            var dict = new Dictionary<string, string>
            {
               { "dtmx:DtmGrpcUrl", dtmUrl },
            };

            var config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();

            var services = new ServiceCollection();
            services.AddDtmGrpc(config, "dtm");

            var provider = services.BuildServiceProvider();

            var dtmOptionsAccs = provider.GetService<IOptions<DtmOptions>>();
            var dtmOptions = dtmOptionsAccs.Value;
            Assert.NotEqual(dtmUrl, dtmOptions.DtmUrl);
        }
    }
}
