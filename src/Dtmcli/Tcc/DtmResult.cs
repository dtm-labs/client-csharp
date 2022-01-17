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
    }
}
