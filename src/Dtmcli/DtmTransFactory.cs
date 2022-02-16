namespace Dtmcli
{
    public class DtmTransFactory : IDtmTransFactory
    {
        private readonly IDtmClient _cient;
        private readonly IBranchBarrierFactory _branchBarrierFactory;

        public DtmTransFactory(IDtmClient client, IBranchBarrierFactory branchBarrierFactory)
        {
            this._cient = client;
            this._branchBarrierFactory = branchBarrierFactory;
        }

        public Msg NewMsg(string gid)
        {
            var msg = new Msg(_cient, _branchBarrierFactory, gid);
            return msg;
        }

        public Saga NewSaga(string gid)
        {
            var saga = new Saga(_cient, gid);
            return saga;
        }
    }
}
