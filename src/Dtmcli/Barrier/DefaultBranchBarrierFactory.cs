using Microsoft.Extensions.Logging;
using System;

namespace Dtmcli
{
    public class DefaultBranchBarrierFactory : IBranchBarrierFactory
    {
        private readonly ILogger _logger;

        public DefaultBranchBarrierFactory(ILoggerFactory loggerFactory)
        { 
            this._logger = loggerFactory.CreateLogger<DefaultBranchBarrierFactory>();
        }

        public BranchBarrier CreateBranchBarrier(string transType, string gid, string branchID, string op, ILogger logger = null)
        {
            if(logger == null) logger = _logger;

            var ti = new BranchBarrier(transType, gid, branchID, op, logger);

            if (ti.IsInValid()) throw new DtmcliException($"invalid trans info: {ti.ToString()}");

            return ti;
        }

#if NET5_0_OR_GREATER
        public BranchBarrier CreateBranchBarrier(Microsoft.AspNetCore.Http.IQueryCollection query, ILogger logger = null)
        {
            if (logger == null) logger = _logger;
           
            _ = query.TryGetValue(Constant.Request.BRANCH_ID, out var branchID);
            _ = query.TryGetValue(Constant.Request.GID, out var gid);
            _ = query.TryGetValue(Constant.Request.OP, out var op);
            _ = query.TryGetValue(Constant.Request.TRANS_TYPE, out var transType);

            var ti = new BranchBarrier(transType, gid, branchID, op, logger);

            if (ti.IsInValid()) throw new DtmcliException($"invalid trans info: {ti.ToString()}");

            return ti;
        }
#endif
    }
}
