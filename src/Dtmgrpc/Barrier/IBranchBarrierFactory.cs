using DtmCommon;
using Microsoft.Extensions.Logging;

namespace Dtmgrpc
{
    public interface IBranchBarrierFactory : IBaseBarrierFactory
    {
        BranchBarrier CreateBranchBarrier(Grpc.Core.ServerCallContext context, ILogger logger = null);
    }
}