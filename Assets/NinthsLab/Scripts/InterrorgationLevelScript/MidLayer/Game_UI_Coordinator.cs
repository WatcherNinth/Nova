using UnityEngine;
using System.Collections.Generic;
using DialogueSystem;
using AIEngine.Network;
using LogicEngine;
using LogicEngine.LevelGraph;

namespace Interrorgation.MidLayer
{
    /// <summary>
    /// 游戏与 UI 的协调器，负责在底层逻辑事件 (GameEventDispatcher) 和上层 UI 事件 (UIEventDispatcher) 之间进行编排与转换
    /// </summary>
    public class Game_UI_Coordinator : MonoBehaviour
    {
        #region 单例与引用
        private static Game_UI_Coordinator _instance;
        public static Game_UI_Coordinator Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<Game_UI_Coordinator>();
                }
                return _instance;
            }
        }

        [Header("系统引用")]
        [SerializeField] private DialogueRuntimeManager dialogueManager;
        #endregion

        #region 生命周期
        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"[Game_UI_Coordinator] 发现重复的 Coordinator 实例在 {gameObject.name} 上，正在销毁以防止重复事件触发。");
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        void OnEnable()
        {
            // 订阅 UI 事件 -> 转发给逻辑
            UIEventDispatcher.OnPlayerSubmitInput += HandlePlayerSubmitInput;
            UIEventDispatcher.OnPlayerSubmitTemplateAnswer += HandlePlayerSubmitTemplateAnswer;
            UIEventDispatcher.OnNodeOptionSubmitted += HandleNodeOptionSubmitted;

            // 订阅逻辑事件 -> 转发给 UI 或进行编排
            GameEventDispatcher.OnTemplateSettlement += HandleTemplateSettlement;
            GameEventDispatcher.OnDialogueGenerated += HandleDialogueGenerated;
            GameEventDispatcher.OnPhaseUnlockEvents += HandlePhaseUnlock;
            GameEventDispatcher.OnDiscoveredNewTemplates += HandleDiscoveredTemplates;

            // 订阅 AI 响应事件
            AIEventDispatcher.OnResponseReceived += HandleAIResponse;
        }

        void OnDisable()
        {
            UIEventDispatcher.OnPlayerSubmitInput -= HandlePlayerSubmitInput;
            UIEventDispatcher.OnPlayerSubmitTemplateAnswer -= HandlePlayerSubmitTemplateAnswer;
            UIEventDispatcher.OnNodeOptionSubmitted -= HandleNodeOptionSubmitted;

            GameEventDispatcher.OnTemplateSettlement -= HandleTemplateSettlement;
            GameEventDispatcher.OnDialogueGenerated -= HandleDialogueGenerated;
            GameEventDispatcher.OnPhaseUnlockEvents -= HandlePhaseUnlock;
            GameEventDispatcher.OnDiscoveredNewTemplates -= HandleDiscoveredTemplates;

            AIEventDispatcher.OnResponseReceived -= HandleAIResponse;
        }
        #endregion

        #region 玩家交互
        /// <summary>
        /// 处理 UI 侧提交的原始文本输入
        /// </summary>
        private void HandlePlayerSubmitInput(string input)
        {
            GameEventDispatcher.DispatchPlayerInputString(input);
        }
        #endregion

        #region 节点逻辑 (Nodes)
        /// <summary>
        /// 处理 UI 侧提交的节点选项
        /// </summary>
        private void HandleNodeOptionSubmitted(string nodeId)
        {
            GameEventDispatcher.DispatchNodeOptionSubmitted(nodeId);
        }
        #endregion

        #region 模板系统 (Templates)
        /// <summary>
        /// 处理 UI 侧提交的模板答案
        /// </summary>
        private void HandlePlayerSubmitTemplateAnswer(string templateId, List<string> answers)
        {
            GameEventDispatcher.DispatchPlayerSubmitTemplateAnswer(templateId, answers);
        }

        /// <summary>
        /// 处理逻辑侧分发的模板结算结果
        /// </summary>
        private void HandleTemplateSettlement(GameEventDispatcher.TemplateSettlementContext context)
        {
            if (context.IsSuccess)
            {
                Debug.Log("[Coordinator] 编排：模板成功序列。正在解锁目标节点并通知 UI。");
                // 1. 通知逻辑层解锁节点
                GameEventDispatcher.DispatchDiscoverNewNodes(new List<string> { context.TargetNodeId },
                    new GameEventDispatcher.NodeDiscoverContext(GameEventDispatcher.NodeDiscoverContext.e_DiscoverNewNodeMethod.Template));
                
                // 2. 通知 UI 层更新节点状态
                UIEventDispatcher.DispatchDiscoveredNewNodes(new List<NodeData> { LevelGraphContext.CurrentGraph.nodeLookup[context.TargetNodeId].Node });
                
                // 3. 通知 UI 层结算结果（播放成功特效等）
                UIEventDispatcher.DispatchTemplateAnswerResult(context);
            }
            else
            {
                Debug.Log("[Coordinator] 编排：模板失败表现。通知 UI 错误反馈。");
                UIEventDispatcher.DispatchTemplateAnswerResult(context);
            }
        }

        /// <summary>
        /// 处理逻辑侧分发的发现新模板通知
        /// </summary>
        private void HandleDiscoveredTemplates(List<string> templateIds)
        {
            var templates = new List<TemplateData>();
            var graph = LevelGraphContext.CurrentGraph;
            foreach (var id in templateIds)
            {
                if (graph.allTemplates.TryGetValue(id, out var template))
                {
                    templates.Add(template);
                }
                else
                {
                    Debug.LogError($"[Game_UI_Coordinator] Graph 中找不到 ID 为 {id} 的模板定义。");
                }
            }
            // 转发给 UI 侧
            UIEventDispatcher.DispatchDiscoveredNewTemplates(templates);
        }
        #endregion

        #region 对话与演出
        /// <summary>
        /// 处理后端生成的对话文本并推送到对话系统前端
        /// </summary>
        private void HandleDialogueGenerated(List<string> dialogues)
        {
            Debug.Log($"[Coordinator] 收到 {dialogues.Count} 行对话，转发给对话管理器。");

            if (dialogueManager != null)
            {
                dialogueManager.PushNewBatch(dialogues);
            }
            else
            {
                Debug.LogError("[Coordinator] 未绑定 DialogueRuntimeManager！");
            }
        }
        #endregion

        #region 阶段管理 (Phases)
        /// <summary>
        /// 处理逻辑侧发起的阶段解锁事件
        /// </summary>
        private void HandlePhaseUnlock(string completedName, List<(string id, string name)> nextPhases)
        {
            // 转发给 UI 总线，弹出阶段选择界面
            UIEventDispatcher.DispatchShowPhaseSelection(completedName, nextPhases);
        }
        #endregion

        #region AI 逻辑与发现
        /// <summary>
        /// 核心 AI 响应处理逻辑：根据裁判和发现模型的结果，分发到各个逻辑子系统
        /// </summary>
        private void HandleAIResponse(AIResponseData responseData)
        {
            if (responseData == null) return;

            // 1. 处理裁判模型 (Referee) 的结果
            var result = responseData.RefereeResult;
            if (result != null)
            {
                // 解锁通过的逻辑节点
                if (result.PassedNodeIds != null && result.PassedNodeIds.Count > 0)
                {
                    GameEventDispatcher.DispatchDiscoverNewNodes(result.PassedNodeIds, 
                        new GameEventDispatcher.NodeDiscoverContext(GameEventDispatcher.NodeDiscoverContext.e_DiscoverNewNodeMethod.PlayerInput));
                }
                // 解锁发现的实体
                if (result.EntityList != null && result.EntityList.Count > 0)
                {
                    GameEventDispatcher.DispatchDiscoveredNewEntityItems(result.EntityList);
                }
            }

            // 2. 处理发现模型 (Discovery) 的结果：通常转化为模板解锁
            if (responseData.DiscoveryResult != null && responseData.DiscoveryResult.DiscoveredNodeIds.Count > 0)
            {
                var ids = responseData.DiscoveryResult.DiscoveredNodeIds;
                LevelGraphData levelGraph = LevelGraphContext.CurrentGraph;

                if (levelGraph != null)
                {
                    List<string> templatesToUnlock = new List<string>();
                    foreach (var nodeId in ids)
                    {
                        if (levelGraph.nodeLookup.TryGetValue(nodeId, out var nodeInfo) && nodeInfo.Node != null)
                        {
                            string specialTmplId = nodeInfo.Node.Template?.SpecialTemplateId;
                            if (!string.IsNullOrEmpty(specialTmplId)) 
                                templatesToUnlock.Add(specialTmplId);
                            else if (nodeInfo.Node.Template?.Template != null) 
                                templatesToUnlock.Add($"nodeTemplate_{nodeId}");
                        }
                    }
                    if (templatesToUnlock.Count > 0)
                    {
                        GameEventDispatcher.DispatchDiscoveredNewTemplates(templatesToUnlock);
                    }
                }
            }
        }
        #endregion
    }
}
