using System;
using System.Collections.Generic;
using LogicEngine.LevelLogic;

namespace Interrorgation.MidLayer
{
    public static class UIEventDispatcher
    {        
        public static event Action<string> OnPlayerSubmitInput;
        public static void DispatchPlayerSubmitInput(string input)
        {
            OnPlayerSubmitInput?.Invoke(input);
        }

        public static event Action<List<RuntimeNodeData>> OnDiscoveredNewNodes;

        public static void DispatchDiscoveredNewNodes(List<RuntimeNodeData> nodes)
        {
            OnDiscoveredNewNodes?.Invoke(nodes);
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

        // ========================================================
        // [新增] 用于通知 UI 层显示对话列表
        // ========================================================
        public static event Action<List<string>> OnShowDialogues;

        public static void DispatchShowDialogues(List<string> dialogues)
        {
            OnShowDialogues?.Invoke(dialogues);
        }
        
        // ========================================================
        // [新增] 用于通知 UI 层显示阶段选择弹窗 (之前 HandlePhaseUnlock 也没地方传)
        // ========================================================
        public static event Action<string, List<(string id, string name)>> OnShowPhaseSelection;

        public static void DispatchShowPhaseSelection(string completedPhase, List<(string id, string name)> nextPhases)
        {
            OnShowPhaseSelection?.Invoke(completedPhase, nextPhases);
        }
    }
}