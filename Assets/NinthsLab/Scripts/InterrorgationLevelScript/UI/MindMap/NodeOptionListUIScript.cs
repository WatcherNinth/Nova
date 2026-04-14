using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Interrorgation.MidLayer;
using LogicEngine;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UI;

namespace FrontendEngine.MindMap
{
    public class NodeOptionListUIScript : MonoBehaviour
    {
        [SerializeField] private NodeOptionUIScript _optionPrefab;
        [SerializeField] private Transform _optionContainer;
        [SerializeField] private ScrollRect _scrollRect;

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

        void HandleNewNodes(List<NodeData> newNodes)
        {
            // 添加新选项
            foreach (var node in newNodes)
            {
                addNodeOption(node.Id);
            }
        }

        void addNodeOption(string targetNodeId)
        {
            var option = Instantiate(_optionPrefab, _optionContainer);
            option.Init(targetNodeId);
            option.gameObject.SetActive(true);
        }

        void HandleNodeStatusChanged(LogicEngine.LevelLogic.RuntimeNodeData nodeData)
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
                        return;
                    }
                    if (nodeData.IsInvalidated)
                    {
                        child.GetComponent<Button>().interactable = false;
                    }
                }
            }
        }

        void HandleShowDialogues(List<string> dialogues)
        {
            // 当新的对话出现时，隐藏选项列表，等待对话结束后再显示
            _scrollRect.gameObject.SetActive(false);
        }

        void HandleDialogueEnd()
        {
            _scrollRect.gameObject.SetActive(true);
        }

    }

}