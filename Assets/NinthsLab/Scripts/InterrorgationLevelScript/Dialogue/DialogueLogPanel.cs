using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DialogueSystem;

namespace FrontendEngine
{
    public class DialogueLogPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject panelRoot; // 面板本身的根节点 (用于开关)
        [SerializeField] private Transform contentContainer; // ScrollView 的 Content
        [SerializeField] private LogItemUI itemPrefab; // 单条日志的预制体
        [SerializeField] private ScrollRect scrollRect;

        [Header("Buttons")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button openButton; // 场景中一直显示的“Log”按钮

        // 对象池 (简单的优化，避免重复 Instantiate)
        private List<LogItemUI> _spawnedItems = new List<LogItemUI>();

        private void Start()
        {
            // 初始化状态
            panelRoot.SetActive(false);

            if (closeButton) closeButton.onClick.AddListener(Hide);
            if (openButton) openButton.onClick.AddListener(Show);
        }

        public void Show()
        {
            RefreshUI();
            panelRoot.SetActive(true);
            
            // 自动滚动到底部
            // 需要等待一帧让 UI 布局刷新，否则滚动位置可能不对
            StartCoroutine(ScrollToBottom());
        }

        public void Hide()
        {
            panelRoot.SetActive(false);
        }

        private void RefreshUI()
        {
            // 1. 获取数据
            if (DialogueLogManager.Instance == null) return;
            var logs = DialogueLogManager.Instance.GetLogs();

            int index = 0;
            
            // 2. 遍历数据并显示
            foreach (var entry in logs)
            {
                LogItemUI item;

                // 简单的对象池逻辑：如果池子里有就复用，没有就生成
                if (index < _spawnedItems.Count)
                {
                    item = _spawnedItems[index];
                    item.gameObject.SetActive(true);
                }
                else
                {
                    item = Instantiate(itemPrefab, contentContainer);
                    _spawnedItems.Add(item);
                }

                item.Setup(entry);
                index++;
            }

            // 3. 隐藏多余的池对象
            for (int i = index; i < _spawnedItems.Count; i++)
            {
                _spawnedItems[i].gameObject.SetActive(false);
            }
        }

        private System.Collections.IEnumerator ScrollToBottom()
        {
            yield return new WaitForEndOfFrame();
            if(scrollRect) scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}