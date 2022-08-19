using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DtmCommon;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Dtmcli.Tests
{
    public class TestHelper
    {
        public static void MockTransCallDtm(Mock<IDtmClient> mock, string op, bool isEx)
        {
            var setup = mock
                .Setup(x => x.TransCallDtm(It.IsAny<TransBase>(), It.IsAny<object>(), op, It.IsAny<CancellationToken>()));

            if (isEx)
            {
                setup.Throws(new Exception(""));
            }
            else
            {
                setup.Returns(Task.CompletedTask);
            }
        }

        public static void MockTransRegisterBranch(Mock<IDtmClient> mock, string op, bool isEx)
        {
            var setup = mock
                .Setup(x => x.TransRegisterBranch(It.IsAny<TransBase>(), It.IsAny<Dictionary<string, string>>(), op, It.IsAny<CancellationToken>()));

            if (isEx)
            {
                setup.Throws(new Exception(""));
            }
            else
            {
                setup.Returns(Task.CompletedTask);
            }
        }

        public static void MockTransRequestBranch(Mock<IDtmClient> mock, HttpStatusCode statusCode, string content = "content")
        {
            var httpRspMsg = new HttpResponseMessage(statusCode);
            httpRspMsg.Content = new StringContent(content);

            mock
                .Setup(x => x.TransRequestBranch(It.IsAny<TransBase>(), It.IsAny<HttpMethod>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
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