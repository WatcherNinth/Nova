using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;
using LogicEngine.LevelGraph;
using AIEngine.Network;
using AIEngine.Prompts;
using LogicEngine.Nodes; // 引用 NodeAIInfo, NodeBasicInfo
using LogicEngine;
using Interrorgation.MidLayer;

namespace AIEngine.Logic
{
    public static class AIDiscoveryModel
    {
        private const string DEFAULT_MODEL = "qwen-plus";

        // [修改] 移除了 HashSet 参数
        public static string CreateRequestPayload(
            LevelGraphData graphData, 
            string currentPhaseId, 
            string playerInput, 
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
                    NodeData node = info.Node;

                    // A. 阶段检查 (保持不变)：必须属于当前激活阶段 或 全局节点
                    if (!info.IsUniversal && info.OwnerPhaseId != currentPhaseId) 
                    {
                        continue;
                    }
                    // C. 提取描述
                    string desc = ExtractDescription(node);
                    
                    // D. 有效性检查
                    if (!string.IsNullOrEmpty(desc))
                    {
                        candidates.Add(nodeId, desc);
                    }
                }
            }

            if (candidates.Count == 0) 
            {
                // Debug.LogWarning($"[AIDiscoveryModel] 在阶段 '{currentPhaseId}' 没有找到任何候选节点。");
                return null;
            }

            // 2. 构建 Prompt
            AIPromptData promptData = BuildDiscoveryPrompt(playerInput, candidates);

            // 3. 序列化
            string modelName = string.IsNullOrEmpty(overrideModelName) ? DEFAULT_MODEL : overrideModelName;
            return AIRequestBuilder.ConstructPayload(promptData, modelName);
        }

        public static AIResponseData ParseResponse(string rawResponseJson)
        {
            try
            {
                // A. 解包 OpenAI 外壳
                var standardResponse = JsonConvert.DeserializeObject<OpenAIStandardResponse>(rawResponseJson);
                if (standardResponse == null || standardResponse.Choices == null || standardResponse.Choices.Count == 0)
                    return AIResponseData.CreateError("Discovery API 返回空");

                string innerContent = CleanMarkdownJson(standardResponse.Choices[0].Message.Content);
                
                // --- Debug: 看看 AI 到底回了什么 ---
                Debug.Log($"<color=orange>[AIDiscoveryModel] AI Raw Output: {innerContent}</color>");

                // B. 反序列化业务数据
                var rawResult = JsonConvert.DeserializeObject<AIDiscoveryRawJson>(innerContent);
                if (rawResult == null) return AIResponseData.CreateError("无法解析 Discovery JSON");

                // C. 转换为结果对象
                AIDiscoveryResult result = new AIDiscoveryResult();
                result.DiscoveredNodeIds = rawResult.DiscoveredIds ?? new List<string>();

                // D. 返回
                var responseData = new AIResponseData();
                responseData.DiscoveryResult = result; 
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
            StringBuilder sb = new StringBuilder();
            foreach (var kvp in candidates)
            {
                // 格式: - node_id: 描述文本
                sb.AppendLine($"- {kvp.Key}: {kvp.Value}");
            }

            string systemPrompt = @"
你的任务是判断用户的发言是否暗示、询问或涉及了以下列表中的某个话题（线索）。
如果用户的发言与某个话题相关（即使只是模糊匹配、或者提到了话题中的关键物体），就认为用户“发现”了这个话题。

**工作原则**：
1. **语义相关**：不需要字面完全匹配。例如话题是“伪造的手表时间”，用户问“时间有问题吗”，算发现。
2. **关键词匹配**：如果用户提到了话题描述中的核心名词（如“血迹”、“涂鸦”），通常算发现。
3. **宁滥勿缺**：如果拿不准，倾向于判定为发现。

**输入数据**：
- 候选话题列表：见下文。

**输出格式 (Strict JSON)**：
请返回一个 JSON 对象，包含键 ""discovered_ids"" (字符串数组)。
如果没有发现任何话题，数组为空。

示例: { ""discovered_ids"": [""node_a"", ""node_b""] }
";
            
            return new AIPromptData 
            {
                SystemInstruction = systemPrompt.Trim(),
                DynamicContext = $"**候选话题列表:**\n{sb.ToString()}",
                UserInput = input
            };
        }

        // [修复] 适配你的 NodeData 结构
        private static string ExtractDescription(NodeData node)
        {
            if (node == null) return null;

            // 1. 优先取 AI 专用的 Prompt
            if (node.AI != null && !string.IsNullOrEmpty(node.AI.Prompt)) 
            {
                return node.AI.Prompt;
            }
            
            // 2. 其次取 Basic 里的 Description
            if (node.Basic != null && !string.IsNullOrEmpty(node.Basic.Description)) 
            {
                return node.Basic.Description;
            }

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