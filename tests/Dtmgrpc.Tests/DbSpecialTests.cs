using DtmCommon;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dtmgrpc.Tests
{
    public class DbSpecialTests
    {
        [Fact]
        public void Test_Default_DbSpecial()
        {
            var dtm = "http://localhost:36790";
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDtmGrpc(x =>
            {
                x.DtmGrpcUrl = dtm;
            });

            var provider = services.BuildServiceProvider();
            var dbSpecialDelegate = provider.GetRequiredService<DbSpecialDelegate>();

            var special = dbSpecialDelegate.GetDbSpecial();

            Assert.IsType<MysqlDBSpecial>(special);
            Assert.Equal("xa start 'xa1'", special.GetXaSQL("start", "xa1"));
            Assert.Equal("insert ignore into a(f) values(@f)", special.GetInsertIgnoreTemplate("a(f) values(@f)", "c"));
        }

        [Fact]
        public void Test_PgSQL_DbSpecial()
        {
            var dtm = "http://localhost:36790";
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDtmGrpc(x =>
            {
                x.DtmGrpcUrl = dtm;
                x.DBType = "postgres";
            });

            var provider = services.BuildServiceProvider();
            var dbSpecialDelegate = provider.GetRequiredService<DbSpecialDelegate>();

            var special = dbSpecialDelegate.GetDbSpecial();

            Assert.IsType<PostgresDBSpecial>(special);
            Assert.Equal("begin", special.GetXaSQL("start", "xa1"));
            Assert.Equal(string.Empty, special.GetXaSQL("aa", "bb"));
            Assert.Equal("insert into a(f) values(@f) on conflict ON CONSTRAINT c do nothing", special.GetInsertIgnoreTemplate("a(f) values(@f)", "c"));
            Assert.Equal("insert into a(f) values(@f)", special.GetPlaceHoldSQL("insert into a(f) values(@f)"));
        }

        [Fact]
        public void Test_MsSQL_DbSpecial()
        {
            var dtm = "http://localhost:36790";
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDtmGrpc(x =>
            {
                x.DtmGrpcUrl = dtm;
                x.DBType = "sqlserver";
            });

            var provider = services.BuildServiceProvider();
            var dbSpecialDelegate = provider.GetRequiredService<DbSpecialDelegate>();

            var special = dbSpecialDelegate.GetDbSpecial();

            Assert.IsType<SqlServerDBSpecial>(special);
            Assert.Equal("insert into a(f) values(@f)", special.GetInsertIgnoreTemplate("a(f) values(@f)", "c"));
            Assert.Equal("insert into a(f) values(@f)", special.GetPlaceHoldSQL("insert into a(f) values(@f)"));
            Assert.Throws<DtmException>(() => special.GetXaSQL("", ""));
        }

        [Fact]
        public void Test_Other_DbSpecial()
        {
            var dtm = "http://localhost:36790";
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDtmGrpc(x =>
            {
                x.DtmGrpcUrl = dtm;
                x.DBType = "other";
            });

            var provider = services.BuildServiceProvider();

            var ex = Assert.Throws<DtmException>(()=> provider.GetRequiredService<DbSpecialDelegate>());
            Assert.Equal("unknown db type 'other'", ex.Message);
        }
    }
}
