using System.Text.Json.Serialization;

namespace BusiGrpcService.Dtos
{
    public class BusiRequest
    {
        [JsonPropertyName("amount")]
        public long Amount { get; set; }
        
        [JsonPropertyName("transOutResult")]
        public string TransOutResult { get; set; } = string.Empty;
        
        [JsonPropertyName("transInResult")]
        public string TransInResult { get; set; } = string.Empty;
    }
    
    public class BusiReply
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}