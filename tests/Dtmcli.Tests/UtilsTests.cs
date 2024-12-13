using System;
using Xunit;
using System.Net;
using System.Net.Http;
using System.Text.Json.Serialization;
using DtmCommon;
using Newtonsoft.Json;

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
            Assert.IsType<DtmCommon.DtmOngingException>(res);
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
            Assert.IsType<DtmCommon.DtmFailureException>(res);
            Assert.Equal("FAILURE", res.Message);
        }

        [Theory]
        [InlineData(HttpStatusCode.Conflict, "123")]
        [InlineData(HttpStatusCode.OK, "FAILURE")]
        public void CheckStatus_Should_Throw_Failure_Exception(HttpStatusCode code, string msg)
        {
            Assert.Throws<DtmCommon.DtmException>(() => DtmImp.Utils.CheckStatus(code, msg));
        }

        [Fact]
        public void OrString()
        {
            Assert.Equal("", DtmImp.Utils.OrString());
            Assert.Equal("A", DtmImp.Utils.OrString("", "A"));
            Assert.Equal("A", DtmImp.Utils.OrString("", "A", "B"));
            Assert.Equal("A", DtmImp.Utils.OrString("A", "B"));
        }

        [Fact]
        public void String2DtmError()
        {
            Assert.IsType<Exception>(DtmImp.Utils.String2DtmError(null));
            Assert.Null(DtmImp.Utils.String2DtmError(string.Empty));
            Assert.Null(DtmImp.Utils.String2DtmError("SUCCESS"));
            Assert.IsType<DtmFailureException>(DtmImp.Utils.String2DtmError("FAILURE"));
            Assert.IsType<DtmOngingException>(DtmImp.Utils.String2DtmError("ONGOING"));
            Assert.IsType<Exception>(DtmImp.Utils.String2DtmError("Object ..."));
        }
        
        [Fact]
        public void Result2HttpJson()
        {
            Assert.Equal(200, DtmImp.Utils.Result2HttpJson(null).httpStatusCode);
            Assert.Equal(409, DtmImp.Utils.Result2HttpJson(new DtmFailureException()).httpStatusCode);
            Assert.Equal(425, DtmImp.Utils.Result2HttpJson(new DtmOngingException()).httpStatusCode);
            Assert.Equal(500, DtmImp.Utils.Result2HttpJson(new DtmDuplicatedException()).httpStatusCode);
            
            Assert.Equal(500, DtmImp.Utils.Result2HttpJson(new Exception("message context A")).httpStatusCode);
            Assert.Contains("message context A", JsonConvert.SerializeObject(DtmImp.Utils.Result2HttpJson(new Exception("message context A")).res));
            
            Assert.Equal(200, DtmImp.Utils.Result2HttpJson("normal text").httpStatusCode);
            Assert.Equal("normal text", DtmImp.Utils.Result2HttpJson("normal text").res);

            var obj = new { A = "hello", B = "world" };
            Assert.Equal(200, DtmImp.Utils.Result2HttpJson(obj).httpStatusCode);
            Assert.Equal(obj, DtmImp.Utils.Result2HttpJson(obj).res);
        }
    }
}