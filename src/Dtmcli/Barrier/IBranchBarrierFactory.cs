using Microsoft.Extensions.Logging;

namespace Dtmcli
{
    public interface IBranchBarrierFactory
    {
        BranchBarrier CreateBranchBarrier(string transType, string gid, string branchID, string op, ILogger logger = null);
    }
}
