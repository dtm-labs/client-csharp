namespace Dtmgrpc.Driver
{
    public interface IDtmDriver
    {
        string GetName();

        void RegisterGrpcResolver();

        void RegisterGrpcService(string target, string endpoint);

        (string server, string serviceName, string method, string error) ParseServerMethod(string url);
    }
}
