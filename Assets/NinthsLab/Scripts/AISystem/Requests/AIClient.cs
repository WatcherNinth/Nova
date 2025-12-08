using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace AIEngine.Network
{
    public class AIClient : MonoBehaviour
    {
        // ==========================================
        // 配置 (建议在 Inspector 中设置)
        // ==========================================
        [Header("Server Config")]
        [Tooltip("例如: https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions")]
        public string baseUrl = "YOUR_BASE_URL_HERE";

        [Tooltip("你的 API Key")]
        public string apiKey = "YOUR_API_KEY_HERE";

        [Header("Debug")]
        public bool printRawResponse = true;

        // ==========================================
        // 核心请求方法
        // ==========================================

        /// <summary>
        /// 发送请求给 AI 服务器
        /// </summary>
        /// <param name="jsonPayload">由 AIRequestBuilder 生成的 JSON 字符串</param>
        /// <param name="onSuccess">成功回调：(解析后的业务数据, 原始API返回JSON)</param>
        /// <param name="onFailure">失败回调：(HTTP状态码, 错误信息)</param>
        public void SendRequest(string jsonPayload, Action<AIRefereeResult, string> onSuccess, Action<long, string> onFailure)
        {
            if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(apiKey))
            {
                // 本地配置错误，状态码传 0
                onFailure?.Invoke(0, "[AIClient] API Key 或 Base URL 未配置！请在 Inspector 中检查设置。");
                return;
            }

            StartCoroutine(PostCoroutine(jsonPayload, onSuccess, onFailure));
        }

        private IEnumerator PostCoroutine(string jsonPayload, Action<AIRefereeResult, string> onSuccess, Action<long, string> onFailure)
        {
            string finalUrl = baseUrl.Trim();
            // 1. 创建 UnityWebRequest
            using (UnityWebRequest request = new UnityWebRequest(finalUrl, "POST"))
            {
                // 2. 设置 Body
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();

                // 3. 设置 Headers
                request.SetRequestHeader("Content-Type", "application/json");
                // 注意：大多数 OpenAI 兼容接口使用 "Bearer <token>" 格式
                request.SetRequestHeader("Authorization", $"Bearer {apiKey.Trim()}");

                Debug.Log($"<color=cyan>[AIClient 诊断]</color>\n" +
                          $"1. URL: {finalUrl}\n" +
                          $"2. Method: POST\n" +
                          $"3. Headers: Authorization: Bearer {apiKey.Substring(0, 5)}***\n" +
                          $"4. Body Payload:\n{jsonPayload}");

                var operation = request.SendWebRequest();

                // 循环等待直到完成
                while (!operation.isDone)
                {
                    // 打印进度 (防止卡死没反应)
                    // 上传阶段 progress 会很快到 1，下载阶段可能会停顿，这是正常的
                    Debug.Log($"[AIClient] 请求处理中... {(request.uploadProgress + request.downloadProgress) / 2 * 100:F1}%");
                    
                    // 等待下一帧
                    yield return null; 
                }

                // 4. 发送并等待
                // yield return request.SendWebRequest();

                // 5. 获取 HTTP 状态码
                long responseCode = request.responseCode;
                string responseText = request.downloadHandler.text;

                // 6. 处理结果
                if (request.result == UnityWebRequest.Result.ConnectionError || 
                    request.result == UnityWebRequest.Result.ProtocolError)
                {
                    // 网络错误或 HTTP 4xx/5xx 错误
                    string errorMsg = $"[AIClient] Network/HTTP Error: {request.error}\nBody: {responseText}";
                    
                    if (printRawResponse)
                    {
                        Debug.LogError(errorMsg);
                    }
                    
                    onFailure?.Invoke(responseCode, errorMsg);
                }
                else
                {
                    // 请求成功 (200 OK)
                    if (printRawResponse)
                    {
                        Debug.Log($"[AIClient] Raw Response ({responseCode}): {responseText}");
                    }

                    // 进入解析流程
                    HandleResponseParsing(responseText, onSuccess, onFailure, responseCode);
                }
            }
        }

        /// <summary>
        /// 处理双层 JSON 解析逻辑
        /// </summary>
        private void HandleResponseParsing(string rawJson, Action<AIRefereeResult, string> onSuccess, Action<long, string> onFailure, long responseCode)
        {
            try
            {
                // 步骤 A: 反序列化外层 (OpenAI 格式信封)
                var apiResponse = JsonConvert.DeserializeObject<AIResponseRoot>(rawJson);

                if (apiResponse == null || apiResponse.Choices == null || apiResponse.Choices.Count == 0)
                {
                    onFailure?.Invoke(responseCode, "[AIClient] API 返回内容为空或 Choices 列表为空。");
                    return;
                }

                // 获取 AI 回复的文本内容
                string innerContent = apiResponse.Choices[0].Message.Content;
                
                // 清洗可能存在的 Markdown 标记
                innerContent = CleanMarkdownJson(innerContent);

                if (printRawResponse)
                {
                    Debug.Log($"[AIClient] Logic JSON Content: {innerContent}");
                }

                // 步骤 B: 反序列化内层 (Refree 业务数据)
                var refereeResult = JsonConvert.DeserializeObject<AIRefereeResult>(innerContent);

                if (refereeResult == null)
                {
                    onFailure?.Invoke(responseCode, "[AIClient] 无法解析内部逻辑 JSON (AIRefereeResult)。");
                    return;
                }

                // 成功！将业务对象和原始 JSON 一并返回
                onSuccess?.Invoke(refereeResult, rawJson);
            }
            catch (Exception ex)
            {
                onFailure?.Invoke(responseCode, $"[AIClient] 解析异常: {ex.Message}");
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// 简单的清洗函数，移除 ```json 和 ``` 包裹
        /// </summary>
        private string CleanMarkdownJson(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            text = text.Trim();
            // 移除开头的 ```json 或 ```
            if (text.StartsWith("```json"))
            {
                text = text.Substring(7);
            }
            else if (text.StartsWith("```"))
            {
                text = text.Substring(3);
            }

            // 移除结尾的 ```
            if (text.EndsWith("```"))
            {
                text = text.Substring(0, text.Length - 3);
            }
            return text.Trim();
        }
    }
}