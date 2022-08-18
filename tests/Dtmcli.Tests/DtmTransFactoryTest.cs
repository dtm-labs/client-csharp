using Moq;
using Xunit;

namespace Dtmcli.Tests
{
    public class DtmTransFactoryTest
    {
        [Fact]
        public void NewMsg_Should_Succeed()
        {
            var dtmClient = new Mock<IDtmClient>();
            var bbFactory = new Mock<IBranchBarrierFactory>();

            var tFactory = new DtmTransFactory(dtmClient.Object, bbFactory.Object);

            var gid = "TestMsgNormal";

            var msg = tFactory.NewMsg(gid);

            Assert.NotNull(msg);
        }

        [Fact]
        public void NewSaga_Should_Succeed()
        {
            var dtmClient = new Mock<IDtmClient>();
            var bbFactory = new Mock<IBranchBarrierFactory>();

            var tFactory = new DtmTransFactory(dtmClient.Object, bbFactory.Object);

            var gid = "TestSagaNormal";

            var saga = tFactory.NewSaga(gid);

            Assert.NotNull(saga);
        }
    }
}