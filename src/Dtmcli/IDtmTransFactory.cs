namespace Dtmcli
{
    public interface IDtmTransFactory
    {
        Saga NewSaga(string gid);

        Msg NewMsg(string gid);
    }
}
