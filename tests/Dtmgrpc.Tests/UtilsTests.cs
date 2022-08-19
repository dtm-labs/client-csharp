using DtmCommon;
using Grpc.Core;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Dtmgrpc.Tests
{
    public class UtilsTests
    {
        private static DtmFailureException DtmFailure = new DtmFailureException();
        private static DtmOngingException DtmOnging = new DtmOngingException();

        [Fact]
        public void DtmError2GrpcError_Should_Throw_Aborted_RpcException()
        {
            var ex = Assert.Throws<RpcException>(()=> DtmGImp.Utils.DtmError2GrpcError(DtmFailure));
            Assert.Equal(StatusCode.Aborted, ex.StatusCode);
        }

        [Fact]
        public void DtmError2GrpcError_Should_Throw_FailedPrecondition_RpcException()
        {
            var ex = Assert.Throws<RpcException>(() => DtmGImp.Utils.DtmError2GrpcError(DtmOnging));
            Assert.Equal(StatusCode.FailedPrecondition, ex.StatusCode);
        }

        [Fact]
        public void DtmError2GrpcError_Should_Throw_Unknown_RpcException()
        {
            var ex = Assert.Throws<RpcException>(() => DtmGImp.Utils.DtmError2GrpcError(new System.ArgumentNullException()));
            Assert.Equal(StatusCode.Unknown, ex.StatusCode);
        }

        [Theory]
        [InlineData(StatusCode.Aborted, "ONGOING")]
        [InlineData(StatusCode.FailedPrecondition, "other")]
        public void GrpcError2DtmError_Should_Be_DtmOngingException(StatusCode code, string msg)
        {
            var rpcEx = new RpcException(new Status(code, msg), msg);
            var ex = DtmGImp.Utils.GrpcError2DtmError(rpcEx);
            Assert.IsType<DtmOngingException>(ex);
        }

        [Fact]
        public void GrpcError2DtmError_Should_Be_DtmFailureException()
        {
            var rpcEx = new RpcException(new Status(StatusCode.Aborted, "Other"));
            var ex = DtmGImp.Utils.GrpcError2DtmError(rpcEx);
            Assert.IsType<DtmFailureException>(ex);
        }

        [Fact]
        public void GrpcError2DtmError_Should_Be_RawException()
        {
            var rpcEx = new System.ArgumentNullException();
            var ex = DtmGImp.Utils.GrpcError2DtmError(rpcEx);
            Assert.IsType<System.ArgumentNullException>(ex);
        }

        [Fact]
        public void String2DtmError_Should_Succeed()
        {
            var fEx = DtmGImp.Utils.String2DtmError(Constant.ResultFailure);
            Assert.IsType<DtmFailureException>(fEx);

            var oEx = DtmGImp.Utils.String2DtmError(Constant.ResultOngoing);
            Assert.IsType<DtmOngingException>(oEx);

            var nullEx = DtmGImp.Utils.String2DtmError(Constant.ResultSuccess);
            Assert.Null(nullEx);

            nullEx = DtmGImp.Utils.String2DtmError(string.Empty);
            Assert.Null(nullEx);

            nullEx = DtmGImp.Utils.String2DtmError("other");
            Assert.Null(nullEx);
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("http://a.b.com/", "a.b.com")]
        [InlineData("https://a.b.com", "a.b.com")]
        public void GetWithoutPrefixgRPCUrl_Should_Succeed(string url, string exp)
        {
            var res = DtmGImp.Utils.GetWithoutPrefixgRPCUrl(url);
            Assert.Equal(exp, res);
        }

        [Fact]
        public void TransInfo2Metadata_Should_Succeed()
        {
            var meta = DtmGImp.Utils.TransInfo2Metadata("1", "2", "3", "4", "5");

            Assert.Equal("1", meta.Get(Constant.Md.Gid).Value);
            Assert.Equal("2", meta.Get(Constant.Md.TransType).Value);
            Assert.Equal("3", meta.Get(Constant.Md.BranchId).Value);
            Assert.Equal("4", meta.Get(Constant.Md.Op).Value);
            Assert.Equal("5", meta.Get(Constant.Md.Dtm).Value);
        }

        [Fact]
        public void DtmGet_Should_Succeed()
        {
            var meta = new Metadata();
            meta.Add("a", "b");
            meta.Add("c", "d");
            var context = new CusServerCallContext(meta);

            var str1 = DtmGImp.Utils.DtmGet(context, "a");
            Assert.Equal("b", str1);

            var str2 = DtmGImp.Utils.DtmGet(context, "d");
            Assert.Null(str2);
        }

        [Fact]
        public void DtmGet_When_Header_IsNull_Should_Succeed()
        {
            var context = new CusServerCallContext(null);

            var str = DtmGImp.Utils.DtmGet(context, "a");            
            Assert.Null(str);
        }
    }
}
