using Microsoft.Extensions.Logging;

namespace Dtmcli
{
    public interface IBranchBarrierFactory
    {
        BranchBarrier CreateBranchBarrier(string transType, string gid, string branchID, string op, ILogger logger = null);

#if NET5_0_OR_GREATER
        BranchBarrier CreateBranchBarrier(Microsoft.AspNetCore.Http.IQueryCollection query, ILogger logger = null);
#endif
    }
}
