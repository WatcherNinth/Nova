using System.Collections.Generic;
using AIEngine.Network;
using DialogueSystem;
using Interrorgation.MidLayer;
using Interrorgation.UI.UISequence;
using LogicEngine;
using LogicEngine.LevelGraph;
using UnityEngine;

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
                    _instance = FindFirstObjectByType<Game_UI_Coordinator>();
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
            GameEventDispatcher.OnNodeStatusChanged += HandleNodeStatusChanged;
            GameEventDispatcher.OnEntityStatusChanged += HandleEntityStatusChanged;
            GameEventDispatcher.OnTemplateStatusChanged += HandleTemplateStatusChanged;
            GameEventDispatcher.OnScopeStackChanged += HandleScopeStackChanged;

            GameEventDispatcher.OnTemplateSettlement += HandleTemplateSettlement;
            GameEventDispatcher.OnDialogueGenerated += HandleDialogueGenerated;
            GameEventDispatcher.OnPhaseUnlockEvents += HandlePhaseUnlock;
            GameEventDispatcher.OnDiscoveredNewTemplates += HandleDiscoveredTemplates;
            GameEventDispatcher.OnDiscoveredNewEntity += HandleDiscoveredEntities;
            GameEventDispatcher.OnDiscoverNewNodes += HandleDiscoveredNodes;

            // 订阅 AI 响应事件
            AIEventDispatcher.OnResponseReceived += HandleAIResponse;

            // 订阅关卡初始化事件
            GameEventDispatcher.OnLogicInitialized += HandleLogicInitialized;
        }

        void OnDisable()
        {
            UIEventDispatcher.OnPlayerSubmitInput -= HandlePlayerSubmitInput;
            UIEventDispatcher.OnPlayerSubmitTemplateAnswer -= HandlePlayerSubmitTemplateAnswer;
            UIEventDispatcher.OnNodeOptionSubmitted -= HandleNodeOptionSubmitted;

            GameEventDispatcher.OnNodeStatusChanged -= HandleNodeStatusChanged;
            GameEventDispatcher.OnEntityStatusChanged -= HandleEntityStatusChanged;
            GameEventDispatcher.OnTemplateStatusChanged -= HandleTemplateStatusChanged;
            GameEventDispatcher.OnScopeStackChanged -= HandleScopeStackChanged;
            GameEventDispatcher.OnTemplateSettlement -= HandleTemplateSettlement;
            GameEventDispatcher.OnDialogueGenerated -= HandleDialogueGenerated;
            GameEventDispatcher.OnPhaseUnlockEvents -= HandlePhaseUnlock;
            GameEventDispatcher.OnDiscoveredNewTemplates -= HandleDiscoveredTemplates;
            GameEventDispatcher.OnDiscoveredNewEntity -= HandleDiscoveredEntities;
            GameEventDispatcher.OnDiscoverNewNodes -= HandleDiscoveredNodes;

            AIEventDispatcher.OnResponseReceived -= HandleAIResponse;

            // 取消订阅关卡初始化事件
            GameEventDispatcher.OnLogicInitialized -= HandleLogicInitialized;
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
                Debug.Log("[Coordinator] 编排：模板成功序列。正在解锁目标节点。");
                // 1. 通知逻辑层解锁节点 (Logic -> Logic Event)
                // 注意：逻辑解锁会通过 OnDiscoverNewNodes 进一步触发 UI 序列
                GameEventDispatcher.DispatchDiscoverNewNodes(new List<string> { context.TargetNodeId },
                    new GameEventDispatcher.NodeDiscoverContext(GameEventDispatcher.NodeDiscoverContext.e_DiscoverNewNodeMethod.Template));

                // 2. 将结算结果反馈入队
                UISequenceManager.Instance.Enqueue(new UINotifyCommand("TemplateResult", (id) =>
                {
                    UIEventDispatcher.DispatchTemplateAnswerResult(context, id);
                }, isBlocking: false, dedupDescription: $"TemplateId: {context.TemplateId} Result: Success"));
            }
            else
            {
                Debug.Log("[Coordinator] 编排：模板失败表现。通知 UI 错误反馈。");
                UISequenceManager.Instance.Enqueue(new UINotifyCommand("TemplateResult", (id) =>
                {
                    UIEventDispatcher.DispatchTemplateAnswerResult(context, id);
                }, isBlocking: false, dedupDescription: $"TemplateId: {context.TemplateId} Result: Fail"));
            }
        }

        /// <summary>
        /// 处理逻辑侧分发的发现新节点通知并翻译给 UI
        /// </summary>
        private void HandleDiscoveredNodes(List<string> nodeIds, GameEventDispatcher.NodeDiscoverContext context)
        {
            var graph = LevelGraphContext.CurrentGraph;
            foreach (var id in nodeIds)
            {
                if (graph.nodeLookup.TryGetValue(id, out var nodeInfo))
                {
                    UISequenceManager.Instance.Enqueue(new UINotifyCommand("DiscNode", (actionId) =>
                    {
                        UIEventDispatcher.DispatchDiscoveredNewNode(nodeInfo.Node, actionId);
                    }, isBlocking: false, dedupDescription: id));
                }
            }
        }

        /// <summary>
        /// 处理逻辑侧分发的发现新模板通知并翻译给 UI
        /// </summary>
        private void HandleDiscoveredTemplates(List<string> templateIds)
        {
            var graph = LevelGraphContext.CurrentGraph;
            foreach (var id in templateIds)
            {
                if (graph.allTemplates.TryGetValue(id, out var template))
                {
                    UISequenceManager.Instance.Enqueue(new UINotifyCommand("DiscTemplate", (actionId) =>
                    {
                        UIEventDispatcher.DispatchDiscoveredNewTemplate(template, actionId);
                    }, isBlocking: false, dedupDescription: id));
                }
                else
                {
                    Debug.LogError($"[Game_UI_Coordinator] Graph 中找不到 ID 为 {id} 的模板定义。");
                }
            }
        }

        /// <summary>
        /// 处理逻辑侧分发的发现新实体通知
        /// </summary>
        private void HandleDiscoveredEntities(List<string> entityIds)
        {
            var graph = LevelGraphContext.CurrentGraph;
            foreach (var id in entityIds)
            {
                if (graph.entityListData.Data.TryGetValue(id, out var entity))
                {
                    UISequenceManager.Instance.Enqueue(new UINotifyCommand("DiscEntity", (actionId) =>
                    {
                        UIEventDispatcher.DispatchDiscoveredNewEntity(entity, actionId);
                    }, isBlocking: false, dedupDescription: id));
                }
            }
        }
        #endregion

        #region 对话与演出
        /// <summary>

        /// 处理后端生成的对话文本并推送到对话系统前端
        /// </summary>
        private void HandleDialogueGenerated(List<string> dialogues)
        {
            Debug.Log($"[Coordinator] 收到 {dialogues.Count} 行对话，入队演出序列。");

            if (dialogueManager != null)
            {
                UISequenceManager.Instance.Enqueue(new UIDialogueCommand("DialogueBatch", () =>
                {
                    dialogueManager.PushNewBatch(dialogues);
                    UIEventDispatcher.DispatchShowDialogues(dialogues);
                }, dedupDescription: string.Join("\n", dialogues)));
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

        #region AI 逻辑处理 (AI Logical Bridge)
        /* 
         * 注意：根据架构优化原则，AI 响应的"解析逻辑"本应放在专门的 AILogicManager 中。
         * 目前由 Coordinator 暂时代管，负责将 AI 原始响应 (AIResponseData) 
         * 转换为逻辑层变更 (GameEvent) 以及 UI 层表现 (UIEvent)。
         */

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
                // 仅触发逻辑事件，UI 转发将由 HandleDiscoveredNodes / HandleDiscoveredEntities 自动完成
                if (result.PassedNodeIds != null && result.PassedNodeIds.Count > 0)
                {
                    GameEventDispatcher.DispatchDiscoverNewNodes(result.PassedNodeIds,
                        new GameEventDispatcher.NodeDiscoverContext(GameEventDispatcher.NodeDiscoverContext.e_DiscoverNewNodeMethod.PlayerInput));
                }

                if (result.EntityList != null && result.EntityList.Count > 0)
                {
                    GameEventDispatcher.DispatchDiscoveredNewEntityItems(result.EntityList);
                }
            }

            // 处理发现模型(Discovery) 的结果：通常转化为模板解锁
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
        #region 其他状态变更 Handle
        private void HandleNodeStatusChanged(LogicEngine.LevelLogic.RuntimeNodeData data)
        {
            UISequenceManager.Instance.Enqueue(new UINotifyCommand("NodeStatus", (id) =>
            {
                UIEventDispatcher.DispatchNodeStatusChanged(data, id);
            }, isBlocking: false, dedupDescription: data.Id));
        }

        private void HandleEntityStatusChanged(LogicEngine.LevelLogic.RuntimeEntityItemData data)
        {
            UISequenceManager.Instance.Enqueue(new UINotifyCommand("EntityStatus", (id) =>
            {
                UIEventDispatcher.DispatchEntityStatusChanged(data, id);
            }, isBlocking: false, dedupDescription: data.Id));
        }

        private void HandleTemplateStatusChanged(LogicEngine.LevelLogic.RuntimeTemplateData data)
        {
            UISequenceManager.Instance.Enqueue(new UINotifyCommand("TemplateStatus", (id) =>
            {
                UIEventDispatcher.DispatchTemplateStatusChanged(data, id);
            }, isBlocking: false, dedupDescription: data.Id));
        }

        private void HandleScopeStackChanged(List<string> stack)
        {
            UISequenceManager.Instance.Enqueue(new UINotifyCommand("ScopeChanged", (id) =>
            {
                UIEventDispatcher.DispatchScopeStackChanged(stack, id);
            }, isBlocking: false, dedupDescription: string.Join(",", stack)));
        }
        #endregion
        #endregion

        #region 关卡初始化事件处理
        /// <summary>
        /// 逻辑层初始化完成后，通知 UI 层关卡就绪
        /// </summary>
        private void HandleLogicInitialized()
        {
            UIEventDispatcher.DispatchLevelReady();
        }
        #endregion
    }
}