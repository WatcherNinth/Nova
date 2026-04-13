
using DG.Tweening.Plugins.Options;
using UnityEngine;
using System.Collections.Generic;
using DialogueSystem;
using AIEngine.Network;
using LogicEngine;
using LogicEngine.LevelGraph;

namespace Interrorgation.MidLayer
{
    public class Game_UI_Coordinator : MonoBehaviour
    {
        #region 单例
        // ==========================================
        // 稳健的单例模式 (适配 Edit Mode)
        // ==========================================
        private static Game_UI_Coordinator _instance;

        public static Game_UI_Coordinator Instance
        {
            get
            {
                // 如果为空，尝试在场景中寻找
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<Game_UI_Coordinator>();
                }
                return _instance;
            }
            private set
            {
                _instance = value;
            }
        }

        #endregion

        [Header("References")]
        [SerializeField] private DialogueRuntimeManager dialogueManager;

        void OnEnable()
        {
            UIEventDispatcher.OnPlayerSubmitInput += HandlePlayerSubmitInput;
            // [UI -> Game] 模板提交
            UIEventDispatcher.OnPlayerSubmitTemplateAnswer += HandlePlayerSubmitTemplateAnswer;

            GameEventDispatcher.OnTemplateSettlement += HandleTemplateSettlement;

            // [Dialogue] 后端输出对话并推到前端的事件
            GameEventDispatcher.OnDialogueGenerated += HandleDialogueGenerated;
            GameEventDispatcher.OnPhaseUnlockEvents += HandlePhaseUnlock;
            // [Game -> UI] 模板发现
            GameEventDispatcher.OnDiscoveredNewTemplates += HandleDiscoveredTemplates;

            // [新增] 监听 AI 响应
            AIEventDispatcher.OnResponseReceived += HandleAIResponse;

            UIEventDispatcher.OnNodeOptionSubmitted += HandleNodeOptionSubmitted;
        }

        void OnDisable()
        {
            UIEventDispatcher.OnPlayerSubmitInput -= HandlePlayerSubmitInput;
            UIEventDispatcher.OnPlayerSubmitTemplateAnswer -= HandlePlayerSubmitTemplateAnswer;
            GameEventDispatcher.OnTemplateSettlement -= HandleTemplateSettlement;
            GameEventDispatcher.OnDialogueGenerated -= HandleDialogueGenerated;
            GameEventDispatcher.OnPhaseUnlockEvents -= HandlePhaseUnlock;
            GameEventDispatcher.OnDiscoveredNewTemplates -= HandleDiscoveredTemplates;

            // [新增] 注销 AI 响应
            AIEventDispatcher.OnResponseReceived -= HandleAIResponse;

            UIEventDispatcher.OnNodeOptionSubmitted -= HandleNodeOptionSubmitted;
        }

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

        void HandlePlayerSubmitInput(string input)
        {
            GameEventDispatcher.DispatchPlayerInputString(input);
        }

        // 处理 UI 侧发起的模板提交
        void HandlePlayerSubmitTemplateAnswer(string templateId, List<string> answers)
        {
            // UI -> Game
            GameEventDispatcher.DispatchPlayerSubmitTemplateAnswer(templateId, answers);
        }

        void HandleNodeOptionSubmitted(string nodeId)
        {
            GameEventDispatcher.DispatchNodeOptionSubmitted(nodeId);
        }

        private void HandleTemplateSettlement(GameEventDispatcher.TemplateSettlementContext context)
        {
            if (context.IsSuccess)
            {
                Debug.Log("[Coordinator] 编排：模板成功序列");
                GameEventDispatcher.DispatchDiscoverNewNodes(new List<string> { context.TargetNodeId },
                    new GameEventDispatcher.NodeDiscoverContext(GameEventDispatcher.NodeDiscoverContext.e_DiscoverNewNodeMethod.Template));
                UIEventDispatcher.DispatchDiscoveredNewNodes(new List<NodeData> { LevelGraphContext.CurrentGraph.nodeLookup[context.TargetNodeId].Node });
                UIEventDispatcher.DispatchTemplateAnswerResult(context);
            }
            else
            {
                Debug.Log("[Coordinator] 编排：模板失败表现");
                UIEventDispatcher.DispatchTemplateAnswerResult(context);
            }
        }

        void HandleDialogueGenerated(List<string> dialogues)
        {
            Debug.Log($"[Coordinator] 收到 {dialogues.Count} 行对话，转发给 DialogueSystem。");

            // 【关键】将数据推入对话系统
            if (dialogueManager != null)
            {
                dialogueManager.PushNewBatch(dialogues);
            }
            else
            {
                Debug.LogError("Coordinator 未绑定 DialogueRuntimeManager！");
            }
        }

        // [修改] 处理阶段解锁
        void HandlePhaseUnlock(string completedName, List<(string id, string name)> nextPhases)
        {
            // 转发给 UI 总线
            UIEventDispatcher.DispatchShowPhaseSelection(completedName, nextPhases);
        }

        // 处理逻辑侧分发的发现新模板
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
            // Game -> UI
            UIEventDispatcher.DispatchDiscoveredNewTemplates(templates);
        }

        // --- AI Response Processing (Migrated from PlayerMindMapManager) ---

        private void HandleAIResponse(AIResponseData responseData)
        {
            if (responseData == null) return;

            var result = responseData.RefereeResult;
            if (result != null)
            {
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

            if (responseData.DiscoveryResult != null && responseData.DiscoveryResult.DiscoveredNodeIds.Count > 0)
            {
                var ids = responseData.DiscoveryResult.DiscoveredNodeIds;
                LevelGraphData levelGraph = LevelGraphContext.CurrentGraph;

                if (levelGraph != null)
                {
                    // 尝试作为模板发现
                    List<string> templatesToUnlock = new List<string>();
                    foreach (var nodeId in ids)
                    {
                        if (levelGraph.nodeLookup.TryGetValue(nodeId, out var nodeInfo) && nodeInfo.Node != null)
                        {
                            string specialTmplId = nodeInfo.Node.Template?.SpecialTemplateId;
                            if (!string.IsNullOrEmpty(specialTmplId)) templatesToUnlock.Add(specialTmplId);
                            else if (nodeInfo.Node.Template?.Template != null) templatesToUnlock.Add($"nodeTemplate_{nodeId}");
                        }
                    }
                    if (templatesToUnlock.Count > 0)
                    {
                        GameEventDispatcher.DispatchDiscoveredNewTemplates(templatesToUnlock);
                    }
                }
            }
        }
    }
}