using System.Text.Json.Serialization;

namespace Dtmcli
{
    public class DtmGid
    {
        [JsonPropertyName("gid")]
        public string Gid { get; set; }

        [JsonPropertyName("dtm_result")]
        public string Dtm_Result { get; set; }
    }
}
