using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Dtmcli.IntegrationTests;

public class MsgHttpTest
{
    [Fact]
    public async Task Submit_Should_Succeed()
    {
        var provider = ITTestHelper.AddDtmHttp();
        var transFactory = provider.GetRequiredService<Dtmcli.IDtmTransFactory>();

        var gid = "msgTestGid" + Guid.NewGuid().ToString();
        var msg = transFactory.NewMsg(gid);
        msg.EnableWaitResult();
        var req = ITTestHelper.GenBusiReq(false, false);
        var busiUrl = ITTestHelper.BuisHttpUrl;
        msg.Add(busiUrl + "/busi.Busi/TransOut", req)
            .Add(busiUrl + "/busi.Busi/TransIn", req);

        await msg.Prepare(busiUrl + "/busi.Busi/QueryPrepared_404");
        await msg.Submit();

        var status = await ITTestHelper.GetTranStatus(gid);
        Assert.Equal("succeed", status);
    }

    [Fact]
    public async Task Submit_With_EffectTime_Should_Succeed_Later()
    {
        var provider = ITTestHelper.AddDtmHttp();
        var transFactory = provider.GetRequiredService<Dtmcli.IDtmTransFactory>();

        var gid = "msgTestGid" + Guid.NewGuid().ToString();
        DateTime effectTime = DateTime.Now.AddSeconds(10);
        var msg = transFactory.NewMsg(gid);
        var req = ITTestHelper.GenBusiReq(false, false);
        req.EffectTime = effectTime;
        var busiUrl = ITTestHelper.BuisHttpUrl;
        msg.Add(busiUrl + "/busi.Busi/TransOut", req)
            .Add(busiUrl + "/busi.Busi/TransIn", req);

        await msg.Prepare(busiUrl + "/busi.Busi/QueryPrepared_404");
        await msg.Submit();

        // Since the downstream execution is delayed by 10 seconds, it will be 'submitted' after 2 seconds and 'succeed' after 15 seconds
        await Task.Delay(TimeSpan.FromSeconds(0));
        var status = await ITTestHelper.GetTranStatus(gid);
        Assert.Equal("submitted", status);

        await Task.Delay(TimeSpan.FromSeconds(2));
        status = await ITTestHelper.GetTranStatus(gid);
        Assert.Equal("submitted", status);

        await Task.Delay(TimeSpan.FromSeconds(13));
        status = await ITTestHelper.GetTranStatus(gid);
        Assert.Equal("succeed", status);
    }
    
    [Fact]
    public async Task Submit_With_NextCronTime_Should_Succeed_Later()
    {
        var provider = ITTestHelper.AddDtmHttp();
        var transFactory = provider.GetRequiredService<Dtmcli.IDtmTransFactory>();

        var gid = "msgTestGid" + Guid.NewGuid().ToString();
        DateTime effectTime = DateTime.Now.AddSeconds(10);
        var msg = transFactory.NewMsg(gid, effectTime);
        var req = ITTestHelper.GenBusiReq(false, false);
        var busiUrl = ITTestHelper.BuisHttpUrl;
        msg.Add(busiUrl + "/busi.Busi/TransOut", req)
            .Add(busiUrl + "/busi.Busi/TransIn", req);

        await msg.Prepare(busiUrl + "/busi.Busi/QueryPrepared_404");
        await msg.Submit();

        // Since the downstream execution is delayed by 10 seconds, it will be 'submitted' after 2 seconds and 'succeed' after 15 seconds
        await Task.Delay(TimeSpan.FromSeconds(0));
        var status = await ITTestHelper.GetTranStatus(gid);
        Assert.Equal("submitted", status);

        await Task.Delay(TimeSpan.FromSeconds(2));
        status = await ITTestHelper.GetTranStatus(gid);
        Assert.Equal("submitted", status);

        await Task.Delay(TimeSpan.FromSeconds(13));
        status = await ITTestHelper.GetTranStatus(gid);
        Assert.Equal("succeed", status);
    }
}