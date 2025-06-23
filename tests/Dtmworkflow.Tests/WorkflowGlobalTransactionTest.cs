using Dtmcli;
using Dtmgrpc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Dtmworkflow.Tests;

public class WorkflowGlobalTransactionTest
{
    [Fact]
    public void Exists()
    {
        var factory = new Mock<IWorkflowFactory>();
        var wf = new WorkflowGlobalTransaction(factory.Object, NullLoggerFactory.Instance);
        
        Assert.Throws<System.ArgumentNullException>(() => wf.Exists(null));
        Assert.False(wf.Exists(string.Empty));
        Assert.False(wf.Exists("my-wf1"));
        Assert.False(wf.Exists("my-wf2"));
        Assert.False(wf.Exists("my-wf3"));

        wf.Register("my-wf1", (workflow, data) => null);
        wf.Register("my-wf2", (workflow, data) => null);

        Assert.Throws<System.ArgumentNullException>(() => wf.Exists(null));
        Assert.False(wf.Exists(string.Empty));
        Assert.True(wf.Exists("my-wf1"));
        Assert.True(wf.Exists("my-wf2"));
        Assert.False(wf.Exists("my-wf3"));
        
        var wf2 = new WorkflowGlobalTransaction(factory.Object, NullLoggerFactory.Instance);
        Assert.False(wf2.Exists("my-wf1"));
        Assert.False(wf2.Exists("my-wf2"));
        Assert.False(wf2.Exists("my-wf3"));
    }
}