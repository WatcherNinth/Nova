using System.Text;
using LogicEngine.LevelGraph; // 引用剧本数据结构
using UnityEngine;
using LogicEngine;       // 为了访问 NodeData, PhaseData
using LogicEngine.Nodes; // 为了访问 NodeBasicInfo, NodeAIInfo

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
        /// 根据当前游戏状态构建提示词数据
        /// </summary>
        /// <param name="graphData">剧本总数据</param>
        /// <param name="currentPhaseId">当前激活的阶段ID (如 phase1)</param>
        /// <param name="playerInput">玩家输入</param>
        public static AIPromptData Build(LevelGraphData graphData, string currentPhaseId, string playerInput)
        {
            var promptData = new AIPromptData();
            
            // 1. 设置固定的系统指令
            promptData.SystemInstruction = BASE_SYSTEM_TEMPLATE.Trim();
            promptData.UserInput = playerInput;

            // 2. 构建动态上下文 (实体 + 逻辑节点)
            StringBuilder contextBuilder = new StringBuilder();

            // --- 构建逻辑节点列表 ---
            contextBuilder.AppendLine("**逻辑节点列表 (供你分析和匹配):**");
            
            // 遍历所有节点查找表
            foreach (var kvp in graphData.nodeLookup)
            {
                var nodeId = kvp.Key;
                var info = kvp.Value;

                // 筛选逻辑：只包含全局节点(Universal) 或 当前阶段的节点
                // 注意：这里假设 OwnerPhaseId 为 null 表示全局节点
                bool isValid = info.IsUniversal || (info.OwnerPhaseId == currentPhaseId);

                if (isValid && info.Node != null)
                {
                    // 假设 NodeData 有 Description 字段 (根据 Python 代码推断)
                    // 如果 NodeData 结构不同，请修改此处引用
                    // string desc = info.Node.Description ?? "无描述"; 
                    // 这里暂时用 ToString 或者需要在 NodeData 中补充字段
                    string desc = GetNodeDescription(info.Node); 
                    contextBuilder.AppendLine($"- {nodeId}: {desc}");
                }
            }

            // --- 构建实体列表 ---
            contextBuilder.AppendLine("\n**实体列表（用于 partial_match 判断）:**");
            // 这里为了节省 Token，通常不需要把整个 JSON 扔进去，而是格式化一下
            // 但为了还原 Python 逻辑，我们可以模拟 JSON 结构或简化列表
            if (graphData.entityListData != null && graphData.entityListData.Data != null)
            {
                contextBuilder.AppendLine("{");
                foreach (var kvp in graphData.entityListData.Data)
                {
                    var entity = kvp.Value;
                    string aliases = entity.Alias != null ? string.Join(", ", entity.Alias) : "";
                    contextBuilder.AppendLine($"  \"{kvp.Key}\": {{ \"name\": \"{entity.Name}\", \"alias\": [{aliases}] }},");
                }
                contextBuilder.AppendLine("}");
            }

            promptData.DynamicContext = contextBuilder.ToString();

            return promptData;
        }

        // 辅助方法：从 NodeData 获取描述文本
        private static string GetNodeDescription(NodeData node)
        {
            if (node == null) return "无数据";

            // 1. 优先尝试获取 AI 模块中的 Prompt (这是专门给 AI 看的)
            if (node.AI != null && !string.IsNullOrEmpty(node.AI.Prompt))
            {
                return node.AI.Prompt;
            }

            // 2. 其次尝试获取 Basic 模块中的 Description (这是给玩家看的，也可作为保底)
            if (node.Basic != null && !string.IsNullOrEmpty(node.Basic.Description))
            {
                return node.Basic.Description;
            }

            // 3. 如果都没有，返回默认值
            return "无描述";
        }
    }
}