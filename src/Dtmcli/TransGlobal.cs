using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Dtmcli;

/// <summary>
/// query status only
/// </summary>
internal class TransGlobalForStatus
{
    [JsonPropertyName("transaction")] public DtmTransactionForStatus Transaction { get; set; }
    
    public class  DtmTransactionForStatus
    {
        [JsonPropertyName("status")] public string Status { get; set; }
    }
}

// convert from json(a prepared TCC global trans sample) to c# code
public class TransGlobal
{
    [JsonPropertyName("branches")] public List<DtmBranch> Branches { get; set; }

    [JsonPropertyName("transaction")] public DtmTransaction Transaction { get; set; }

    public class DtmTransaction
    {
        [JsonPropertyName("id")] public int Id { get; set; }

        [JsonPropertyName("create_time")] public DateTimeOffset CreateTime { get; set; }

        [JsonPropertyName("update_time")] public DateTimeOffset UpdateTime { get; set; }

        [JsonPropertyName("gid")] public string Gid { get; set; }

        [JsonPropertyName("trans_type")] public string TransType { get; set; }

        [JsonPropertyName("steps")] public List<TransactionStep> Steps { get; set; }

        [JsonPropertyName("payloads")] public List<string> Payloads { get; set; }

        [JsonPropertyName("status")] public string Status { get; set; }

        [JsonPropertyName("query_prepared")] public string QueryPrepared { get; set; }

        [JsonPropertyName("protocol")] public string Protocol { get; set; }

        [JsonPropertyName("options")] public string Options { get; set; }

        [JsonPropertyName("next_cron_interval")]
        public int NextCronInterval { get; set; }

        [JsonPropertyName("next_cron_time")] public DateTimeOffset NextCronTime { get; set; }

        [JsonPropertyName("wait_result")] public bool WaitResult { get; set; }

        [JsonPropertyName("concurrent")] public bool Concurrent { get; set; }
    }

    public class DtmBranch
    {
        [JsonPropertyName("id")] public int Id { get; set; }

        [JsonPropertyName("create_time")] public DateTimeOffset CreateTime { get; set; }

        [JsonPropertyName("update_time")] public DateTimeOffset UpdateTime { get; set; }

        [JsonPropertyName("gid")] public string Gid { get; set; }

        [JsonPropertyName("url")] public string Url { get; set; }

        [JsonPropertyName("bin_data")] public string BinData { get; set; }

        [JsonPropertyName("branch_id")] public string BranchId { get; set; }

        [JsonPropertyName("op")] public string Op { get; set; }

        [JsonPropertyName("status")] public string Status { get; set; }
    }

    public class TransactionStep
    {
        [JsonPropertyName("action")] public string Action { get; set; }
    }
}