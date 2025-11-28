using System.Collections.Generic;
using Newtonsoft.Json;
using LogicEngine.Validation;

namespace LogicEngine.LevelGraph
{
    public class NodeMutexItem : IValidatable
    {
        // 情况A：如果Json值是字符串，存入这里
        public string Description { get; set; }

        // 情况B：如果Json值是列表，存入这里
        public List<string> RelatedNodeIds { get; set; }

        // 辅助属性
        [JsonIgnore]
        public bool IsGroupList => RelatedNodeIds != null && RelatedNodeIds.Count > 0;

        public void OnValidate(ValidationContext context)
        {
            // 校验逻辑待实现
        }
    }

    public class NodeMutexGroupData : IValidatable
    {
        // 核心数据：<MutexGroupID, <NodeOrGroupID, MutexItem>>
        public Dictionary<string, Dictionary<string, NodeMutexItem>> Data { get; set; } 
            = new Dictionary<string, Dictionary<string, NodeMutexItem>>();

        public void OnValidate(ValidationContext context)
        {
            // 校验逻辑待实现
        }
    }
}