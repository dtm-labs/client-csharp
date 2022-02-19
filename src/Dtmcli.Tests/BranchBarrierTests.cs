using Apps72.Dev.Data.DbMocker;
using Apps72.Dev.Data.DbMocker.Data;
using Xunit;
using Dapper;
using System;
using System.Threading.Tasks;
using Moq;
using System.Linq;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using DtmCommon;

namespace Dtmcli.Tests
{
    public class BranchBarrierTests
    {
        private readonly IBranchBarrierFactory _factory;

        public BranchBarrierTests()
        {
            var provider = TestHelper.AddDtmCli();

            var factory = provider.GetRequiredService<IBranchBarrierFactory>();
            _factory = factory;
        }

#if NET5_0_OR_GREATER
        [Fact]
        public void CreateBranchBarrier_FromQs_Should_Succeed()
        {
            var dict = new System.Collections.Generic.Dictionary<string, StringValues>()
            {
                { "branch_id","11" },
                { "gid","1111" },
                { "op","try" },
                { "trans_type","tcc" },
            };

            var qs = new Microsoft.AspNetCore.Http.QueryCollection(dict);

            var bb = _factory.CreateBranchBarrier(qs);

            Assert.NotNull(bb);
        }

        [Fact]
        public void CreateBranchBarrier_FromQs_Should_ThrowException()
        {
            var dict = new System.Collections.Generic.Dictionary<string, StringValues>()
            {
                { "branch_id","11" },
                { "gid","1111" },
                { "op","" },
                { "trans_type","" },
            };

            var qs = new Microsoft.AspNetCore.Http.QueryCollection(dict);

            Assert.Throws<DtmException>(() => _factory.CreateBranchBarrier(qs));
        }
#endif

        [Theory]
        [InlineData("cancel", "try")]
        [InlineData("compensate", "action")]
        public async void Call_Should_Not_Trigger_When_IsNullCompensation(string op, string origin)
        {
            var branchBarrier = _factory.CreateBranchBarrier("tcc", "gid", "bid", op);

            var conn = GetDbConnection();

            // mock originAffected = 1
            conn.Mocks.When(cmd => cmd.Parameters.AsList().Select(x => x.Value).Contains(origin)).ReturnsScalar(cmd => 1);

            // mock currentAffected != 0
            conn.Mocks.When(cmd => cmd.Parameters.AsList().Select(x => x.Value).Contains(op)).ReturnsScalar(cmd => 1);

            var mockBusiCall = new Mock<Func<System.Data.Common.DbTransaction, Task<bool>>>();

            await branchBarrier.Call(conn, mockBusiCall.Object);

            mockBusiCall.Verify(x => x.Invoke(It.IsAny<System.Data.Common.DbTransaction>()), Times.Never);
        }

        [Theory]
        [InlineData("other1", "other2")]
        public async void Call_Should_Not_Trigger_When_IsDuplicateOrPend(string op, string origin)
        {
            var branchBarrier = _factory.CreateBranchBarrier("tcc", "gid", "bid", op);

            var conn = GetDbConnection();

            // mock originAffected = 0
            conn.Mocks.When(cmd => cmd.Parameters.AsList().Select(x => x.Value).Contains(origin)).ReturnsScalar(cmd => 0);

            // mock currentAffected = 0
            conn.Mocks.When(cmd => cmd.Parameters.AsList().Select(x => x.Value).Contains(op)).ReturnsScalar(cmd => 0);

            var mockBusiCall = new Mock<Func<System.Data.Common.DbTransaction, Task<bool>>>();

            await branchBarrier.Call(conn, mockBusiCall.Object);

            mockBusiCall.Verify(x => x.Invoke(It.IsAny<System.Data.Common.DbTransaction>()), Times.Never);
        }

        [Theory]
        [InlineData("cancel", "try")]
        [InlineData("compensate", "action")]
        public async void Call_Should_Trigger_When_IsNotNullCompensation_And_DuplicateOrPend(string op, string origin)
        {
            var branchBarrier = _factory.CreateBranchBarrier("tcc", "gid", "bid", op);

            var conn = GetDbConnection();

            // mock originAffected = 0
            conn.Mocks.When(cmd => cmd.Parameters.AsList().Select(x => x.Value).Contains(origin)).ReturnsScalar(cmd => 0);

            // mock currentAffected > 0
            conn.Mocks.When(cmd => cmd.Parameters.AsList().Select(x => x.Value).Contains(op)).ReturnsScalar(cmd => 1);

            var mockBusiCall = new Mock<Func<System.Data.Common.DbTransaction, Task<bool>>>();

            await branchBarrier.Call(conn, mockBusiCall.Object);

            mockBusiCall.Verify(x => x.Invoke(It.IsAny<System.Data.Common.DbTransaction>()), Times.Once);
        }

        private MockDbConnection GetDbConnection() => new MockDbConnection();

        [Fact]
        public void SetBarrierTableName_Should_Succeed()
        {
            var provider = TestHelper.AddDtmCli(tbName: "aaa.bbb");

            var factory = provider.GetRequiredService<IBranchBarrierFactory>();

            var branchBarrier = factory.CreateBranchBarrier("msg", "gid", "bid", "msg");

            Assert.Equal("aaa.bbb", branchBarrier.DtmOptions.BarrierTableName);
        }

        [Fact]
        public async void DbUtils_Should_Work_With_Cus_BarrierTable_Name()
        {
            var provider = TestHelper.AddDtmCli(tbName: "aaa.bbb");

            var dbUtils = provider.GetRequiredService<DbUtils>();

            var conn = GetDbConnection();
            conn.Mocks.When(cmd => cmd.CommandText.Contains("aaa.bbb")).ReturnsScalar(cmd => 1);
            conn.Mocks.When(cmd => !cmd.CommandText.Contains("aaa.bbb")).ReturnsScalar(cmd => 2);
            var (row, err) = await dbUtils.InsertBarrier(conn, "tt", "gid", "bid", "op", "bid", "reason");

            Assert.Equal(1, row);
        }

        [Fact]
        public async void Call_Should_Throw_Duplicated_Exception_When_QueryPrepared_At_First()
        {
            var branchBarrier = _factory.CreateBranchBarrier("msg", "gid", "bid", "msg");

            var connQ = GetDbConnection();
            connQ.Mocks.When(cmd => cmd.CommandText.Contains("insert", StringComparison.Ordinal)).ReturnsScalar(cmd => 1);
            connQ.Mocks.When(cmd => cmd.CommandText.Contains("select", StringComparison.OrdinalIgnoreCase)).ReturnsScalar(cmd => DtmCommon.Constant.Barrier.MSG_BARRIER_REASON);

            // QueryPrepared at first
            var qRes = await branchBarrier.QueryPrepared(connQ);
            Assert.Equal(DtmCommon.Constant.ResultFailure, qRes);

            var connC = GetDbConnection();
            connC.Mocks.When(cmd => cmd.Parameters.AsList().Select(x => x.Value).Contains("msg")).ReturnsScalar(cmd => 0);

            var mockBusiCall = new Mock<Func<System.Data.Common.DbTransaction, Task<bool>>>();

            // Call later
            var ex = await Assert.ThrowsAsync<DtmDuplicatedException>(async () => await branchBarrier.Call(connC, mockBusiCall.Object));
            Assert.Equal(DtmCommon.Constant.ResultDuplicated, ex.Message);
            mockBusiCall.Verify(x => x.Invoke(It.IsAny<System.Data.Common.DbTransaction>()), Times.Never);
        }
    }
}