using DtmCommon;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Dtmcli.DtmImp
{
    public static class Utils
    {
        private const int StatusTooEarly = 425;
        private const string CheckStatusMsgFormat = "http response status: {0}, Message :{1}";

        public static async Task<Exception> RespAsErrorCompatible(HttpResponseMessage resp)
        {
            var str = resp.Content != null ? await resp.Content.ReadAsStringAsync() : string.Empty;

            // System.Net.HttpStatusCode do not contain StatusTooEarly
            if ((int)resp.StatusCode == StatusTooEarly || str.Contains(DtmCommon.Constant.ResultOngoing))
            {
                return new DtmException(DtmCommon.Constant.ResultOngoing);
            }
            else if (resp.StatusCode == HttpStatusCode.Conflict || str.Contains(DtmCommon.Constant.ResultFailure))
            {
                return new DtmException(DtmCommon.Constant.ResultFailure);
            }
            else if (resp.StatusCode != HttpStatusCode.OK)
            {
                return new Exception(str);
            }

            return null;
        }

        public static void CheckStatus(HttpStatusCode status, string dtmResult)
        {
            if (status != HttpStatusCode.OK || dtmResult.Contains(DtmCommon.Constant.ResultFailure))
            {
                throw new DtmException(string.Format(CheckStatusMsgFormat, status.ToString(), dtmResult));
            }
        }
    }
}
