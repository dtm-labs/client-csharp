using DtmCommon;
using Microsoft.Extensions.Logging;

namespace Dtmcli
{
    public interface IBranchBarrierFactory : IBaseBarrierFactory
    {
#if NET5_0_OR_GREATER
        BranchBarrier CreateBranchBarrier(Microsoft.AspNetCore.Http.IQueryCollection query, ILogger logger = null);
#endif
    }
}
