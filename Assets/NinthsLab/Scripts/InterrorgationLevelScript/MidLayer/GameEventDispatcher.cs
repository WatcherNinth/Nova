using System;
using System.Collections.Generic;
using LogicEngine.LevelLogic;
using UnityEngine;

namespace Interrorgation.MidLayer
{
    public static class GameEventDispatcher
    {
        /// <summary>
        /// GameEventDispatcher: 玩家输入内容
        /// </summary>
        public static event Action<string> OnPlayerInputString;

        public static void DispatchPlayerInputString(string input)
        {
            OnPlayerInputString?.Invoke(input);
        }

        public class NodeDiscoverContext
        {
            public enum e_DiscoverNewNodeMethod
            {
                PlayerInput,
                Template,
            }
            public e_DiscoverNewNodeMethod Method;
            public string TemplateId;
            public NodeDiscoverContext(e_DiscoverNewNodeMethod method, string templateId = null)
            {
                Method = method;
                TemplateId = templateId;
            }
        }

        public static event Action<List<string>, NodeDiscoverContext> OnDiscoverNewNodes;
        public static void DispatchDiscoverNewNodes(List<string> nodeIds, NodeDiscoverContext context)
        {
            OnDiscoverNewNodes?.Invoke(nodeIds, context);
        }

        public static event Action<List<string>> OnDiscoveredNewEntity;

        public static void DispatchDiscoveredNewEntityItems(List<string> entityItemIds)
        {
            OnDiscoveredNewEntity?.Invoke(entityItemIds);
        }

        public static event Action<List<string>> OnDiscoveredNewTemplates;
        public static void DispatchDiscoveredNewTemplates(List<string> templateIds)
        {
            OnDiscoveredNewTemplates?.Invoke(templateIds);
        }

        // [修改] UI -> Logic: 提交节点
        public static event Action<string> OnNodeOptionSubmitted;
        public static void DispatchNodeOptionSubmitted(string id)
        {
            OnNodeOptionSubmitted?.Invoke(id);
        }

        #region 模板
        public static event Action<string, List<string>> OnPlayerSubmitTemplateAnswer;
        public static void DispatchPlayerSubmitTemplateAnswer(string templateId, List<string> answers)
        {
            Debug.Log($"[GameEventDispatcher] Dispatching PlayerSubmitTemplateAnswer: TemplateID={templateId}, Answers=[{string.Join(", ", answers)}]");
            OnPlayerSubmitTemplateAnswer?.Invoke(templateId, answers);
        }

        public class TemplateSettlementContext
        {
            public bool IsSuccess;
            public string TemplateId;
            public string TargetNodeId; // 如果成功，对应的节点ID
            public List<string> ErrorMessages; // 如果失败，反馈给玩家的错误（可选）
        }

        public static event Action<TemplateSettlementContext> OnTemplateSettlement;

        public static void DispatchTemplateSettlement(TemplateSettlementContext context)
        {
            OnTemplateSettlement?.Invoke(context);
        }

        #endregion

        // [新增] Logic -> UI: 节点状态变更 (包含 Submitted 或 Invalidated 标记变更)
        /// <summary>
        /// 当节点状态发生变更时（例如被证明、被提交、被失效等）。注意必须验证IsInvalidated字段
        /// </summary>
        public static event Action<RuntimeNodeData> OnNodeStatusChanged;
        public static void DispatchNodeStatusChanged(RuntimeNodeData nodeData)
        {
            OnNodeStatusChanged?.Invoke(nodeData);
        }

        /// <summary>
        /// 当实体状态发生变更时（例如从 Hidden 变为 Discovered）
        /// </summary>
        public static event Action<RuntimeEntityItemData> OnEntityStatusChanged;
        public static void DispatchEntityStatusChanged(RuntimeEntityItemData entityData)
        {
            OnEntityStatusChanged?.Invoke(entityData);
        }

        /// <summary>
        /// 当模板状态发生变更时（例如从 Hidden 变为 Discovered 或 Used）
        /// </summary>
        public static event Action<RuntimeTemplateData> OnTemplateStatusChanged;
        public static void DispatchTemplateStatusChanged(RuntimeTemplateData templateData)
        {
            OnTemplateStatusChanged?.Invoke(templateData);
        }

        // [新增] Logic -> UI: 阶段状态变更
        public static event Action<string, RuntimePhaseStatus> OnPhaseStatusChanged;
        public static void DispatchPhaseStatusChanged(string phaseId, RuntimePhaseStatus status)
        {
            OnPhaseStatusChanged?.Invoke(phaseId, status);
        }
        // [新增] Logic -> UI: 产生对话/剧情文本
        // List<string> 是文本列表
        // 注：进入dialoguesystem时，将从List<string>中一行一行提取文本
        public static event Action<List<string>> OnDialogueGenerated;
        public static void DispatchDialogueGenerated(List<string> dialogues)
        {
            OnDialogueGenerated?.Invoke(dialogues);
        }

        // [新增] Logic -> UI: 解锁了可跳转的新阶段 (强制选择/并行选择)
        // 参数: 刚刚完成的阶段名, 可跳转的阶段列表(ID, Name)
        public static event Action<string, List<(string id, string name)>> OnPhaseUnlockEvents;
        public static void DispatchPhaseUnlockEvents(string completedPhaseName, List<(string id, string name)> nextPhases)
        {
            OnPhaseUnlockEvents?.Invoke(completedPhaseName, nextPhases);
        }

        public static event Action<string> OnPlayerRequestPhaseSwitch;
        public static void DispatchPlayerRequestPhaseSwitch(string phaseId)
        {
            OnPlayerRequestPhaseSwitch?.Invoke(phaseId);
        }

        // [新增] Logic -> UI: 发送当前可用的切换列表 (用于 UI 显示侧边栏按钮或弹窗)
        // 这通常在状态变更时自动触发，或者 UI 主动查询
        public static event Action<List<(string id, string name, string status)>> OnAvailablePhasesChanged;
        public static void DispatchAvailablePhasesChanged(List<(string, string, string)> phases)
        {
            OnAvailablePhasesChanged?.Invoke(phases);
        }

        public static event Action<List<string>> OnScopeStackChanged;
        public static void DispatchScopeStackChanged(List<string> scopeStack)
        {
            OnScopeStackChanged?.Invoke(scopeStack);
        }

        #region Runtime Status Query (Node, Entity, Template)

        /// <summary>
        /// [Query] 获取指定节点的当前 Runtime 状态
        /// </summary>
        public static event Func<string, RuntimeNodeData> OnGetNodeStatus;
        public static RuntimeNodeData GetNodeStatus(string nodeId) => OnGetNodeStatus?.Invoke(nodeId);

        /// <summary>
        /// [Query] 获取指定实体的当前 Runtime 状态
        /// </summary>
        public static event Func<string, RuntimeEntityItemData> OnGetEntityStatus;
        public static RuntimeEntityItemData GetEntityStatus(string entityId) => OnGetEntityStatus?.Invoke(entityId);

        /// <summary>
        /// [Query] 获取指定模板的当前 Runtime 状态
        /// </summary>
        public static event Func<string, RuntimeTemplateData> OnGetTemplateStatus;
        public static RuntimeTemplateData GetTemplateStatus(string templateId) => OnGetTemplateStatus?.Invoke(templateId);

        #endregion
    }
}