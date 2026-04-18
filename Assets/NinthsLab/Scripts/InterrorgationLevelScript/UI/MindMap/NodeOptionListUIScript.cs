using System.Collections.Generic;
using Interrorgation.MidLayer;
using Interrorgation.UI.UIState;
using LogicEngine;
using UnityEngine;
using UnityEngine.UI;

namespace Interrorgation.UI
{
    public class NodeOptionListUIScript : UIStateController<NodeOptionListUIScript.OptionListState>
    {
        public enum OptionListState
        {
            Hidden,
            Shown
        }

        [SerializeField] private NodeOptionUIScript _optionPrefab;
        [SerializeField] private Transform _optionContainer;
        [SerializeField] private ScrollRect _scrollRect;

        protected override OptionListState InitialState => OptionListState.Hidden;

        protected override Dictionary<OptionListState, List<OptionListState>> DefineTransitions()
        {
            return new Dictionary<OptionListState, List<OptionListState>>
            {
                { OptionListState.Hidden, new List<OptionListState> { OptionListState.Shown } },
                { OptionListState.Shown,  new List<OptionListState> { OptionListState.Hidden } },
            };
        }

        protected override bool CheckIsVisualActive(OptionListState state) => state == OptionListState.Shown;

        protected override void SubscribeEvents()
        {
            UIEventDispatcher.OnDiscoveredNewNodes += HandleNewNodes;
            UIEventDispatcher.OnNodeStatusChanged += HandleNodeStatusChanged;
            UIEventDispatcher.OnShowDialogues += HandleShowDialogues;
            DialogueSystem.DialogueEventDispatcher.OnDialogueBatchEnded += HandleDialogueEnd;
        }

        protected override void UnsubscribeEvents()
        {
            UIEventDispatcher.OnDiscoveredNewNodes -= HandleNewNodes;
            UIEventDispatcher.OnNodeStatusChanged -= HandleNodeStatusChanged;
            UIEventDispatcher.OnShowDialogues -= HandleShowDialogues;
            DialogueSystem.DialogueEventDispatcher.OnDialogueBatchEnded -= HandleDialogueEnd;
        }

        public override void OnStateEnter(OptionListState state)
        {
            switch (state)
            {
                case OptionListState.Hidden:
                    _scrollRect.gameObject.SetActive(false);
                    break;
                case OptionListState.Shown:
                    _scrollRect.gameObject.SetActive(true);
                    break;
            }
        }

        private void HandleNewNodes(List<NodeData> newNodes, string actionId)
        {
            if (newNodes == null || newNodes.Count == 0) return;
            DispatchOrBacklog(() =>
            {
                foreach (var node in newNodes) addNodeOption(node.Id);
            }, actionId);
        }

        private void addNodeOption(string targetNodeId)
        {
            var option = Instantiate(_optionPrefab, _optionContainer);
            option.Init(targetNodeId);
            option.gameObject.SetActive(true);
        }

        private void HandleNodeStatusChanged(LogicEngine.LevelLogic.RuntimeNodeData nodeData, string actionId)
        {
            DispatchOrBacklog(() =>
            {
                foreach (Transform child in _optionContainer)
                {
                    var optionScript = child.GetComponent<NodeOptionUIScript>();
                    if (optionScript != null && optionScript.nodeId == nodeData.Id)
                    {
                        // 如果节点状态是已提交，则隐藏选项
                        if (nodeData.Status == LogicEngine.LevelLogic.RunTimeNodeStatus.Submitted)
                        {
                            child.gameObject.SetActive(false);
                            Destroy(child.gameObject, 0.1f); // 延迟销毁以避免潜在的 UI 交互问题
                            break;
                        }
                        if (nodeData.IsInvalidated)
                        {
                            child.GetComponent<Button>().interactable = false;
                        }
                    }
                }
            }, actionId);
        }

        private void refreshAllOptions()
        {
            var runtimeData = GameEventDispatcher.GetAllNodeStatus();
            // 刷新所有选项的状态（例如在节点状态变更时调用）
            foreach (Transform child in _optionContainer)
            {
                var optionScript = child.GetComponent<NodeOptionUIScript>();
                if (optionScript != null)
                {
                    runtimeData.TryGetValue(optionScript.nodeId, out var nodeData);
                    if (nodeData != null)
                    {
                        if (nodeData.Status == LogicEngine.LevelLogic.RunTimeNodeStatus.Submitted)
                        {
                            child.gameObject.SetActive(false);
                            Destroy(child.gameObject, 0.1f); // 延迟销毁以避免潜在的 UI 交互问题
                        }
                        else if (nodeData.IsInvalidated)
                        {
                            child.GetComponent<Button>().interactable = false;
                        }
                    }
                }
            }
        }

        private void HandleShowDialogues(List<string> dialogues)
        {
            StateMachine.TryTransitionTo(OptionListState.Hidden);
            refreshAllOptions();
        }

        private void HandleDialogueEnd()
        {
            StateMachine.TryTransitionTo(OptionListState.Shown);
        }
    }
}