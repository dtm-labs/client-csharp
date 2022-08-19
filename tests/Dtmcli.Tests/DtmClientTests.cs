using Xunit;
using System.Net;
using Moq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;

namespace Dtmcli.Tests
{
    public class DtmClientTests
    {
        [Fact]
        public async void GenGid_Should_Succeed()
        {
            var factory = new Mock<IHttpClientFactory>();
            var options = Microsoft.Extensions.Options.Options.Create(new DtmCommon.DtmOptions { DtmUrl = "http://localhost:8080" });
            var mockHttpMessageHandler = new ClientMockHttpMessageHandler(HttpStatusCode.OK, "{\"dtm_result\":\"SUCCESS\",\"gid\":\"123\"}");
            factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(mockHttpMessageHandler));

            var client = new DtmClient(factory.Object, options);

            var res = await client.GenGid(new CancellationToken());

            Assert.Equal("123", res);
        }

        [Fact]
        public async void GenGid_Should_Throw_Failure_Exception()
        {
            var factory = new Mock<IHttpClientFactory>();
            var options = Microsoft.Extensions.Options.Options.Create(new DtmCommon.DtmOptions { DtmUrl = "http://localhost:8080" });
            var mockHttpMessageHandler = new ClientMockHttpMessageHandler(HttpStatusCode.BadGateway, "");
            factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(mockHttpMessageHandler));

            var client = new DtmClient(factory.Object, options);

            await Assert.ThrowsAsync<DtmCommon.DtmException>( async()=> await client.GenGid(new CancellationToken()));
        }

        [Fact]
        public async void TransRegisterBranch_Should_Succeed()
        {
            var factory = new Mock<IHttpClientFactory>();
            var options = Microsoft.Extensions.Options.Options.Create(new DtmCommon.DtmOptions { DtmUrl = "http://localhost:8080" });
            var mockHttpMessageHandler = new ClientMockHttpMessageHandler(HttpStatusCode.OK, "");
            factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(mockHttpMessageHandler));

            var client = new DtmClient(factory.Object, options);

            var tb = new DtmCommon.TransBase() { Gid = "123", TransType = "tcc" };

            await client.TransRegisterBranch(tb, null, "OP", new CancellationToken());
        }

        [Fact]
        public async void TransRegisterBranch_With_Added_Should_Succeed()
        {
            var factory = new Mock<IHttpClientFactory>();
            var options = Microsoft.Extensions.Options.Options.Create(new DtmCommon.DtmOptions { DtmUrl = "http://localhost:8080" });
            var mockHttpMessageHandler = new ClientMockHttpMessageHandler(HttpStatusCode.OK, "");
            factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(mockHttpMessageHandler));

            var client = new DtmClient(factory.Object, options);

            var tb = new DtmCommon.TransBase() { Gid = "123", TransType = "tcc" };
            var added = new System.Collections.Generic.Dictionary<string, string>() { { "a", "b" } };

            await client.TransRegisterBranch(tb, added, "OP", new CancellationToken());
        }

        [Fact]
        public async void TransRequestBranch_Should_Succeed()
        {
            var factory = new Mock<IHttpClientFactory>();
            var options = Microsoft.Extensions.Options.Options.Create(new DtmCommon.DtmOptions { DtmUrl = "http://localhost:8080" });
            var mockHttpMessageHandler = new ClientMockHttpMessageHandler(HttpStatusCode.OK, "");
            factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(mockHttpMessageHandler));

            var client = new DtmClient(factory.Object, options);

            var tb = new DtmCommon.TransBase()
            {
                Gid = "123",
                TransType = "tcc",
            };

            await client.TransRequestBranch(tb, HttpMethod.Post, new { }, "00", "try", "http://www.baidu.com?a=1", new CancellationToken());
        }

        [Fact]
        public async void TransRequestBranch_With_BranchHeaders_Should_Succeed()
        {
            var factory = new Mock<IHttpClientFactory>();
            var options = Microsoft.Extensions.Options.Options.Create(new DtmCommon.DtmOptions { DtmUrl = "http://localhost:8080" });
            var mockHttpMessageHandler = new ClientMockHttpMessageHandler(HttpStatusCode.OK, "");
            factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(mockHttpMessageHandler));

            var client = new DtmClient(factory.Object, options);

            var tb = new DtmCommon.TransBase() 
            { 
                Gid = "123", 
                TransType = "tcc",
                BranchHeaders = new System.Collections.Generic.Dictionary<string, string> { { "a", "b" } }
            };
            
            await client.TransRequestBranch(tb, HttpMethod.Post, new { }, "00", "try", "http://www.baidu.com", new CancellationToken());
        }

#if NET5_0_OR_GREATER
        [Fact]
        public void TransBaseFromQuery_Should_Succeed()
        {
            var factory = new Mock<IHttpClientFactory>();
            var options = Microsoft.Extensions.Options.Options.Create(new DtmCommon.DtmOptions { DtmUrl = "http://localhost:8080" });
            var mockHttpMessageHandler = new ClientMockHttpMessageHandler(HttpStatusCode.OK, "");
            factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(mockHttpMessageHandler));

            var client = new DtmClient(factory.Object, options);

            var dict = new System.Collections.Generic.Dictionary<string, Microsoft.Extensions.Primitives.StringValues>()
            {
                { "branch_id","11" },
                { "gid","1111" },
                { "op","try" },
                { "trans_type","tcc" },
            };

            var qs = new Microsoft.AspNetCore.Http.QueryCollection(dict);

            var tb = client.TransBaseFromQuery(qs);

            Assert.Equal(dict["op"], tb.Op);
            Assert.Equal(dict["gid"], tb.Gid);
            Assert.Equal(dict["trans_type"], tb.TransType);
            Assert.Equal(dict["branch_id"], tb.BranchIDGen.BranchID);
        }
#endif
    }

    internal class ClientMockHttpMessageHandler : DelegatingHandler
    {
        private readonly HttpStatusCode _code;
        private readonly string _msg;
        public ClientMockHttpMessageHandler(HttpStatusCode code, string msg)
        {
            this._code = code;
            this._msg = msg;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var content = new StringContent(_msg);
            var resp = new HttpResponseMessage(_code);
            resp.Content = content;

            return Task.FromResult(resp);
        }
    }
}