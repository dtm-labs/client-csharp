using DtmCommon;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
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
                x.DtmTimeout = 8000;
                x.BranchTimeout = 8000;
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
        public async void AddDtmcli_With_IConfiguration_Should_Succeed()
        {
            var dtmUrl = "http://localhost:36789";

            var dict = new Dictionary<string, string>
            {
               { "dtm:DtmUrl", dtmUrl },
               { "dtm:DtmTimeout", "1000" },
               { "dtm:BranchTimeout", "8000" },
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
            
            // for real test
            await Assert.ThrowsAnyAsync<System.Exception>(async () => await dtmClient.GenGid(default));
            await dtmClient.TransRequestBranch(new TransBase(), System.Net.Http.HttpMethod.Get, null, "", "", "https://www.baidu.com", default);
        }

        [Fact]
        public void AddDtmcli_With_IConfiguration_And_Empty_Option_Should_Succeed()
        {
            var dtmUrl = "http://localhost:36789";

            var dict = new Dictionary<string, string>
            {
               { "dtmx:DtmUrl", dtmUrl },
            };

            var config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();

            var services = new ServiceCollection();
            services.AddDtmcli(config, "dtm");

            var provider = services.BuildServiceProvider();

            var dtmOptionsAccs = provider.GetService<IOptions<DtmOptions>>();
            var dtmOptions = dtmOptionsAccs.Value;
            Assert.NotEqual(dtmUrl, dtmOptions.DtmUrl);
        }


        [Fact]
        public void AddDtmBarrier_Without_Action_Should_Throw_Exception()
        {
            var services = new ServiceCollection();

            Assert.Throws<System.ArgumentNullException>(() => services.AddDtmBarrier(null));
        }


        [Fact]
        public void AddDtmBarrier_With_Action_Should_Succeed()
        {
            var services = new ServiceCollection();
            services.AddDtmBarrier(x =>
            {
                x.SqlDbType = "mysql";
                x.BarrierSqlTableName = "dtm_barrier.barrier2";
            });

            var provider = services.BuildServiceProvider();

            var dtmOptionsAccs = provider.GetService<IOptions<DtmOptions>>();
            var dtmOptions = dtmOptionsAccs.Value;
            Assert.Equal("mysql", dtmOptions.SqlDbType);
            Assert.Equal("dtm_barrier.barrier2", dtmOptions.BarrierSqlTableName);

            var dtmClient = provider.GetService<IDtmClient>();
            Assert.Null(dtmClient);

            var barrierFactory = provider.GetRequiredService<IBranchBarrierFactory>();
            Assert.NotNull(barrierFactory);

            var specials = provider.GetServices<IDbSpecial>();
            Assert.Equal(3, specials.ToList().Count);
        }

        [Fact]
        public void AddDtmBarrier_With_IConfiguration_Should_Succeed()
        {
            var dict = new Dictionary<string, string>
            {
               { "dtm:SqlDbType", "mysql" },
               { "dtm:BarrierSqlTableName", "dtm_barrier.barrier2" },
            };

            var config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();

            var services = new ServiceCollection();
            services.AddDtmBarrier(config, "dtm");

            var provider = services.BuildServiceProvider();

            var dtmOptionsAccs = provider.GetService<IOptions<DtmOptions>>();
            var dtmOptions = dtmOptionsAccs.Value;
            Assert.Equal("mysql", dtmOptions.SqlDbType);
            Assert.Equal("dtm_barrier.barrier2", dtmOptions.BarrierSqlTableName);

            var dtmClient = provider.GetService<IDtmClient>();
            Assert.Null(dtmClient);

            var barrierFactory = provider.GetRequiredService<IBranchBarrierFactory>();
            Assert.NotNull(barrierFactory);

            var specials = provider.GetServices<IDbSpecial>();
            Assert.Equal(3, specials.ToList().Count);
        }
    }
}