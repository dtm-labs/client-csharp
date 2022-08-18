using System;

namespace Dtmgrpc.Driver
{
    public class DefaultDtmDriver : IDtmDriver
    {
        private static readonly int PathPartCount = 3;
        private static readonly string DefaultName = "default";

        public string GetName() => DefaultName;

        public (string server, string serviceName, string method, string error) ParseServerMethod(string url)
        {
            try
            {
                if (url.Contains("://"))
                {
                    // http://localhost:5005/servicename/method
                    return (string.Empty, string.Empty, string.Empty, $"bad url: {url}.");
                }
                else
                {
                    // localhost:5005/servicename/method
                    var arr = url.Split('/');
                    if (arr.Length < PathPartCount) return (string.Empty, string.Empty, string.Empty, $"bad url: {url}.");
                    return (arr[0], arr[1], arr[2], "");
                }

            }
            catch (Exception ex)
            {
                return (string.Empty, string.Empty, string.Empty, ex.Message);
            }
        }

        public void RegisterGrpcResolver()
        {
        }

        public void RegisterGrpcService(string target, string endpoint)
        {
        }
    }
}
