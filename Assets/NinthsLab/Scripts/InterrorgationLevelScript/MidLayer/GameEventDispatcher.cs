using System;
using System.Collections.Generic;
using LogicEngine.LevelLogic;

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

        public static event Action<List<RuntimeNodeData>, NodeDiscoverContext> OnDiscoveredNewNodes;

        public static void DispatchDiscoveredNewNodes(List<RuntimeNodeData> nodes, NodeDiscoverContext context)
        {
            OnDiscoveredNewNodes?.Invoke(nodes, context);
        }

        public static event Action<List<RuntimeEntityItemData>> OnDiscoveredNewEntity;

        public static void DispatchDiscoveredNewEntityItems(List<RuntimeEntityItemData> entityItems)
        {
            OnDiscoveredNewEntity?.Invoke(entityItems);
        }

        public static event Action<List<RuntimeTemplateData>> OnDiscoveredNewTemplates;
        public static void DispatchDiscoveredNewTemplates(List<RuntimeTemplateData> templates)
        {
            OnDiscoveredNewTemplates?.Invoke(templates);
        }

        // [修改] UI -> Logic: 提交节点
        public static event Action<string> OnNodeOptionSubmitted;
        public static void DispatchNodeOptionSubmitted(string id)
        {
            OnNodeOptionSubmitted?.Invoke(id);
        }

        public static event Action<string, List<string>> OnPlayerSubmitTemplateAnswer;
        public static void DispatchPlayerSubmitTemplateAnswer(string templateId, List<string> answers)
        {
            OnPlayerSubmitTemplateAnswer?.Invoke(templateId, answers);
        }

        // [新增] Logic -> UI: 节点状态变更 (包含 Submitted 或 Invalidated 标记变更)
        public static event Action<RuntimeNodeData> OnNodeStatusChanged;
        public static void DispatchNodeStatusChanged(RuntimeNodeData nodeData)
        {
            OnNodeStatusChanged?.Invoke(nodeData);
        }

        // [新增] Logic -> UI: 阶段状态变更
        public static event Action<string, RuntimePhaseStatus> OnPhaseStatusChanged;
        public static void DispatchPhaseStatusChanged(string phaseId, RuntimePhaseStatus status)
        {
            OnPhaseStatusChanged?.Invoke(phaseId, status);
        }
        // [新增] Logic -> UI: 产生对话/剧情文本
        // List<string> 是文本列表
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
    }
}