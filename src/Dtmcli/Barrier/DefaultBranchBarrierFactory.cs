using DtmCommon;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dtmcli
{
    public class DefaultBranchBarrierFactory : IBranchBarrierFactory
    {
        private readonly ILogger _logger;
        private readonly DtmOptions _options;
        private readonly DbUtils _dbUtils;

        public DefaultBranchBarrierFactory(ILoggerFactory loggerFactory, IOptions<DtmOptions> optionsAccs, DbUtils dbUtils)
        { 
            this._logger = loggerFactory.CreateLogger<DefaultBranchBarrierFactory>();
            this._dbUtils = dbUtils;
            this._options = optionsAccs.Value;
        }

        public BranchBarrier CreateBranchBarrier(string transType, string gid, string branchID, string op, ILogger logger = null)
        {
            if(logger == null) logger = _logger;

            var ti = new BranchBarrier(transType, gid, branchID, op, _options, _dbUtils, logger);

            if (ti.IsInValid()) throw new DtmException($"invalid trans info: {ti.ToString()}");

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

            var ti = new BranchBarrier(transType, gid, branchID, op, _options, _dbUtils, logger);

            if (ti.IsInValid()) throw new DtmException($"invalid trans info: {ti.ToString()}");

            return ti;
        }
#endif
    }
}
