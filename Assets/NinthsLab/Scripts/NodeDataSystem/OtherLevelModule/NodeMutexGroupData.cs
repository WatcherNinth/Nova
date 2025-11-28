using System.Collections.Generic;
using Newtonsoft.Json;
using LogicEngine.Validation;
using LogicEngine;

namespace LogicEngine.LevelGraph
{
    public class NodeMutexItem : IValidatable
    {
        // JSON 中的 Key (例如 "nodes_1")，仅作为条目索引，不再作为 NodeID 验证
        [JsonIgnore]
        public string KeyId { get; set; }

        // [重命名] 情况A：如果Json值是字符串，这里存的是单个互斥节点的 ID
        public string SingleNodeId { get; set; }

        // 情况B：如果Json值是列表，这里存的是一组互斥节点的 ID
        public List<string> GroupNodeIds { get; set; }

        // 辅助属性
        [JsonIgnore]
        public bool IsGroupList => GroupNodeIds != null && GroupNodeIds.Count > 0;

        public void OnValidate(ValidationContext context)
        {
            // 获取全局 Graph 数据
            var graph = LevelGraphContext.CurrentGraph;
            if (graph == null) return;

            // 逻辑分支：单项 vs 组
            if (!IsGroupList)
            {
                // Case A: 单项互斥
                // [修正] 验证 SingleNodeId (Value) 而不是 KeyId
                if (string.IsNullOrEmpty(SingleNodeId))
                {
                    context.LogError($"互斥条目 '{KeyId}' 的值为空，无法作为互斥节点 ID。");
                }
                else if (!graph.nodeLookup.ContainsKey(SingleNodeId))
                {
                    context.LogError($"互斥条目 '{KeyId}' 指向了一个不存在的 Node ID: '{SingleNodeId}'");
                }
            }
            else
            {
                // Case B: 组互斥
                // 验证 GroupNodeIds 列表里的每一个 ID
                foreach (var nodeId in GroupNodeIds)
                {
                    if (!graph.nodeLookup.ContainsKey(nodeId))
                    {
                        context.LogError($"互斥组 '{KeyId}' 包含了一个不存在的 Node ID: '{nodeId}'");
                    }
                }
            }
        }
    }

    public class NodeMutexGroupData : IValidatable
    {
        // 结构保持为 List，方便遍历
        // Key: MutexGroupID (例如 "test_mutex_group")
        public Dictionary<string, List<NodeMutexItem>> Data { get; set; } 
            = new Dictionary<string, List<NodeMutexItem>>();

        public void OnValidate(ValidationContext context)
        {
            if (Data == null) return;

            foreach (var kvp in Data)
            {
                string groupId = kvp.Key;
                List<NodeMutexItem> items = kvp.Value;
                context.ValidateList($"MutexGroup_{groupId}", items);
            }
        }

        public List<NodeMutexItem> GetMutexItems(string groupId)
        {
            if (Data.TryGetValue(groupId, out var list))
            {
                return list;
            }
            return null;
        }
    }
}