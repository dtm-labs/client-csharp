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
                return new DtmOngingException();
            }
            else if (resp.StatusCode == HttpStatusCode.Conflict || str.Contains(DtmCommon.Constant.ResultFailure))
            {
                return new DtmFailureException();
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

        public static void CheckStatusCode(HttpStatusCode status)
        {
            if (status != HttpStatusCode.OK)
            {
                throw new DtmException(string.Format(CheckStatusMsgFormat, status.ToString(), string.Empty));
            }
        }

        /// <summary>
        /// OrString return the first not null or not empty string
        /// </summary>
        /// <param name="ss"></param>
        /// <returns></returns>
        public static string OrString(params string[] ss)
        {
            foreach (var s in ss)
            {
                if (!string.IsNullOrEmpty(s))
                    return s;
            }

            return "";
        }

        /// <summary>
        /// translate string to dtm error
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Exception String2DtmError(string str)
        {
            if (str == DtmCommon.Constant.ResultSuccess || str == string.Empty)
                return null;
            if (str == string.Empty)
                return null;
            if (str == DtmCommon.Constant.ResultFailure)
                return new DtmCommon.DtmFailureException();
            if (str == DtmCommon.Constant.ResultOngoing)
                return new DtmCommon.DtmOngingException();
            return new Exception(str);
        }

        /// <summary>
        /// translate object to http response
        /// 409 => ErrFailure; Code 425 => ErrOngoing; Code 500 => InternalServerError
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public static (int httpStatusCode, object res) Result2HttpJson(object result)
        {
            if (result is not Exception err)
            {
                return ((int)(HttpStatusCode.OK), result);
            }

            var res = new { error = err.Message };
            if (err is DtmFailureException)
                return ((int)HttpStatusCode.Conflict, res);
            if (err is DtmOngingException)
                return (425 /*HttpStatusCode.TooEarly*/, res);

            return ((int)HttpStatusCode.InternalServerError, res);
        }
    }
}
