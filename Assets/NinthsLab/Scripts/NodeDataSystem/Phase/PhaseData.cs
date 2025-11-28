using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using LogicEngine.Phase;
using LogicEngine.Validation;

namespace LogicEngine
{
    /// <summary>
    /// 阶段数据的核心结构
    /// </summary>
    public class PhaseData : IValidatable
    {
        /// <summary>
        /// 阶段名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 对话配置信息
        /// </summary>
        [JsonProperty("dialogue")]
        public PhaseDialogueInfo Dialogue { get; set; }

        /// <summary>
        /// 依赖条件，直接存储为JToken以便后续解析逻辑处理
        /// </summary>
        [JsonProperty("depends_on")]
        public JToken DependsOn { get; set; }

        /// <summary>
        /// 是否隐藏，默认为false
        /// </summary>
        [JsonProperty("is_hidden")]
        public bool IsHidden { get; set; } = false;

        /// <summary>
        /// 节点字典
        /// Key: 节点ID
        /// Value: NodeData (假设该类已在LogicEngine中定义)
        /// </summary>
        [JsonProperty("nodes")]
        public Dictionary<string, NodeData> Nodes { get; set; }

        /// <summary>
        /// 结束当前阶段所需的结论节点键值列表
        /// </summary>
        [JsonProperty("completion_nodes")]
        public List<string> CompletionNodes { get; set; }

        /// <summary>
        /// 实现IValidatable接口的校验方法
        /// </summary>
        /// <param name="context">校验上下文</param>
        public void OnValidate(ValidationContext context)
        {
            bool dependsonEmpty = DependsOn == null && !DependsOn.HasValues;
            if (dependsonEmpty && IsHidden)
            {
                context.LogWarning("检测到DependsOn字段为空且IsHidden为true，没有前置条件的的隐藏阶段无法进入！");
            }
            context.ValidateChild("DialogueModule", Dialogue);
            foreach (var node in Nodes)
            {
                context.ValidateChild($"Nodes_{node.Key}", node.Value);
            }
            using (context.Scope("completion_nodes"))
            {
                if (CompletionNodes == null || CompletionNodes.Count == 0)
                {
                    context.LogError("completion_nodes永远不应该为空");
                }
                foreach (var node in CompletionNodes)
                {
                    if (!LevelGraphContext.CurrentGraph.allIds.Contains(node))
                    {
                        context.LogError($"completion_nodes里发现了一个{node}，但LevelGraph里并不存在这个键值");
                    }
                }
            }
        }
    }
}

namespace LogicEngine.Phase
{
    /// <summary>
    /// 阶段对话信息的具体配置
    /// </summary>
    public class PhaseDialogueInfo:IValidatable
    {
        /// <summary>
        /// 阶段开始时的对话数据，存储为JToken以应对可能的空值或复杂结构
        /// </summary>
        [JsonProperty("on_phase_start")]
        public JToken OnPhaseStart { get; set; }

        /// <summary>
        /// 阶段结束时的对话数据，存储为JToken
        /// </summary>
        [JsonProperty("on_phase_complete")]
        public JToken OnPhaseComplete { get; set; }

        public void OnValidate(ValidationContext context)
        {
            // 根据需求留空，后续可在逻辑层扩展校验逻辑
        }
    }
}