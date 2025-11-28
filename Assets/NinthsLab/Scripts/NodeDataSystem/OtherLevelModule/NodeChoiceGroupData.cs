using System.Collections.Generic;
using Newtonsoft.Json;
using LogicEngine.Validation;

namespace LogicEngine.LevelGraph
{
    public class NodeChoiceItem : IValidatable
    {
        // [新增] 自动生成的ID，对应JSON里的 Key
        [JsonIgnore] 
        public string TargetNodeId { get; set; }

        [JsonProperty("text_override")]
        public string TextOverride { get; set; }

        [JsonProperty("is_hidden")]
        public bool IsHidden { get; set; } = false;

        [JsonProperty("display_anyway")]
        public bool DisplayAnyway { get; set; } = false;

        public void OnValidate(ValidationContext context)
        {
            if (string.IsNullOrEmpty(TargetNodeId))
            {
                context.LogError("NodeChoiceItem 的 TargetNodeId 为空，解析可能出现了问题。");
            }

            // 这里可以利用 LevelGraphContext 检查 TargetNodeId 是否存在于全局节点中
            var graph = LevelGraphContext.CurrentGraph;
            if (graph != null)
            {
                if (!graph.nodeLookup.ContainsKey(TargetNodeId))
                {
                    context.LogError($"选项组指向了一个不存在的节点 ID: '{TargetNodeId}'");
                }
            }
        }
    }

    public class NodeChoiceGroupData : IValidatable
    {
        // 改动：值变成了 List，不再是嵌套字典
        // Key: GroupID (例如 "test_group")
        // Value: 该组下的所有选项列表
        public Dictionary<string, List<NodeChoiceItem>> Data { get; set; } 
            = new Dictionary<string, List<NodeChoiceItem>>();

        public void OnValidate(ValidationContext context)
        {
            if (Data == null) return;

            foreach (var kvp in Data)
            {
                string groupId = kvp.Key;
                List<NodeChoiceItem> options = kvp.Value;

                // 使用 context 提供的 helper 来验证列表
                context.ValidateList($"ChoiceGroup_{groupId}", options);
            }
        }
        
        /// <summary>
        /// 辅助方法：获取指定组的所有选项
        /// </summary>
        public List<NodeChoiceItem> GetGroupOptions(string groupId)
        {
            if (Data.TryGetValue(groupId, out var list))
            {
                return list;
            }
            return null;
        }
    }
}