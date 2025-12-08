using System.Text;
using LogicEngine.LevelGraph; // 引用 LevelGraphData, NodeData
using LogicEngine.Nodes;      // 引用 NodeAIInfo
using UnityEngine;
using LogicEngine;

namespace AIEngine.Prompts
{
    public static class AIPromptBuilder
    {
        // 对应 llm_service.py 中的核心规则模板
        private const string BASE_SYSTEM_TEMPLATE = @"
你的任务是作为一个推理游戏的智能助理，判断玩家的发言是否和剧本中的论点一致。

**核心工作原则 (必须严格遵守)：**
1. **禁止推理与联想**: 你的工作不是推理。你**绝对不能**猜测玩家“可能想说什么”。
2. **逐一匹配**: 你必须在思考过程中对每一个论点进行独立的“是/否”判断。
3. **实体匹配优先**: 必须首先检查玩家发言中提到的**关键实体**与论点描述是否一致。
4. **虚拟语气识别**: 忽略“假设”、“如果”等修饰词，提取核心论证内容。
5. **输出格式**: 必须且只能是一个符合要求的 JSON 对象。

**严格指令 (输出格式):**
{
  ""reasoning"": ""思考过程"",
  ""node_confidence"": { ""node_id"": 1.0, ""node_id_2"": 0.0 },
  ""partial_match"": { ""key"": [""关键词1""] }
}
";

        /// <summary>
        /// 主入口：构建提示词数据
        /// </summary>
        public static AIPromptData Build(LevelGraphData graphData, string currentPhaseId, string playerInput)
        {
            var promptData = new AIPromptData();

            // 1. 设置系统基础指令
            promptData.SystemInstruction = BASE_SYSTEM_TEMPLATE.Trim();
            promptData.UserInput = playerInput;

            // 2. 组合动态上下文 (节点 + 实体)
            StringBuilder contextBuilder = new StringBuilder();

            // 分别调用收集方法
            string nodesContext = CollectLogicNodes(graphData, currentPhaseId);
            string entitiesContext = CollectEntities(graphData);

            contextBuilder.Append(nodesContext);
            contextBuilder.Append(entitiesContext);

            promptData.DynamicContext = contextBuilder.ToString();

            return promptData;
        }

        /// <summary>
        /// 步骤A: 收集并格式化逻辑节点信息
        /// </summary>
        private static string CollectLogicNodes(LevelGraphData graphData, string currentPhaseId)
        {
            if (graphData.nodeLookup == null) return "";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("**逻辑节点列表 (供你分析和匹配):**");

            foreach (var kvp in graphData.nodeLookup)
            {
                string nodeId = kvp.Key;
                var info = kvp.Value;

                // 筛选逻辑：全局节点 或 当前阶段节点
                bool isValid = info.IsUniversal || (info.OwnerPhaseId == currentPhaseId);

                if (isValid && info.Node != null)
                {
                    // 直接从 NodeData 获取给 AI 看的描述
                    string desc = ExtractNodeDescription(info.Node);
                    sb.AppendLine($"- {nodeId}: {desc}");
                }
            }
            sb.AppendLine(); // 空一行
            return sb.ToString();
        }

        /// <summary>
        /// 步骤B: 收集并格式化实体列表
        /// </summary>
        private static string CollectEntities(LevelGraphData graphData)
        {
            if (graphData.entityListData == null || graphData.entityListData.Data == null) return "";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("**实体列表（用于 partial_match 判断）:**");
            sb.AppendLine("{");

            foreach (var kvp in graphData.entityListData.Data)
            {
                string key = kvp.Key;
                var entity = kvp.Value;

                // 格式化 Alias 数组: ["别名1", "别名2"]
                string aliases = entity.Alias != null && entity.Alias.Count > 0
                    ? $"\"{string.Join("\", \"", entity.Alias)}\""
                    : "";

                // 格式化单行 JSON: "key": { "name": "名字", "alias": [...] },
                sb.AppendLine($"  \"{key}\": {{ \"name\": \"{entity.Name}\", \"alias\": [{aliases}] }},");
            }

            sb.AppendLine("}");
            return sb.ToString();
        }

        /// <summary>
        /// 辅助方法：专门负责从 NodeData 中提取 AI 需要的文本
        /// </summary>
        private static string ExtractNodeDescription(NodeData node)
        {
            if (node == null) return "无数据";

            // 1. 优先使用 NodeAIInfo 中的 Prompt (专门为 AI 设计的提示语)
            if (node.AI != null && !string.IsNullOrEmpty(node.AI.Prompt))
            {
                return node.AI.Prompt;
            }

            // 2. 降级使用 NodeBasicInfo 中的 Description (给玩家看的描述)
            if (node.Basic != null && !string.IsNullOrEmpty(node.Basic.Description))
            {
                return node.Basic.Description;
            }

            return "无描述";
        }
    }
}