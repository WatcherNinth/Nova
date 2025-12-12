using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AIEngine.Network
{
    // ==========================================
    // 1. 发现者模型最终业务结果 (Clean Data)
    // ==========================================
    [Serializable]
    public class AIDiscoveryResult
    {
        /// <summary>
        /// AI 判定玩家“发现”了的节点 ID 列表
        /// </summary>
        public List<string> DiscoveredNodeIds = new List<string>();
    }

    // ==========================================
    // 2. 原始 JSON 映射类 (Raw DTO)
    // ==========================================
    [Serializable]
    public class AIDiscoveryRawJson
    {
        // 对应 Prompt 中要求的 "discovered_ids"
        [JsonProperty("discovered_ids")]
        public List<string> DiscoveredIds;
    }
}