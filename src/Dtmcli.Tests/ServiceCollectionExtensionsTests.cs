using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using Xunit;

namespace Dtmcli.Tests
{
    public class ServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddDtmcli_With_Action_Should_Succeed()
        {
            var dtmUrl = "http://localhost:36789";

            var services = new ServiceCollection();
            services.AddDtmcli(x =>
            {
                x.DtmUrl = dtmUrl;
            });

            var provider = services.BuildServiceProvider();

            var dtmOptionsAccs = provider.GetService<IOptions<DtmOptions>>();
            var dtmOptions = dtmOptionsAccs.Value;
            Assert.Equal(dtmUrl, dtmOptions.DtmUrl);

            var dtmClient = provider.GetRequiredService<IDtmClient>();
            Assert.NotNull(dtmClient);
        }

        [Fact]
        public void AddDtmcli_Without_Action_Should_Throw_Exception()
        {
            var services = new ServiceCollection();

            Assert.Throws<System.ArgumentNullException>(() => services.AddDtmcli(null));
        }

        [Fact]
        public void AddDtmcli_With_IConfiguration_Should_Succeed()
        {
            var dtmUrl = "http://localhost:36789";

            var dict = new Dictionary<string, string>
            {
               { "dtm:DtmUrl", dtmUrl },
            };

            var config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();

            var services = new ServiceCollection();
            services.AddDtmcli(config, "dtm");

            var provider = services.BuildServiceProvider();

            var dtmOptionsAccs = provider.GetService<IOptions<DtmOptions>>();
            var dtmOptions = dtmOptionsAccs.Value;
            Assert.Equal(dtmUrl, dtmOptions.DtmUrl);

            var dtmClient = provider.GetRequiredService<IDtmClient>();
            Assert.NotNull(dtmClient);
        }
    }
}