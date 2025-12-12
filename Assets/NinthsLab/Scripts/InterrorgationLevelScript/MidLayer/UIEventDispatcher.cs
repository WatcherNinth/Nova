using System;
using System.Collections.Generic;
using LogicEngine;
using LogicEngine.LevelGraph;

namespace Interrorgation.MidLayer
{
    public static class UIEventDispatcher
    {        
        public static event Action<string> OnPlayerSubmitInput;
        public static void DispatchPlayerSubmitInput(string input)
        {
            OnPlayerSubmitInput?.Invoke(input);
        }

        public static event Action<List<NodeData>> OnDiscoveredNewNodes;

        public static void DispatchDiscoveredNewNodes(List<NodeData> nodes)
        {
            OnDiscoveredNewNodes?.Invoke(nodes);
        }

        public static event Action<List<EntityItem>> OnDiscoveredNewEntity;

        public static void DispatchDiscoveredNewEntityItems(List<EntityItem> entityItems)
        {
            OnDiscoveredNewEntity?.Invoke(entityItems);
        }

        public static event Action<List<TemplateData>> OnDiscoveredNewTemplates;
        public static void DispatchDiscoveredNewTemplates(List<TemplateData> templates)
        {
            OnDiscoveredNewTemplates?.Invoke(templates);
        }
    }
}