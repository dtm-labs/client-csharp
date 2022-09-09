using DtmCommon;
using Grpc.Core;
using System;

namespace Dtmworkflow
{
    internal static class Utils
    {
        internal static System.Net.Http.HttpResponseMessage NewJSONResponse(System.Net.HttpStatusCode status, byte[] result)
        {
            var resp = new System.Net.Http.HttpResponseMessage(status);
            resp.Content = new System.Net.Http.ByteArrayContent(result);
            resp.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            return resp;
        }

        internal static (byte[], Exception) HTTPResp2DtmError(System.Net.Http.HttpResponseMessage resp)
        {
            var code = resp.StatusCode;
            var data = resp.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();

            if ((int)code == 425)
            {

            }
            else if (code == System.Net.HttpStatusCode.Conflict)
            {

            }
            else if (code != System.Net.HttpStatusCode.OK)
            {

            }

            return (data, null);
        }

        internal static Exception GrpcError2DtmError(Exception ex)
        {
            if (ex is RpcException rpcEx)
            {
                if (rpcEx.StatusCode == StatusCode.Aborted)
                {
                    return new DtmFailureException(ex.Message);
                }
                else if (rpcEx.StatusCode == StatusCode.FailedPrecondition)
                {
                    return new DtmOngingException(ex.Message);
                }
            }

            return ex;
        }
    }
}
