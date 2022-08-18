using Moq;
using Xunit;

namespace Dtmgrpc.Tests
{
    public class DtmTransFactoryTests
    {
        [Fact]
        public void NewMsg_Should_Succeed()
        {
            var tFactory = BuildFactory();

            var gid = "TestMsgNormal";

            var msg = tFactory.NewMsgGrpc(gid);

            Assert.NotNull(msg);
        }

        [Fact]
        public void NewSaga_Should_Succeed()
        {
            var tFactory = BuildFactory();

            var gid = "TestSagaNormal";

            var saga = tFactory.NewSagaGrpc(gid);

            Assert.NotNull(saga);
        }

        [Fact]
        public void NewTcc_Should_Succeed()
        {
            var tFactory = BuildFactory();

            var gid = "TestTccNormal";

            var saga = tFactory.NewTccGrpc(gid);

            Assert.NotNull(saga);
        }

        private DtmTransFactory BuildFactory()
        {
            var dtmClient = new Mock<IDtmgRPCClient>();
            var bbFactory = new Mock<IBranchBarrierFactory>();
            var option = Microsoft.Extensions.Options.Options.Create(new DtmCommon.DtmOptions { });

            return new DtmTransFactory(option, dtmClient.Object, bbFactory.Object);

        }
    }
}
