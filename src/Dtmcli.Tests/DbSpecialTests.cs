using DtmCommon;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dtmcli.Tests
{
    public class DbSpecialTests
    {
        [Fact]
        public void Test_Default_DbSpecial()
        {
            var provider = TestHelper.AddDtmCli();
            var dbSpecialDelegate = provider.GetRequiredService<DbSpecialDelegate>();

            var special = dbSpecialDelegate.GetDbSpecial();

            Assert.IsType<MysqlDBSpecial>(special);
            Assert.Equal("xa start 'xa1'", special.GetXaSQL("start", "xa1"));
            Assert.Equal("insert ignore into a(f) values(@f)", special.GetInsertIgnoreTemplate("a(f) values(@f)", "c"));
        }

        [Fact]
        public void Test_PgSQL_DbSpecial()
        {
            var provider = TestHelper.AddDtmCli(db: "postgres");
            var dbSpecialDelegate = provider.GetRequiredService<DbSpecialDelegate>();

            var special = dbSpecialDelegate.GetDbSpecial();

            Assert.IsType<PostgresDBSpecial>(special);
            Assert.Equal("begin", special.GetXaSQL("start", "xa1"));
            Assert.Equal("insert into a(f) values(@f) on conflict ON CONSTRAINT c do nothing", special.GetInsertIgnoreTemplate("a(f) values(@f)", "c"));
        }

        [Fact]
        public void Test_MsSQL_DbSpecial()
        {
            var provider = TestHelper.AddDtmCli(db: "sqlserver");
            var dbSpecialDelegate = provider.GetRequiredService<DbSpecialDelegate>();

            var special = dbSpecialDelegate.GetDbSpecial();

            Assert.IsType<SqlServerDBSpecial>(special);
            Assert.Equal("insert into a(f) values(@f)", special.GetInsertIgnoreTemplate("a(f) values(@f)", "c"));
            Assert.Throws<DtmException>(() => special.GetXaSQL("", ""));
        }

        [Fact]
        public void Test_Other_DbSpecial()
        {
            var provider = TestHelper.AddDtmCli(db: "other");

            var ex = Assert.Throws<DtmException>(() => provider.GetRequiredService<DbSpecialDelegate>());
            Assert.Equal("unknown db type 'other'", ex.Message);
        }
    }
}