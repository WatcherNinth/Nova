using System;
using System.Collections.Generic;
using LogicEngine;
using LogicEngine.LevelLogic;
using UnityEngine;

namespace Interrorgation.MidLayer
{
    /// <summary>
    /// UI 事件派发器，用于逻辑层向 UI 层发送通知
    /// </summary>
    public static class UIEventDispatcher
    {        
        #region 玩家交互
        /// <summary>
        /// 玩家提交原始输入文本
        /// </summary>
        public static event Action<string> OnPlayerSubmitInput;
        public static void DispatchPlayerSubmitInput(string input)
        {
            OnPlayerSubmitInput?.Invoke(input);
        }
        #endregion

        #region 状态变更 (Runtime Status)
        /// <summary>
        /// 节点状态变更
        /// </summary>
        public static event Action<RuntimeNodeData> OnNodeStatusChanged;
        public static void DispatchNodeStatusChanged(RuntimeNodeData nodeData)
        {
            OnNodeStatusChanged?.Invoke(nodeData);
        }

        /// <summary>
        /// 实体状态变更
        /// </summary>
        public static event Action<RuntimeEntityItemData> OnEntityStatusChanged;
        public static void DispatchEntityStatusChanged(RuntimeEntityItemData entityData)
        {
            OnEntityStatusChanged?.Invoke(entityData);
        }

        /// <summary>
        /// 模板状态变更
        /// </summary>
        public static event Action<RuntimeTemplateData> OnTemplateStatusChanged;
        public static void DispatchTemplateStatusChanged(RuntimeTemplateData templateData)
        {
            OnTemplateStatusChanged?.Invoke(templateData);
        }
        #endregion

        #region 节点逻辑 (Nodes)
        /// <summary>
        /// 发现并解锁了新节点
        /// </summary>
        public static event Action<List<NodeData>> OnDiscoveredNewNodes;
        public static void DispatchDiscoveredNewNodes(List<NodeData> nodes)
        {
            OnDiscoveredNewNodes?.Invoke(nodes);
        }

        /// <summary>
        /// 玩家点击并提交了某个节点选项
        /// </summary>
        public static event Action<string> OnNodeOptionSubmitted;
        public static void DispatchNodeOptionSubmitted(string nodeId)
        {
            OnNodeOptionSubmitted?.Invoke(nodeId);
        }
        #endregion

        #region 实体系统 (Entities)
        /// <summary>
        /// 发现并解锁了新实体
        /// </summary>
        public static event Action<List<EntityItem>> OnDiscoveredNewEntity;
        public static void DispatchDiscoveredNewEntityItems(List<EntityItem> entityItems)
        {
            OnDiscoveredNewEntity?.Invoke(entityItems);
        }
        #endregion

        #region 模板系统 (Templates)
        /// <summary>
        /// 发现并解锁了新模板
        /// </summary>
        public static event Action<List<TemplateData>> OnDiscoveredNewTemplates;
        public static void DispatchDiscoveredNewTemplates(List<TemplateData> templates)
        {
            OnDiscoveredNewTemplates?.Invoke(templates);
        }

        /// <summary>
        /// 玩家提交模板答案
        /// </summary>
        public static event Action<string, List<string>> OnPlayerSubmitTemplateAnswer;
        public static void DispatchPlayerSubmitTemplateAnswer(string templateId, List<string> answers)
        {
            Debug.Log($"[UIEventDispatcher] 派发模板答案提交事件: TemplateID={templateId}, Answers=[{string.Join(", ", answers)}]");
            OnPlayerSubmitTemplateAnswer?.Invoke(templateId, answers);
        }

        /// <summary>
        /// 模板答案校验结果反馈
        /// </summary>
        public static event Action<GameEventDispatcher.TemplateSettlementContext> OnTemplateAnswerResult;
        public static void DispatchTemplateAnswerResult(GameEventDispatcher.TemplateSettlementContext context)
        {
            OnTemplateAnswerResult?.Invoke(context);
        }
        #endregion

        #region 对话与演出
        /// <summary>
        /// 通知 UI 层显示对话列表
        /// </summary>
        public static event Action<List<string>> OnShowDialogues;
        public static void DispatchShowDialogues(List<string> dialogues)
        {
            OnShowDialogues?.Invoke(dialogues);
        }
        #endregion
        
        #region 阶段管理 (Phases)
        /// <summary>
        /// 通知 UI 层显示阶段选择弹窗（当一个阶段完成并解锁后续阶段时触发）
        /// </summary>
        public static event Action<string, List<(string id, string name)>> OnShowPhaseSelection;
        public static void DispatchShowPhaseSelection(string completedPhase, List<(string id, string name)> nextPhases)
        {
            OnShowPhaseSelection?.Invoke(completedPhase, nextPhases);
        }
        #endregion
    }
}
