/// 把所有跟裁判模型相关的逻辑都放这里面来
/// 包括prompt构建，用什么模型等等
/// 最后形成一个标准request结构
using UnityEngine;
using Newtonsoft.Json;
using LogicEngine.LevelGraph;
using AIEngine.Prompts; // 引用 AIPromptBuilder
using AIEngine.Network; // 引用 AIRequestBuilder

namespace AIEngine.Logic
{
    /// <summary>
    /// 裁判模型业务逻辑核心。
    /// 负责将游戏数据转换为 AI 请求，并将 AI 的原始回复转换为游戏数据。
    /// </summary>
    public static class AIRefereeModel
    {
        // 这里可以定义一些裁判模型特有的默认参数
        private const string DEFAULT_REFEREE_MODEL = "qwen-plus";

        /// <summary>
        /// 1. [输入处理] 构建发送给 AI 的请求体字符串
        /// </summary>
        public static string CreateRequestPayload(LevelGraphData levelGraph, string currentPhaseId, string playerInput, string overrideModelName = null)
        {
            // A. 构建 Prompt 数据
            AIPromptData promptData = AIPromptBuilder.Build(levelGraph, currentPhaseId, playerInput);

            // B. 确定使用的模型
            string modelName = string.IsNullOrEmpty(overrideModelName) ? DEFAULT_REFEREE_MODEL : overrideModelName;

            // C. 序列化为 JSON
            return AIRequestBuilder.ConstructPayload(promptData, modelName);
        }

        /// <summary>
        /// 2. [输出处理] 解析 AI 返回的原始字符串
        /// </summary>
        public static AIResponseData ParseResponse(string rawResponseJson)
        {
            try
            {
                // 步骤 A: 反序列化外层 (OpenAI 协议)
                var standardResponse = JsonConvert.DeserializeObject<OpenAIStandardResponse>(rawResponseJson);

                if (standardResponse == null || standardResponse.Choices == null || standardResponse.Choices.Count == 0)
                {
                    return AIResponseData.CreateError("API 返回结构为空");
                }

                string innerContent = standardResponse.Choices[0].Message.Content;

                // 步骤 B: 清洗 Markdown 标记 (```json ... ```)
                innerContent = CleanMarkdownJson(innerContent);

                Debug.Log($"[AIRefereeModel] Extracted Logic JSON: {innerContent}");

                // 步骤 C: 反序列化内层 (业务数据)
                // 这里我们复用 AIResponseData 作为结果容器
                // 注意：因为 AIResponseData 的字段名跟 JSON 里的 key 需要对应，
                // 如果 JSON 是 snake_case (node_confidence)，需要确保 AIResponseData 有对应的 JsonProperty 或者配置
                var result = JsonConvert.DeserializeObject<AIResponseData>(innerContent);

                if (result == null)
                {
                    return AIResponseData.CreateError("无法解析内部业务 JSON");
                }

                return result;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AIRefereeModel] Parse Error: {ex.Message}");
                return AIResponseData.CreateError($"解析异常: {ex.Message}");
            }
        }

        // 工具方法：清洗 Markdown
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