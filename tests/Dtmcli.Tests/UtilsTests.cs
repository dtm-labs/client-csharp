using Xunit;
using System.Net;
using System.Net.Http;

namespace Dtmcli.Tests
{
    public class UtilsTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async void RespAsErrorCompatible_Should_Return_Null(bool isNull)
        {
            HttpResponseMessage resp = new HttpResponseMessage();
            resp.StatusCode = HttpStatusCode.OK;
            resp.Content = isNull ? null : new StringContent("123");

            var res = await DtmImp.Utils.RespAsErrorCompatible(resp);
            Assert.Null(res);
        }

        [Fact]
        public async void RespAsErrorCompatible_Should_Throw_Unkown_Exception()
        {
            HttpResponseMessage resp = new HttpResponseMessage();
            resp.StatusCode = HttpStatusCode.BadGateway;
            resp.Content = new StringContent("string");

            var res = await DtmImp.Utils.RespAsErrorCompatible(resp);
            Assert.Equal("string", res.Message);
        }

        [Fact]
        public async void RespAsErrorCompatible_Should_Throw_Ongoing_Exception()
        {
            HttpResponseMessage resp = new HttpResponseMessage();           
            resp.Content = new StringContent("ONGOING");

            var res = await DtmImp.Utils.RespAsErrorCompatible(resp);
            Assert.IsType<DtmCommon.DtmException>(res);
            Assert.Equal("ONGOING", res.Message);
        }

        [Theory]
        [InlineData(HttpStatusCode.Conflict, "")]
        [InlineData(HttpStatusCode.OK, "FAILURE")]
        public async void RespAsErrorCompatible_Should_Throw_Failure_Exception(HttpStatusCode code, string msg)
        {
            HttpResponseMessage resp = new HttpResponseMessage();
            resp.StatusCode = code;
            resp.Content = new StringContent(msg);

            var res = await DtmImp.Utils.RespAsErrorCompatible(resp);
            Assert.IsType<DtmCommon.DtmException>(res);
            Assert.Equal("FAILURE", res.Message);
        }

        [Theory]
        [InlineData(HttpStatusCode.Conflict, "123")]
        [InlineData(HttpStatusCode.OK, "FAILURE")]
        public void CheckStatus_Should_Throw_Failure_Exception(HttpStatusCode code, string msg)
        {
            Assert.Throws<DtmCommon.DtmException>(() => DtmImp.Utils.CheckStatus(code, msg));
        }
    }
}