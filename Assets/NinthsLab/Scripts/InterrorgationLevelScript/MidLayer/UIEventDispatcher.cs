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


    }
}