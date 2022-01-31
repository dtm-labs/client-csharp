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

namespace Dtmcli.Tests
{
    public class BranchBarrierTests
    {
#if NET5_0_OR_GREATER
        [Fact]
        public void CreateBranchBarrier_FromQs_Should_Succeed()
        {
            var factory = new DefaultBranchBarrierFactory(NullLoggerFactory.Instance);

            var dict = new System.Collections.Generic.Dictionary<string, StringValues>()
            {
                { "branch_id","11" },
                { "gid","1111" },
                { "op","try" },
                { "trans_type","tcc" },
            };

            var qs = new Microsoft.AspNetCore.Http.QueryCollection(dict);

            var bb = factory.CreateBranchBarrier(qs);

            Assert.NotNull(bb);
        }

        [Fact]
        public void CreateBranchBarrier_FromQs_Should_ThrowException()
        {
            var factory = new DefaultBranchBarrierFactory(NullLoggerFactory.Instance);

            var dict = new System.Collections.Generic.Dictionary<string, StringValues>()
            {
                { "branch_id","11" },
                { "gid","1111" },
                { "op","" },
                { "trans_type","" },
            };

            var qs = new Microsoft.AspNetCore.Http.QueryCollection(dict);

            Assert.Throws<DtmcliException>(() => factory.CreateBranchBarrier(qs));
        }
#endif

        [Theory]
        [InlineData("cancel", "try")]
        [InlineData("compensate", "action")]
        public async void Call_Should_Not_Trigger_When_IsNullCompensation(string op, string origin)
        {
            var branchBarrier = new BranchBarrier("tcc", "gid", "bid", op);

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
            var branchBarrier = new BranchBarrier("tcc", "gid", "bid", op);

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
            var branchBarrier = new BranchBarrier("tcc", "gid", "bid", op);

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
    }
}