using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using LogicEngine.LevelGraph;
using AIEngine.Prompts;
using AIEngine.Network;

namespace AIEngine.Logic
{
    public static class AIRefereeModel
    {
        private const string DEFAULT_REFEREE_MODEL = "qwen-plus";
        private const float CONFIDENCE_THRESHOLD = 0.8f;

        public static string CreateRequestPayload(LevelGraphData levelGraph, string currentPhaseId, string playerInput, string overrideModelName = null)
        {
            AIPromptData promptData = AIPromptBuilder.Build(levelGraph, currentPhaseId, playerInput);
            string modelName = string.IsNullOrEmpty(overrideModelName) ? DEFAULT_REFEREE_MODEL : overrideModelName;
            return AIRequestBuilder.ConstructPayload(promptData, modelName);
        }

        public static AIResponseData ParseResponse(string rawResponseJson)
        {
            try
            {
                // 1. 解开 OpenAI 外层
                var standardResponse = JsonConvert.DeserializeObject<OpenAIStandardResponse>(rawResponseJson);
                if (standardResponse == null || standardResponse.Choices == null || standardResponse.Choices.Count == 0)
                {
                    return AIResponseData.CreateError("API 返回结构为空");
                }

                string innerContent = standardResponse.Choices[0].Message.Content;
                innerContent = CleanMarkdownJson(innerContent);

                // 2. 反序列化为 Raw DTO (包含 Reasoning 和 分数)
                var rawResult = JsonConvert.DeserializeObject<AIRefereeRawJson>(innerContent);
                if (rawResult == null)
                {
                    return AIResponseData.CreateError("无法解析 Referee 业务 JSON");
                }

                // -------------------------------------------------------------
                // [Log] 在这里打印 Reasoning，因为它不会被传给逻辑层
                // -------------------------------------------------------------
                Debug.Log($"<color=orange>[AI 思考过程]: {rawResult.Reasoning}</color>");
                // -------------------------------------------------------------

                // 3. [核心逻辑] 执行判定 (Filtering) -> 转存为 Clean Data
                AIRefereeResult cleanResult = new AIRefereeResult();
                
                // [修改] 直接传递 List，如果没有则初始化为空列表，防止空引用
                cleanResult.EntityList = rawResult.EntityList ?? new List<string>();
                
                // 筛选节点 (逻辑不变)
                if (rawResult.NodeConfidence != null)
                {
                    foreach (var kvp in rawResult.NodeConfidence)
                    {
                        string nodeId = kvp.Key;
                        float score = kvp.Value;

                        if (score >= CONFIDENCE_THRESHOLD)
                        {
                            cleanResult.PassedNodeIds.Add(nodeId);
                            Debug.Log($"<color=green>[判定通过] 节点: {nodeId} (分数: {score})</color>");
                        }
                    }
                }

                // 4. 包装返回
                var responseData = new AIResponseData();
                responseData.RefereeResult = cleanResult;
                responseData.HasError = false;

                return responseData;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AIRefereeModel] Parse Error: {ex.Message}");
                return AIResponseData.CreateError($"解析异常: {ex.Message}");
            }
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