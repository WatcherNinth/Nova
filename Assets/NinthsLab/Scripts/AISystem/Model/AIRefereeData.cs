using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AIEngine.Network
{
    // ==========================================
    // 1. 裁判模型最终业务结果 (Clean Data)
    // 只包含逻辑层关心的：通过了哪些节点，匹配了哪些关键词
    // ==========================================
    [Serializable]
    public class AIRefereeResult
    {
        /// <summary>
        /// 仅包含通过判定阈值的节点 ID 列表
        /// </summary>
        public List<string> PassedNodeIds = new List<string>();

        /// <summary>
        /// 关键词提取结果
        /// </summary>
        public Dictionary<string, List<string>> PartialMatch;
    }

    // ==========================================
    // 2. 内部使用：原始 JSON 映射类 (Raw DTO)
    // 包含 AI 返回的所有字段，用于反序列化
    // ==========================================
    [Serializable]
    public class AIRefereeRawJson
    {
        // 这些字段仅用于 Debug 或内部记录，不会传给上层逻辑
        [JsonProperty("reasoning")]
        public string Reasoning;

        [JsonProperty("user_opinion")]
        public string UserOpinion;

        // 原始的 <节点ID, 分数> 字典
        [JsonProperty("node_confidence")]
        public Dictionary<string, float> NodeConfidence;

        [JsonProperty("partial_match")]
        public Dictionary<string, List<string>> PartialMatch;
    }
}