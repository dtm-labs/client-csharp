using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Dtmcli.Tests
{
    public class TestHelper
    {
        public static void MockTransCallDtm(Mock<IDtmClient> mock, string op, bool result)
        {
            mock
                .Setup(x => x.TransCallDtm(It.IsAny<DtmImp.TransBase>(), It.IsAny<object>(), op, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));
        }

        public static void MockTransRegisterBranch(Mock<IDtmClient> mock, string op, bool result)
        {
            mock
                .Setup(x => x.TransRegisterBranch(It.IsAny<DtmImp.TransBase>(), It.IsAny<Dictionary<string, string>>(), op, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));
        }

        public static void MockTransRequestBranch(Mock<IDtmClient> mock, System.Net.HttpStatusCode statusCode, string content = "content")
        {
            var httpRspMsg = new HttpResponseMessage(statusCode);
            httpRspMsg.Content = new StringContent(content);

            mock
                .Setup(x => x.TransRequestBranch(It.IsAny<DtmImp.TransBase>(), It.IsAny<HttpMethod>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(httpRspMsg));
        }

        public static ServiceProvider AddDtmCli(string db = "mysql", string tbName = "dtm_barrier.barrier")
        {
            var dtm = "http://localhost:36789";
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDtmcli(x =>
            {
                x.DtmUrl = dtm;
                x.DBType = db;
                x.BarrierTableName = tbName;
            });

            var provider = services.BuildServiceProvider();
            return provider;
        }
    }
}