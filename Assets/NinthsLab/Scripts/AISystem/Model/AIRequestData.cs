using System;
using System.Collections.Generic;
using Newtonsoft.Json; // 需要 Unity 工程中包含 Newtonsoft.Json

namespace AIEngine.Network
{
    // ==========================================
    // 顶层请求体
    // ==========================================
    [Serializable]
    public class AIRequestBody
    {
        [JsonProperty("model")]
        public string Model;

        [JsonProperty("messages")]
        public List<AIMessage> Messages;

        [JsonProperty("temperature")]
        public float Temperature;

        [JsonProperty("response_format")]
        public AIResponseFormat ResponseFormat;
    }

    // ==========================================
    // 消息结构
    // ==========================================
    [Serializable]
    public class AIMessage
    {
        [JsonProperty("role")]
        public string Role; // "system" or "user"

        [JsonProperty("content")]
        public string Content;

        public AIMessage(string role, string content)
        {
            Role = role;
            Content = content;
        }
    }

    // ==========================================
    // 响应格式设置 (强制 JSON)
    // ==========================================
    [Serializable]
    public class AIResponseFormat
    {
        [JsonProperty("type")]
        public string Type; // e.g., "json_object"
    }
}