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
    }
}
