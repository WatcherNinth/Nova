using System.Collections.Generic;
using AIEngine.Prompts;
using Newtonsoft.Json;
using UnityEngine;

namespace AIEngine.Network
{
    public static class AIRequestBuilder
    {
        // 默认配置
        private const string DEFAULT_MODEL = "qwen3-max-2025-09-23"; // 示例模型名
        private const float DEFAULT_TEMPERATURE = 0.7f;

        /// <summary>
        /// 将 PromptData 转换为准备发送的 JSON 字符串
        /// </summary>
        public static string ConstructPayload(AIPromptData promptData, string modelName = null)
        {
            if (promptData == null)
            {
                Debug.LogError("[AIEngine] PromptData is null!");
                return string.Empty;
            }

            // 1. 构建消息链
            var messages = new List<AIMessage>
            {
                // System Message: 包含规则 + 上下文
                new AIMessage("system", promptData.FullSystemMessage),
                
                // User Message: 玩家输入
                // 注意：在 Python 原版中，prompt 被格式化到了 user content 里
                // 这里我们采用标准的 Chat 结构：System 放设定，User 放输入
                new AIMessage("user", $"# 玩家输入: \"{promptData.UserInput}\"")
            };

            // 2. 构建请求体对象
            var requestBody = new AIRequestBody
            {
                Model = string.IsNullOrEmpty(modelName) ? DEFAULT_MODEL : modelName,
                Messages = messages,
                Temperature = DEFAULT_TEMPERATURE,
                ResponseFormat = new AIResponseFormat { Type = "json_object" }
            };

            // 3. 序列化为 JSON 字符串
            // 使用 Formatting.None 压缩体积，或者 Formatting.Indented 用于 Debug
            try 
            {
                return JsonConvert.SerializeObject(requestBody, Formatting.None);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AIEngine] JSON Serialization failed: {ex.Message}");
                return string.Empty;
            }
        }
    }
}