using Interrorgation.MidLayer;
using LogicEngine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Interrorgation.UI
{
    public class MindMapNodeOptionUIScript : MonoBehaviour
    {
        [SerializeField] private TMP_Text _optionText;
        public string nodeId;

        private NodeData _nodeData;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
        public void Init(string nodeId)
        {
            this.nodeId = nodeId;
            _nodeData = LevelGraphContext.CurrentGraph.nodeLookup[nodeId].Node;
            GetComponent<Button>().onClick.AddListener(OnOptionClicked);
        }

        public void OnOptionClicked()
        {
            // 当选项被点击时，向外部发送事件，告知玩家选择了哪个节点
            UIEventDispatcher.DispatchNodeOptionSubmitted(nodeId);
        }

        public void HideNodeOption()
        {
            _optionText.text = "???";
            GetComponent<Button>().interactable = false;
        }

        public void DiscoverNodeOption()
        {
            _optionText.text = _nodeData.Basic.Description;
            GetComponent<Button>().interactable = true;
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent.GetComponent<RectTransform>());
        }
        
        public void SubmitNodeOption()
        {
            _optionText.text = _nodeData.Basic.Description;
            GetComponent<Button>().interactable = false;
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent.GetComponent<RectTransform>());
        }
    }
}