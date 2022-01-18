using Xunit;

namespace Dtmcli.Tests
{
    public class DbSpecialTests
    {
        [Fact]
        public void TestDbSpecial()
        {
            DtmImp.DbSpecialDelegate.Instance.SetCurrentDBType("mysql");

            var mysql = DtmImp.DbSpecialDelegate.Instance.GetDBSpecial();
            Assert.Equal("xa start 'xa1'", mysql.GetXaSQL("start", "xa1"));
            Assert.Equal("insert ignore into a(f) values(@f)", mysql.GetInsertIgnoreTemplate("a(f) values(@f)", "c"));

            DtmImp.DbSpecialDelegate.Instance.SetCurrentDBType("postgres");

            var postgres = DtmImp.DbSpecialDelegate.Instance.GetDBSpecial();
            Assert.Equal("begin", postgres.GetXaSQL("start", "xa1"));
            Assert.Equal("insert into a(f) values(@f) on conflict ON CONSTRAINT c do nothing", postgres.GetInsertIgnoreTemplate("a(f) values(@f)", "c"));
        }
    }
}