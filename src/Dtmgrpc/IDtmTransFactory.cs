namespace Dtmgrpc
{
    public interface IDtmTransFactory
    {
        SagaGrpc NewSagaGrpc(string gid);

        MsgGrpc NewMsgGrpc(string gid);

        TccGrpc NewTccGrpc(string gid);
    }
}
