using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AIEngine.Network
{
    // ==========================================
    // 1. 裁判模型最终业务结果 (Clean Data)
    // ==========================================
    [Serializable]
    public class AIRefereeResult
    {
        /// <summary>
        /// 通过判定的节点 ID 列表
        /// </summary>
        public List<string> PassedNodeIds = new List<string>();

        /// <summary>
        /// [修改] 关键词/实体提取结果 (只存 ID)
        /// </summary>
        public List<string> EntityList = new List<string>();
    }

    // ==========================================
    // 2. 原始 JSON 映射类 (Raw DTO)
    // ==========================================
    [Serializable]
    public class AIRefereeRawJson
    {
        [JsonProperty("reasoning")]
        public string Reasoning;

        [JsonProperty("user_opinion")]
        public string UserOpinion;

        [JsonProperty("node_confidence")]
        public Dictionary<string, float> NodeConfidence;

        // [修改] 对应 Prompt 中的 "entity_list"
        [JsonProperty("entity_list")]
        public List<string> EntityList;
    }
}