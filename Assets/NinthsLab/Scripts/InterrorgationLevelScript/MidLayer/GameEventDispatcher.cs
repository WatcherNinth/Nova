using System;
using System.Collections.Generic;
using LogicEngine.LevelLogic;
using UnityEngine;

namespace Interrorgation.MidLayer
{
    public static class GameEventDispatcher
    {
        #region 玩家交互 (UI -> Logic)
        /// <summary>
        /// 玩家输入原始字符串内容
        /// </summary>
        public static event Action<string> OnPlayerInputString;
        public static void DispatchPlayerInputString(string input) => OnPlayerInputString?.Invoke(input);

        /// <summary>
        /// 玩家提交节点选项
        /// </summary>
        public static event Action<string> OnNodeOptionSubmitted;
        public static void DispatchNodeOptionSubmitted(string id) => OnNodeOptionSubmitted?.Invoke(id);

        /// <summary>
        /// 玩家提交模板答案
        /// </summary>
        public static event Action<string, List<string>> OnPlayerSubmitTemplateAnswer;
        public static void DispatchPlayerSubmitTemplateAnswer(string templateId, List<string> answers)
        {
            Debug.Log($"[GameEventDispatcher] Dispatching PlayerSubmitTemplateAnswer: TemplateID={templateId}, Answers=[{string.Join(", ", answers)}]");
            OnPlayerSubmitTemplateAnswer?.Invoke(templateId, answers);
        }

        /// <summary>
        /// 玩家请求切换阶段
        /// </summary>
        public static event Action<string> OnPlayerRequestPhaseSwitch;
        public static void DispatchPlayerRequestPhaseSwitch(string phaseId) => OnPlayerRequestPhaseSwitch?.Invoke(phaseId);
        #endregion

        #region 状态变更 (Logic -> UI Status)
        /// <summary>
        /// 节点状态变更 (包含 Submitted 或 Invalidated 标记变更)
        /// </summary>
        public static event Action<RuntimeNodeData> OnNodeStatusChanged;
        public static void DispatchNodeStatusChanged(RuntimeNodeData nodeData) => OnNodeStatusChanged?.Invoke(nodeData);

        /// <summary>
        /// 当实体状态发生变更时（例如从 Hidden 变为 Discovered）
        /// </summary>
        public static event Action<RuntimeEntityItemData> OnEntityStatusChanged;
        public static void DispatchEntityStatusChanged(RuntimeEntityItemData entityData) => OnEntityStatusChanged?.Invoke(entityData);

        /// <summary>
        /// 当模板状态发生变更时（例如从 Hidden 变为 Discovered 或 Used）
        /// </summary>
        public static event Action<RuntimeTemplateData> OnTemplateStatusChanged;
        public static void DispatchTemplateStatusChanged(RuntimeTemplateData templateData) => OnTemplateStatusChanged?.Invoke(templateData);

        /// <summary>
        /// 阶段状态变更
        /// </summary>
        public static event Action<string, RuntimePhaseStatus> OnPhaseStatusChanged;
        public static void DispatchPhaseStatusChanged(string phaseId, RuntimePhaseStatus status) => OnPhaseStatusChanged?.Invoke(phaseId, status);
        #endregion

        #region 逻辑发现 (Discovery)
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

        /// <summary>
        /// 发现了新节点
        /// </summary>
        public static event Action<List<string>, NodeDiscoverContext> OnDiscoverNewNodes;
        public static void DispatchDiscoverNewNodes(List<string> nodeIds, NodeDiscoverContext context) => OnDiscoverNewNodes?.Invoke(nodeIds, context);

        /// <summary>
        /// 发现了新实体(使用时必须跟gameevent那边的同名一起分发，保持同步)
        /// </summary>
        public static event Action<List<string>> OnDiscoveredNewEntity;
        public static void DispatchDiscoveredNewEntityItems(List<string> entityItemIds) => OnDiscoveredNewEntity?.Invoke(entityItemIds);

        /// <summary>
        /// 发现了新模板
        /// </summary>
        public static event Action<List<string>> OnDiscoveredNewTemplates;
        public static void DispatchDiscoveredNewTemplates(List<string> templateIds) => OnDiscoveredNewTemplates?.Invoke(templateIds);
        #endregion

        #region 模板系统 (Templates)
        public class TemplateSettlementContext
        {
            public bool IsSuccess;
            public string TemplateId;
            public string TargetNodeId; // 如果成功，对应的节点ID
            public List<string> ErrorMessages; // 如果失败，反馈给玩家的错误（可选）
        }

        /// <summary>
        /// 模板结算结果通知
        /// </summary>
        public static event Action<TemplateSettlementContext> OnTemplateSettlement;
        public static void DispatchTemplateSettlement(TemplateSettlementContext context) => OnTemplateSettlement?.Invoke(context);
        #endregion

        #region 阶段管理 (Phases)
        /// <summary>
        /// 解锁了可跳转的新阶段 (强制选择/并行选择)
        /// </summary>
        public static event Action<string, List<(string id, string name)>> OnPhaseUnlockEvents;
        public static void DispatchPhaseUnlockEvents(string completedPhaseName, List<(string id, string name)> nextPhases) => OnPhaseUnlockEvents?.Invoke(completedPhaseName, nextPhases);

        /// <summary>
        /// 发送当前可用的切换列表 (用于 UI 显示侧边栏按钮或弹窗)
        /// </summary>
        public static event Action<List<(string id, string name, string status)>> OnAvailablePhasesChanged;
        public static void DispatchAvailablePhasesChanged(List<(string, string, string)> phases) => OnAvailablePhasesChanged?.Invoke(phases);
        #endregion

        #region 对话与演出
        /// <summary>
        /// 产生对话/剧情文本
        /// </summary>
        public static event Action<List<string>> OnDialogueGenerated;
        public static void DispatchDialogueGenerated(List<string> dialogues) => OnDialogueGenerated?.Invoke(dialogues);

        /// <summary>
        /// 调查范围堆栈变更
        /// </summary>
        public static event Action<List<string>> OnScopeStackChanged;
        public static void DispatchScopeStackChanged(List<string> scopeStack) => OnScopeStackChanged?.Invoke(scopeStack);
        #endregion

        #region 状态查询 (Runtime Status Query)
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

        /// <summary>
        /// [Query] 获取所有节点的当前 Runtime 状态全表
        /// </summary>
        public static event Func<Dictionary<string, RuntimeNodeData>> OnGetAllNodeStatus;
        public static Dictionary<string, RuntimeNodeData> GetAllNodeStatus() => OnGetAllNodeStatus?.Invoke();

        /// <summary>
        /// [Query] 获取所有实体的当前 Runtime 状态全表
        /// </summary>
        public static event Func<Dictionary<string, RuntimeEntityItemData>> OnGetAllEntityStatus;
        public static Dictionary<string, RuntimeEntityItemData> GetAllEntityStatus() => OnGetAllEntityStatus?.Invoke();

        /// <summary>
        /// [Query] 获取所有模板的当前 Runtime 状态全表
        /// </summary>
        public static event Func<Dictionary<string, RuntimeTemplateData>> OnGetAllTemplateStatus;
        public static Dictionary<string, RuntimeTemplateData> GetAllTemplateStatus() => OnGetAllTemplateStatus?.Invoke();

        /// <summary>
        /// [Query] 获取当前Scope数据
        /// </summary>
        public static event Func<List<string>> OnGetCurrentScopeStack;
        public static List<string> GetCurrentScopeStack() => OnGetCurrentScopeStack?.Invoke();
        #endregion
    }
}
