using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Interrorgation.MidLayer;
using LogicEngine;

namespace FrontendEngine.MindMap
{
    public class NodeOptionListUIScript : MonoBehaviour
    {
        [SerializeField] private NodeOptionUIScript _optionPrefab;
        [SerializeField] private Transform _optionContainer;

        private void OnEnable()
        {
            UIEventDispatcher.OnDiscoveredNewNodes += HandleNewNodes;
        }

        private void OnDisable()
        {
            UIEventDispatcher.OnDiscoveredNewNodes -= HandleNewNodes;
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


    }

}