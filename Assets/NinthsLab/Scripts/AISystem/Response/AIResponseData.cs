using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AIEngine.Network
{
    // ==========================================
    // 1. 外层：OpenAI API 标准响应格式
    // ==========================================
    [Serializable]
    public class AIResponseRoot
    {
        [JsonProperty("id")]
        public string Id;

        [JsonProperty("choices")]
        public List<AIChoice> Choices;

        [JsonProperty("usage")]
        public AIUsage Usage;
    }

    [Serializable]
    public class AIChoice
    {
        [JsonProperty("message")]
        public AIMessage Message;

        [JsonProperty("finish_reason")]
        public string FinishReason;
    }

    [Serializable]
    public class AIUsage
    {
        [JsonProperty("total_tokens")]
        public int TotalTokens;
    }

    // ==========================================
    // 3. 封装好的AI反馈类
    // 通过AIResponseDispatcher统一分发，各模块只取自己所需
    // ==========================================

    [Serializable]
    public class AIResponseData
    {
        public AIRefereeResult RefereeResult;
    }

    // ==========================================
    // 2. 内层：Refree (裁判) 业务逻辑结果
    // 这是 AI 在 Content 字符串里返回的 JSON
    // ==========================================
    [Serializable]
    public class AIRefereeResult
    {
        /// <summary>
        /// AI 的思考过程
        /// </summary>
        [JsonProperty("reasoning")]
        public string Reasoning;

        /// <summary>
        /// 节点置信度字典 <NodeID, Score>
        /// </summary>
        [JsonProperty("node_confidence")]
        public Dictionary<string, float> NodeConfidence;

        /// <summary>
        /// 部分匹配结果 (关键词提取)
        /// </summary>
        [JsonProperty("partial_match")]
        public Dictionary<string, List<string>> PartialMatch;
        
        // 预留字段：提取出的用户观点总结
        [JsonProperty("user_opinion")]
        public string UserOpinion;
    }
}