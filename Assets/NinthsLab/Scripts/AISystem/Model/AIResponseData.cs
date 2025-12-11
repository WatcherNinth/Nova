using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AIEngine.Network
{
    // ==========================================
    // 1. 最终给游戏逻辑使用的数据 (经过清洗)
    // ==========================================
    [Serializable]
    public class AIResponseData
    {
        [JsonProperty("reasoning")]
        public string Reasoning;

        [JsonProperty("node_confidence")]
        public Dictionary<string, float> NodeConfidence;
        
        [JsonProperty("partial_match")]
        public Dictionary<string, List<string>> PartialMatch;
        
        [JsonProperty("user_opinion")]
        public string UserOpinion;
        
        // 如果出错，这里会有信息
        public bool HasError;
        public string ErrorMessage;

        public static AIResponseData CreateError(string msg)
        {
            return new AIResponseData { HasError = true, ErrorMessage = msg };
        }
    }

    // ==========================================
    // 2. 网络层原始数据结构 (OpenAI 协议标准)
    // 仅在 AIRefereeModel 内部解析时使用
    // ==========================================
    [Serializable]
    public class OpenAIStandardResponse
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("choices")] public List<OpenAIChoice> Choices;
    }

    [Serializable]
    public class OpenAIChoice
    {
        [JsonProperty("message")] public OpenAIMessage Message;
    }

    [Serializable]
    public class OpenAIMessage
    {
        [JsonProperty("content")] public string Content;
    }
}