using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Dtmcli.DtmImp
{
    public class TransBase
    {
        [JsonPropertyName("gid")]
        public string Gid { get; set; }

        [JsonPropertyName("trans_type")]
        public string TransType { get; set; }

        [JsonPropertyName("custom_data")]
        public string CustomData { get; set; }

        [JsonPropertyName("wait_result")]
        public bool WaitResult { get; set; }

        [JsonPropertyName("timeout_to_fail")]
        public long TimeoutToFail { get; set; }

        [JsonPropertyName("retry_interval")]
        public long RetryInterval { get; set; }

        [JsonPropertyName("passthrough_headers")]
        public List<string> PassthroughHeaders { get; set; }

        [JsonPropertyName("branch_headers")]
        public Dictionary<string, string> BranchHeaders { get; set; }

        /// <summary>
        /// use in MSG/SAGA
        /// </summary>
        [JsonPropertyName("steps")]
        public List<Dictionary<string, string>> Steps { get; set; }

        /// <summary>
        /// used in MSG/SAGA
        /// </summary>
        [JsonPropertyName("payloads")]
        public List<string> Payloads { get; set; }

        [JsonIgnore]
        public string BinPayloads { get; set; }

        /// <summary>
        /// used in XA/TCC
        /// </summary>
        [JsonIgnore]
        public BranchIDGen BranchIDGen { get; set; }

        /// <summary>
        /// used in XA/TCC
        /// </summary>
        [JsonIgnore]
        public string Op { get; set; }

        /// <summary>
        /// used in MSG
        /// </summary>
        [JsonPropertyName("query_prepared")]
        public string QueryPrepared { get; set; }

        public static TransBase NewTransBase(string gid, string transType, string branchID)
        {
            return new TransBase
            {
                Gid = gid,
                TransType = transType,
                BranchIDGen = new BranchIDGen(branchID),
            };
        }
    }
}
