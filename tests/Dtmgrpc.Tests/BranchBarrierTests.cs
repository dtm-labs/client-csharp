using Apps72.Dev.Data.DbMocker;
using Dapper;
using DtmCommon;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Dtmgrpc.Tests
{
    public class BranchBarrierTests
    {
        private readonly IBranchBarrierFactory _factory;

        public BranchBarrierTests()
        {
            var dtm = "http://localhost:36790";
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDtmGrpc(x =>
            {
                x.DtmGrpcUrl = dtm;
            });

            var provider = services.BuildServiceProvider();

            var factory = provider.GetRequiredService<IBranchBarrierFactory>();
            _factory = factory;
        }

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
            var dtm = "http://localhost:36790";
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDtmGrpc(x =>
            {
                x.DtmGrpcUrl = dtm;
                x.BarrierTableName = "aaa.bbb";
            });

            var provider = services.BuildServiceProvider();

            var factory = provider.GetRequiredService<IBranchBarrierFactory>();

            var branchBarrier = factory.CreateBranchBarrier("msg", "gid", "bid", "msg");

            Assert.Equal("aaa.bbb", branchBarrier.DtmOptions.BarrierTableName);
        }

        [Fact]
        public async void DbUtils_Should_Work_With_Cus_BarrierTable_Name()
        {
            var dtm = "http://localhost:36790";
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDtmGrpc(x =>
            {
                x.DtmGrpcUrl = dtm;
                x.BarrierTableName = "aaa.bbb";
            });

            var provider = services.BuildServiceProvider();
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
            connQ.Mocks.When(cmd => cmd.CommandText.Contains("select", StringComparison.OrdinalIgnoreCase)).ReturnsScalar(cmd => Constant.Barrier.MSG_BARRIER_REASON);

            // QueryPrepared at first
            var qRes = await branchBarrier.QueryPrepared(connQ);
            Assert.Equal(Constant.ResultFailure, qRes);

            var connC = GetDbConnection();
            connC.Mocks.When(cmd => cmd.Parameters.AsList().Select(x => x.Value).Contains("msg")).ReturnsScalar(cmd => 0);

            var mockBusiCall = new Mock<Func<System.Data.Common.DbTransaction, Task<bool>>>();

            // Call later
            var ex = await Assert.ThrowsAsync<DtmDuplicatedException>(async () => await branchBarrier.Call(connC, mockBusiCall.Object));
            Assert.Equal(Constant.ResultDuplicated, ex.Message);
            mockBusiCall.Verify(x => x.Invoke(It.IsAny<System.Data.Common.DbTransaction>()), Times.Never);
        }

        [Fact]
        public void CreateBranchBarrier_FromContext_Should_Succeed()
        {
            var reqHeader = new Grpc.Core.Metadata();
            reqHeader.Add(Constant.Md.TransType, "tcc");
            reqHeader.Add(Constant.Md.Op, "try");
            reqHeader.Add(Constant.Md.Gid, "1111");
            reqHeader.Add(Constant.Md.BranchId, "11");

            var context = new CusServerCallContext(reqHeader);

            var branchBarrier = _factory.CreateBranchBarrier(context);

            Assert.NotNull(branchBarrier);
        }

        [Fact]
        public void CreateBranchBarrier_FromContext_Should_ThrowException()
        {
            var reqHeader = new Grpc.Core.Metadata();
            reqHeader.Add(Constant.Md.TransType, "");
            reqHeader.Add(Constant.Md.Op, "");
            reqHeader.Add(Constant.Md.Gid, "1111");
            reqHeader.Add(Constant.Md.BranchId, "11");

            var context = new CusServerCallContext(reqHeader);

            Assert.Throws<DtmException>(() => _factory.CreateBranchBarrier(context));
        }
        
        [Fact]
        public async void Should_Throw_If_Sql_Execution_Failed()
        {
            const string dbWrongMessage = "Something wrong with the DB...";
            
            var branchBarrier = _factory.CreateBranchBarrier("msg", "gid", "00", "msg");

            var conn = GetDbConnection();

            conn.Mocks.When(cmd => cmd.CommandText.Contains("insert", StringComparison.Ordinal))
                .ThrowsException(new Exception(dbWrongMessage));

            var ex = await Assert.ThrowsAnyAsync<Exception>(() =>
                branchBarrier.Call(conn, transaction => Task.CompletedTask));
            
            Assert.Equal(dbWrongMessage, ex.Message);
        }
    }
}
