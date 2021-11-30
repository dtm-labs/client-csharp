using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Dtmcli
{
    public class DtmResult
    {
        [JsonPropertyName("dtm_result")]
        public string Dtm_Result { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        public bool Success
        {
            get
            {
                return Dtm_Result.ToUpper() == "SUCCESS";
            }
        }
        /// <summary>
        /// dtm服务暂时不会返回该字段：但TccGlobalTransaction.Excecute方法会组装上去返回
        /// </summary>
        public string Gid { get; set; }
    }
}
