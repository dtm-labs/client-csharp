using Apps72.Dev.Data.DbMocker;
using Apps72.Dev.Data.DbMocker.Data;
using Xunit;
using Dapper;
using System;
using System.Threading.Tasks;
using Moq;
using System.Linq;

namespace Dtmcli.Tests
{
    public class BranchBarrierTests
    {
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

            var mockBusiCall = new Mock<Func<System.Data.Common.DbTransaction, Task>>();

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

            var mockBusiCall = new Mock<Func<System.Data.Common.DbTransaction, Task>>();

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

            var mockBusiCall = new Mock<Func<System.Data.Common.DbTransaction, Task>>();

            await branchBarrier.Call(conn, mockBusiCall.Object);

            mockBusiCall.Verify(x => x.Invoke(It.IsAny<System.Data.Common.DbTransaction>()), Times.Once);
        }

        private MockDbConnection GetDbConnection() => new MockDbConnection();


    }
}