using Interrorgation.MidLayer;
using LogicEngine;
using TMPro;
using UnityEngine;
namespace Interrorgation.UI
{
    public class MindMapNodeOptionUIScript : MonoBehaviour
    {
        [SerializeField] private TMP_Text _optionText;
        public string nodeId;
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
            _optionText.text = LevelGraphContext.CurrentGraph.nodeLookup[nodeId].Node.Basic.Description;
        }

        public void OnOptionClicked()
        {
            // 当选项被点击时，向外部发送事件，告知玩家选择了哪个节点
            UIEventDispatcher.DispatchNodeOptionSubmitted(nodeId);
        }
    }
}