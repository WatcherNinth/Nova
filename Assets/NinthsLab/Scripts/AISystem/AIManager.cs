using UnityEngine;
using System.Collections.Generic;
using LogicEngine.LevelGraph;
using AIEngine.Network;
using AIEngine.Logic;
using Interrorgation.MidLayer;

namespace AIEngine
{
    [RequireComponent(typeof(AIClient))]
    public class AIManager : MonoBehaviour
    {
        [Header("Configuration")]
        public string refereeModelName = "qwen-plus";
        public string discoveryModelName = "qwen-plus";

        private AIClient _client;

        private void Awake()
        {
            _client = GetComponent<AIClient>();
        }

        private void OnEnable()
        {
            AIEventDispatcher.OnPlayerInputString += HandlePlayerInput;
        }

        private void OnDisable()
        {
            AIEventDispatcher.OnPlayerInputString -= HandlePlayerInput;
        }

        /// <summary>
        /// 处理玩家输入：并行调度 Referee 和 Discovery 模型
        /// </summary>
        private void HandlePlayerInput(LevelGraphData levelGraph, string currentPhaseId, string playerInput)
        {
            Debug.Log($"<color=cyan>[AIManager] 收到输入: {playerInput} (Phase: {currentPhaseId})</color>");

            // 准备最终的聚合结果容器
            AIResponseData finalResponse = new AIResponseData();
            
            // 计数器：记录还有几个请求在飞
            // 我们默认 Referee 肯定要跑，Discovery 可能跑也可能不跑
            int pendingRequests = 0;

            // =========================================================
            // 1. 发起 Referee 请求 (裁判模型)
            // =========================================================
            string refereePayload = AIRefereeModel.CreateRequestPayload(levelGraph, currentPhaseId, playerInput, refereeModelName);
            if (!string.IsNullOrEmpty(refereePayload))
            {
                pendingRequests++;
                _client.Post(refereePayload, 
                    (raw) => {
                        // 成功回调：解析并填入 RefereeResult
                        var data = AIRefereeModel.ParseResponse(raw);
                        if (!data.HasError) finalResponse.RefereeResult = data.RefereeResult;
                        else Debug.LogError($"[Referee Error] {data.ErrorMessage}");
                        
                        CheckAllRequestsDone(ref pendingRequests, finalResponse);
                    },
                    (code, err) => {
                        Debug.LogError($"[Referee Network Error] {err}");
                        CheckAllRequestsDone(ref pendingRequests, finalResponse);
                    }
                );
            }

            string discoveryPayload = AIDiscoveryModel.CreateRequestPayload(
                levelGraph, 
                currentPhaseId, 
                playerInput, 
                discoveryModelName
            );

            if (!string.IsNullOrEmpty(discoveryPayload))
            {
                pendingRequests++;
                Debug.Log("[AIManager] 检测到潜在的新线索，正在发起 Discovery 请求...");
                
                _client.Post(discoveryPayload,
                    (raw) => {
                        // 成功回调：解析并填入 DiscoveryResult
                        var data = AIDiscoveryModel.ParseResponse(raw);
                        if (!data.HasError) finalResponse.DiscoveryResult = data.DiscoveryResult;
                        else Debug.LogError($"[Discovery Error] {data.ErrorMessage}");

                        CheckAllRequestsDone(ref pendingRequests, finalResponse);
                    },
                    (code, err) => {
                        Debug.LogError($"[Discovery Network Error] {err}");
                        CheckAllRequestsDone(ref pendingRequests, finalResponse);
                    }
                );
            }
            else
            {
                Debug.Log("[AIManager] 当前阶段没有可供发现的隐藏线索，跳过 Discovery 请求。");
            }

            // 如果一开始就没有请求 (极罕见情况)，直接结束
            if (pendingRequests == 0)
            {
                DispatchErrorResult("未能构建任何有效请求");
            }
        }

        /// <summary>
        /// 检查是否所有请求都已回调，如果是，分发最终结果
        /// </summary>
        private void CheckAllRequestsDone(ref int pendingCount, AIResponseData finalData)
        {
            pendingCount--;
            
            if (pendingCount <= 0)
            {
                Debug.Log("<color=green>[AIManager] 所有 AI 请求已完成，正在合并分发结果...</color>");
                
                // 简单的错误检查：如果两个都空，可能是有问题
                if (finalData.RefereeResult == null && finalData.DiscoveryResult == null)
                {
                    finalData.HasError = true;
                    finalData.ErrorMessage = "所有模型请求均失败或无数据";
                }

                // 分发聚合后的数据
                AIEventDispatcher.DispatchResponseData(finalData);
            }
        }

        private void DispatchErrorResult(string errorMsg)
        {
            var errorData = AIResponseData.CreateError(errorMsg);
            AIEventDispatcher.DispatchResponseData(errorData);
        }
    }
}