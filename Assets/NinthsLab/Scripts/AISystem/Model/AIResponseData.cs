using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AIEngine.Network
{
    // ==========================================
    // 顶层传输容器 (Envelope)
    // ==========================================
    [Serializable]
    public class AIResponseData
    {
        // 具体的裁判业务结果
        public AIRefereeResult RefereeResult;

        public AIDiscoveryResult DiscoveryResult; 

        // 将来如果有 DiscoveryModel，可以在这里加:
        // public AIDiscoveryResult DiscoveryResult;

        // 错误信息
        public bool HasError;
        public string ErrorMessage;

        public static AIResponseData CreateError(string msg)
        {
            return new AIResponseData { HasError = true, ErrorMessage = msg };
        }
    }

    // ==========================================
    // OpenAI 协议外壳 (保持不变)
    // ==========================================
    [Serializable]
    public class OpenAIStandardResponse
    {
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