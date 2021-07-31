using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Dtmcli
{
    public class RegisterTccBranch
    {
        [JsonPropertyName("gid")]
        public string Gid { get; set; }

        [JsonPropertyName("branch_id")]
        public string Branch_id { get; set; }

        [JsonPropertyName("trans_type")]
        public string Trans_type { get; set; } = "tcc";

        [JsonPropertyName("status")]
        public string Status { get; set; } = "prepared";

        [JsonPropertyName("data")]
        public string Data { get; set; }

        [JsonPropertyName("try")]
        public string Try { get; set; }

        [JsonPropertyName("confirm")]
        public string Confirm { get; set; }

        [JsonPropertyName("cancel")]
        public string Cancel { get; set; }
    }
}
