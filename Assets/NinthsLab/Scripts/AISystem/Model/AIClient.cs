using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace AIEngine.Network
{
    /// <summary>
    /// 纯粹的网络传输层。
    /// 只负责将字符串发送到指定 URL，并返回字符串结果。
    /// 不包含任何业务逻辑解析。
    /// </summary>
    public class AIClient : MonoBehaviour
    {
        [Header("Server Config")]
        public string baseUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions";
        public string apiKey = ""; // 在 Inspector 中设置

        [Header("Settings")]
        public int timeoutSeconds = 60;
        public bool debugLog = true;

        /// <summary>
        /// 发送 Post 请求
        /// </summary>
        /// <param name="jsonPayload">完整的 JSON 字符串</param>
        /// <param name="onSuccess">成功回调：返回服务器原始 Response String</param>
        /// <param name="onFailure">失败回调：返回错误信息 (状态码, 错误文本)</param>
        public void Post(string jsonPayload, Action<string> onSuccess, Action<long, string> onFailure)
        {
            if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(apiKey))
            {
                onFailure?.Invoke(0, "[AIClient] URL 或 API Key 未配置");
                return;
            }

            StartCoroutine(PostCoroutine(jsonPayload, onSuccess, onFailure));
        }

        private IEnumerator PostCoroutine(string jsonPayload, Action<string> onSuccess, Action<long, string> onFailure)
        {
            // 去除空格防止配置错误
            string finalUrl = baseUrl.Trim();
            string finalKey = apiKey.Trim();

            using (UnityWebRequest request = new UnityWebRequest(finalUrl, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();

                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {finalKey}");
                request.timeout = timeoutSeconds;

                if (debugLog) Debug.Log($"[AIClient] Sending to {finalUrl}...");

                yield return request.SendWebRequest();

                long responseCode = request.responseCode;

                if (request.result == UnityWebRequest.Result.ConnectionError || 
                    request.result == UnityWebRequest.Result.ProtocolError)
                {
                    string err = $"Network Error: {request.error}\nBody: {request.downloadHandler.text}";
                    Debug.LogError($"[AIClient] {err}");
                    onFailure?.Invoke(responseCode, err);
                }
                else
                {
                    string rawResponse = request.downloadHandler.text;
                    if (debugLog) Debug.Log($"[AIClient] Success ({responseCode}): {rawResponse}");
                    onSuccess?.Invoke(rawResponse);
                }
            }
        }
    }
}