using UnityEngine;
using LogicEngine.LevelGraph;
using AIEngine.Network;
using AIEngine.Logic; // 引用 AIRefereeModel
using Interrorgation.MidLayer;

namespace AIEngine
{
    /// <summary>
    /// AI 系统的核心调度管理器。
    /// 职责：监听游戏输入 -> 调度业务模型构建请求 -> 调度网络层发送 -> 接收并解析 -> 分发结果
    /// </summary>
    [RequireComponent(typeof(AIClient))]
    public class AIManager : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("指定裁判逻辑使用的模型名称")]
        public string refereeModelName = "qwen-plus";

        private AIClient _client;

        private void Awake()
        {
            _client = GetComponent<AIClient>();
        }

        // =========================================================
        // 1. 订阅与取消订阅事件
        // =========================================================
        private void OnEnable()
        {
            // 订阅：当玩家输入时，触发 HandlePlayerInput
            AIEventDispatcher.OnPlayerInputString += HandlePlayerInput;
            Debug.Log("[AIManager] 已启动，正在监听玩家输入...");
        }

        private void OnDisable()
        {
            AIEventDispatcher.OnPlayerInputString -= HandlePlayerInput;
        }

        // =========================================================
        // 2. 处理逻辑 (Core Orchestration)
        // =========================================================
        private void HandlePlayerInput(LevelGraphData levelGraph, string currentPhaseId, string playerInput)
        {
            Debug.Log($"<color=cyan>[AIManager] 收到事件：玩家输入 '{playerInput}' (当前阶段: {currentPhaseId})</color>");

            // --- 步骤 A: 调用 Model 生成标准 Request ---
            // 这里调用 AIRefereeModel 的静态方法，将游戏数据转换为 JSON 字符串
            string requestPayload = AIRefereeModel.CreateRequestPayload(levelGraph, currentPhaseId, playerInput, refereeModelName);

            if (string.IsNullOrEmpty(requestPayload))
            {
                Debug.LogError("[AIManager] Request 构建失败，流程终止。");
                return;
            }

            // --- 步骤 B: 调用 AIClient 发送请求 ---
            // Manager 不关心怎么发，只管把 JSON 丢给 Client
            Debug.Log("[AIManager] 正在调用 AIClient 发送请求...");
            
            _client.Post(requestPayload, 
                // 成功回调
                onSuccess: (rawResponse) => 
                {
                    HandleNetworkSuccess(rawResponse);
                },
                // 失败回调
                onFailure: (code, error) => 
                {
                    Debug.LogError($"[AIManager] 网络请求失败。Code: {code}, Error: {error}");
                    // 这里可以根据需要分发一个错误事件，或者直接分发带错误信息的 AIResponseData
                    DispatchErrorResult($"网络错误 ({code}): {error}");
                }
            );
        }

        // =========================================================
        // 3. 结果处理与分发
        // =========================================================
        private void HandleNetworkSuccess(string rawResponseJson)
        {
            // --- 步骤 C: 调用 Model 解析 Response ---
            // 将晦涩的原始 JSON 交给 Referee Model，还原成业务数据对象
            AIResponseData finalData = AIRefereeModel.ParseResponse(rawResponseJson);

            if (finalData.HasError)
            {
                Debug.LogError($"[AIManager] 业务数据解析失败: {finalData.ErrorMessage}");
                DispatchErrorResult(finalData.ErrorMessage);
                return;
            }

            Debug.Log($"<color=green>[AIManager] 流程完成！正在分发结果... (Reasoning: {finalData.Reasoning.Substring(0, Mathf.Min(20, finalData.Reasoning.Length))}...)</color>");

            // --- 步骤 D: 最后把结果分发出去 ---
            // 触发 AIEventDispatcher.OnResponseReceived
            AIEventDispatcher.DispatchResponseData(finalData);
        }

        /// <summary>
        /// 辅助方法：分发错误信息，防止流程卡死
        /// </summary>
        private void DispatchErrorResult(string errorMsg)
        {
            var errorData = AIResponseData.CreateError(errorMsg);
            AIEventDispatcher.DispatchResponseData(errorData);
        }
    }
}