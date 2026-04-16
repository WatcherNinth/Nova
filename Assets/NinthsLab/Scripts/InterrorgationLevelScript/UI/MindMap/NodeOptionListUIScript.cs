using System.Collections;
using System.Collections.Generic;
using Interrorgation.MidLayer;
using LogicEngine;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;

namespace Interrorgation.UI
{
    public class NodeOptionListUIScript : MonoBehaviour, UISequence.IUIBacklogModule
    {
        [SerializeField] private NodeOptionUIScript _optionPrefab;
        [SerializeField] private Transform _optionContainer;
        [SerializeField] private ScrollRect _scrollRect;

        // [改进] 表现积压列表
        private List<System.Action> _backlog = new List<System.Action>();

        public bool IsVisualActive => _scrollRect.gameObject.activeSelf;

        private void OnEnable()
        {
            UIEventDispatcher.OnDiscoveredNewNodes += HandleNewNodes;
            UIEventDispatcher.OnNodeStatusChanged += HandleNodeStatusChanged;
            UIEventDispatcher.OnShowDialogues += HandleShowDialogues;
            DialogueSystem.DialogueEventDispatcher.OnDialogueBatchEnded += HandleDialogueEnd;
        }

        private void OnDisable()
        {
            UIEventDispatcher.OnDiscoveredNewNodes -= HandleNewNodes;
            UIEventDispatcher.OnNodeStatusChanged -= HandleNodeStatusChanged;
            UIEventDispatcher.OnShowDialogues -= HandleShowDialogues;
            DialogueSystem.DialogueEventDispatcher.OnDialogueBatchEnded -= HandleDialogueEnd;
        }
        // Implementation for the UI script managing node options in the mind map
        void Start()
        {

        }
        void Update()
        {

        }

        void HandleNewNodes(List<NodeData> newNodes, string actionId)
        {
            if (newNodes == null || newNodes.Count == 0) return;

            if (!IsVisualActive)
            {
                Debug.Log($"[MindMapUI] 视觉面板已关闭，拦截并记录积压事件 (ID: {actionId})");
                RecordBacklog(() => {
                    foreach (var node in newNodes) addNodeOption(node.Id);
                });
                // 缓存后立即上报完成
                UIEventDispatcher.DispatchActionCompleted(actionId);
                return;
            }

            // 活跃状态下，直接展示
            foreach (var node in newNodes)
            {
                addNodeOption(node.Id);
            }
            // 以后如果有动画在此处等待，则需要在动画结束后调用 DispatchActionCompleted
            UIEventDispatcher.DispatchActionCompleted(actionId);
        }

        void addNodeOption(string targetNodeId)
        {
            var option = Instantiate(_optionPrefab, _optionContainer);
            option.Init(targetNodeId);
            option.gameObject.SetActive(true);
        }

        void HandleNodeStatusChanged(LogicEngine.LevelLogic.RuntimeNodeData nodeData, string actionId)
        {
            if (!IsVisualActive)
            {
                RecordBacklog(() => HandleNodeStatusChanged(nodeData, ""));
                UIEventDispatcher.DispatchActionCompleted(actionId);
                return;
            }

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
            UIEventDispatcher.DispatchActionCompleted(actionId);
        }

        void refreshAllOptions()
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

        void HandleShowDialogues(List<string> dialogues)
        {
            // 当新的对话出现时，隐藏选项列表，等待对话结束后再显示
            _scrollRect.gameObject.SetActive(false);
            refreshAllOptions();
        }

        void HandleDialogueEnd()
        {
            _scrollRect.gameObject.SetActive(true);
            ProcessBacklog();
        }

        #region IUIBacklogModule 实现
        public void RecordBacklog(System.Action action)
        {
            _backlog.Add(action);
        }

        public void ProcessBacklog()
        {
            if (_backlog.Count == 0) return;

            Debug.Log($"[MindMapUI] 开始回放积压的表现动画 (Count: {_backlog.Count})");
            foreach (var action in _backlog)
            {
                action?.Invoke();
            }
            _backlog.Clear();
        }
        #endregion
    }
}