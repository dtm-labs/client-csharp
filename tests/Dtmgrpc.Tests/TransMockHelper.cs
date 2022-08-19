using DtmCommon;
using Dtmgrpc.DtmGImp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dtmgrpc.Tests
{
    public class TransMockHelper
    {
        public static void MockTransCallDtm(Mock<IDtmgRPCClient> mock, string op, bool isEx)
        {
            var setup = mock
                .Setup(x => x.DtmGrpcCall(It.IsAny<TransBase>(), op));

            if (isEx)
            {
                setup.Throws(new Exception(""));
            }
            else
            {
                setup.Returns(Task.CompletedTask);
            }
        }

        public static void MockRegisterBranch(Mock<IDtmgRPCClient> mock, bool isEx)
        {
            var setup = mock
                .Setup(x => x.RegisterBranch(
                    It.IsAny<TransBase>(),
                    It.IsAny<string>(),
                    It.IsAny<ByteString>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<string>()));

            if (isEx)
            {
                setup.Throws(new Exception(""));
            }
            else
            {
                setup.Returns(Task.CompletedTask);
            }
        }

        public static void MockTransRequestBranch(Mock<IDtmgRPCClient> mock, bool isEx)
        {
            var setup = mock
                .Setup(x => x.InvokeBranch<Empty, Empty>(
                    It.IsAny<TransBase>(),
                    It.IsAny<Empty>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()));

            if (isEx)
            {
                setup.Throws(new Exception(""));
            }
            else
            {
                setup.Returns(Task.FromResult(new Empty()));
            }
        }
    }
}
