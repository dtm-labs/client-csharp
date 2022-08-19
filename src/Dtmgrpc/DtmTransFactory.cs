using DtmCommon;
using Dtmgrpc.DtmGImp;
using Microsoft.Extensions.Options;

namespace Dtmgrpc
{
    public class DtmTransFactory : IDtmTransFactory
    {
        private readonly DtmOptions _options;
        private readonly IDtmgRPCClient _rpcClient;
        private readonly IBranchBarrierFactory _branchBarrierFactory;

        public DtmTransFactory(IOptions<DtmOptions> optionsAccs, IDtmgRPCClient rpcClient, IBranchBarrierFactory branchBarrierFactory)
        {
            this._options = optionsAccs.Value;
            this._rpcClient = rpcClient;
            this._branchBarrierFactory = branchBarrierFactory;
        }

        public MsgGrpc NewMsgGrpc(string gid)
        {
            var msg = new MsgGrpc(_rpcClient, _branchBarrierFactory, _options.DtmGrpcUrl.GetWithoutPrefixgRPCUrl(), gid);
            return msg;
        }

        public SagaGrpc NewSagaGrpc(string gid)
        {
            var saga = new SagaGrpc(_rpcClient, _options.DtmGrpcUrl.GetWithoutPrefixgRPCUrl(), gid);
            return saga;
        }

        public TccGrpc NewTccGrpc(string gid)
        {
            var tcc = new TccGrpc(_rpcClient, TransBase.NewTransBase(gid, Constant.TYPE_TCC, _options.DtmGrpcUrl.GetWithoutPrefixgRPCUrl(), string.Empty));
            return tcc;
        }
    }
}
