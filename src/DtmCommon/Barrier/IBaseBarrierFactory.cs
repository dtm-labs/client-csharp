using Microsoft.Extensions.Logging;

namespace DtmCommon
{
    public interface IBaseBarrierFactory
    {
        BranchBarrier CreateBranchBarrier(string transType, string gid, string branchID, string op, ILogger logger = null);
    }

}