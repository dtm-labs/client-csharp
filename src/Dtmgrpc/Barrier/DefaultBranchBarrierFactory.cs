using DtmCommon;
using Dtmgrpc.DtmGImp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dtmgrpc
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
            if (logger == null) logger = _logger;

            var ti = new BranchBarrier(transType, gid, branchID, op, _options, _dbUtils, logger);

            if (ti.IsInValid()) throw new DtmException($"invalid trans info: {ti.ToString()}");

            return ti;
        }

        public BranchBarrier CreateBranchBarrier(Grpc.Core.ServerCallContext context, ILogger logger = null)
        {
            if (logger == null) logger = _logger;
           
            var tb = Utils.TransBaseFromGrpc(context);

            var ti = new BranchBarrier(tb.TransType, tb.Gid, tb.BranchIDGen.BranchID, tb.Op, _options, _dbUtils, logger);

            if (ti.IsInValid()) throw new DtmException($"invalid trans info: {ti.ToString()}");

            return ti;
        }
    }
}