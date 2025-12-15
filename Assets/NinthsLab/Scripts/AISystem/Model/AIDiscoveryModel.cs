using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;
using LogicEngine.LevelGraph; // 引用节点数据
using AIEngine.Network;       // 引用数据类
using AIEngine.Prompts;       // 引用 AIPromptData
using LogicEngine;

namespace AIEngine.Logic
{
    public static class AIDiscoveryModel
    {
        private const string DEFAULT_MODEL = "qwen-plus";

        // =========================================================
        // 1. 构建请求 (Create Payload)
        // =========================================================
        
        /// <summary>
        /// 构建发现者模型的请求。需要传入当前已知的状态，以排除已发现的内容。
        /// </summary>
        public static string CreateRequestPayload(
            LevelGraphData graphData, 
            string currentPhaseId, 
            string playerInput, 
            HashSet<string> alreadyDiscoveredIds, // 存档中记录的已发现列表
            HashSet<string> currentHandCardIds,   // 玩家当前手牌(选项)列表
            string overrideModelName = null)
        {
            // 1. 筛选候选节点 (Candidates)
            var candidates = new Dictionary<string, string>();
            
            if (graphData.nodeLookup != null)
            {
                foreach (var kvp in graphData.nodeLookup)
                {
                    string nodeId = kvp.Key;
                    var info = kvp.Value;
                    
                    // A. 基础状态检查: 必须是 Locked 或 Pending (未证明)
                    // (此处假设你能在 LogicEngine 获取节点状态，或者在此处简化逻辑，交给 AI 之后再在 Game 层过滤)
                    // 但为了节省 Token，最好在这里就过滤掉属于 "非当前阶段" 的节点
                    
                    // B. 阶段检查: 必须属于当前激活阶段 或 全局节点
                    if (!info.IsUniversal && info.OwnerPhaseId != currentPhaseId) continue;

                    // C. 重复检查: 如果已经发现过，或者是手牌里的，跳过
                    if (alreadyDiscoveredIds != null && alreadyDiscoveredIds.Contains(nodeId)) continue;
                    if (currentHandCardIds != null && currentHandCardIds.Contains(nodeId)) continue;

                    // D. 获取描述
                    // 只有有描述的节点才值得“被发现”
                    string desc = ExtractDescription(info.Node);
                    if (!string.IsNullOrEmpty(desc))
                    {
                        candidates.Add(nodeId, desc);
                    }
                }
            }

            // 如果没有候选者，直接返回 null，Manager 应该跳过这次请求
            if (candidates.Count == 0) return null;

            // 2. 构建 Prompt
            AIPromptData promptData = BuildDiscoveryPrompt(playerInput, candidates);

            // 3. 序列化
            string modelName = string.IsNullOrEmpty(overrideModelName) ? DEFAULT_MODEL : overrideModelName;
            return AIRequestBuilder.ConstructPayload(promptData, modelName);
        }

        // =========================================================
        // 2. 解析结果 (Parse Response)
        // =========================================================
        
        public static AIResponseData ParseResponse(string rawResponseJson)
        {
            try
            {
                // A. 解包 OpenAI 外壳
                var standardResponse = JsonConvert.DeserializeObject<OpenAIStandardResponse>(rawResponseJson);
                if (standardResponse == null || standardResponse.Choices == null || standardResponse.Choices.Count == 0)
                    return AIResponseData.CreateError("API 返回空");

                string innerContent = CleanMarkdownJson(standardResponse.Choices[0].Message.Content);
                Debug.Log($"[AIDiscoveryModel] Logic JSON: {innerContent}");

                // B. 反序列化业务数据
                var rawResult = JsonConvert.DeserializeObject<AIDiscoveryRawJson>(innerContent);
                if (rawResult == null) return AIResponseData.CreateError("无法解析 Discovery JSON");

                // C. 转换为结果对象
                AIDiscoveryResult result = new AIDiscoveryResult();
                result.DiscoveredNodeIds = rawResult.DiscoveredIds ?? new List<string>();

                // D. 返回
                var responseData = new AIResponseData();
                responseData.DiscoveryResult = result; // 赋值给 Discovery 字段
                responseData.HasError = false;
                return responseData;
            }
            catch (System.Exception ex)
            {
                return AIResponseData.CreateError($"Discovery 解析异常: {ex.Message}");
            }
        }

        // =========================================================
        // 内部辅助逻辑
        // =========================================================

        private static AIPromptData BuildDiscoveryPrompt(string input, Dictionary<string, string> candidates)
        {
            // 构造候选列表字符串
            StringBuilder sb = new StringBuilder();
            foreach (var kvp in candidates)
            {
                sb.AppendLine($"- {kvp.Key}: {kvp.Value}");
            }

            string systemPrompt = @"
你的任务是判断玩家的发言是否涉及、暗示或询问了以下列表中的某个话题。
如果玩家的发言与某个话题相关（即使只是稍微沾边、或者是对该话题的提问），就认为玩家“发现”了这个话题。

**工作原则**：
1. **语义相关性**：不需要完全匹配。例如，如果话题是“手表的时间是伪造的”，玩家问“手表有问题吗？”或“时间对不上”，都算作发现。
2. **模糊匹配**：如果玩家提到了话题中的关键物体（如“血迹”、“涂鸦”），通常也算作发现。
3. **宁滥勿缺**：如果拿不准，倾向于让玩家发现。

**严格指令 (输出格式):**
请返回一个 JSON 对象，包含键 ""discovered_ids"" (列表)。
示例: { ""discovered_ids"": [""node_id_1"", ""node_id_3""] }
";
            
            var data = new AIPromptData();
            data.SystemInstruction = systemPrompt.Trim();
            // 上下文是候选列表
            data.DynamicContext = $"**候选话题列表:**\n{sb.ToString()}";
            data.UserInput = input;
            
            return data;
        }

        private static string ExtractDescription(NodeData node)
        {
            if (node == null) return null;
            // 发现者模型通常看 Prompt 或 Description
            if (node.AI != null && !string.IsNullOrEmpty(node.AI.Prompt)) return node.AI.Prompt;
            if (node.Basic != null && !string.IsNullOrEmpty(node.Basic.Description)) return node.Basic.Description;
            return null;
        }

        private static string CleanMarkdownJson(string text)
        {
            text = text.Trim();
            if (text.StartsWith("```json")) text = text.Substring(7);
            else if (text.StartsWith("```")) text = text.Substring(3);
            if (text.EndsWith("```")) text = text.Substring(0, text.Length - 3);
            return text.Trim();
        }
    }
}