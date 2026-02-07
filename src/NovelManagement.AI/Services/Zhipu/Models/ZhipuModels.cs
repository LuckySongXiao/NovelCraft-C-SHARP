using Newtonsoft.Json;

namespace NovelManagement.AI.Services.Zhipu.Models
{
    public class ZhipuRequest
    {
        [JsonProperty("model")]
        public string Model { get; set; } = string.Empty;

        [JsonProperty("messages")]
        public List<ZhipuMessage> Messages { get; set; } = new();

        [JsonProperty("temperature")]
        public double Temperature { get; set; } = 0.7;

        [JsonProperty("max_tokens")]
        public int MaxTokens { get; set; } = 4096;

        [JsonProperty("stream")]
        public bool Stream { get; set; } = false;
        
        [JsonProperty("top_p")]
        public double TopP { get; set; } = 0.7;
        
        [JsonProperty("request_id")]
        public string? RequestId { get; set; }
    }

    public class ZhipuMessage
    {
        [JsonProperty("role")]
        public string Role { get; set; } = string.Empty;

        [JsonProperty("content")]
        public string Content { get; set; } = string.Empty;

        public static ZhipuMessage CreateUserMessage(string content) => new() { Role = "user", Content = content };
        public static ZhipuMessage CreateSystemMessage(string content) => new() { Role = "system", Content = content };
        public static ZhipuMessage CreateAssistantMessage(string content) => new() { Role = "assistant", Content = content };
    }

    public class ZhipuResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("choices")]
        public List<ZhipuChoice> Choices { get; set; } = new();

        [JsonProperty("created")]
        public long Created { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; } = string.Empty;

        [JsonProperty("usage")]
        public ZhipuUsage? Usage { get; set; }
        
        public string GetContent()
        {
            return Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
        }
    }

    public class ZhipuChoice
    {
        [JsonProperty("index")]
        public int Index { get; set; }

        [JsonProperty("message")]
        public ZhipuMessage? Message { get; set; }
        
        [JsonProperty("delta")]
        public ZhipuMessage? Delta { get; set; } // For streaming

        [JsonProperty("finish_reason")]
        public string FinishReason { get; set; } = string.Empty;
    }

    public class ZhipuUsage
    {
        [JsonProperty("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonProperty("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonProperty("total_tokens")]
        public int TotalTokens { get; set; }
    }
}
