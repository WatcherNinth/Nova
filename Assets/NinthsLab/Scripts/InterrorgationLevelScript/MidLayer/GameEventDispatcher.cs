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
    }
}