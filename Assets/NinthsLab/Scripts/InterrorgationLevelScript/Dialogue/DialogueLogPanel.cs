using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DialogueSystem;

namespace FrontendEngine
{
    public class DialogueLogPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private TMP_Text fullLogText; 

        [Header("Buttons")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button openButton;

        [Header("Formatting")]
        [Tooltip("条目之间的间距 (换行符数量)")]
        [SerializeField] private int spacingLines = 2;

        private void Start()
        {
            panelRoot.SetActive(false);
            if (closeButton) closeButton.onClick.AddListener(Hide);
            if (openButton) openButton.onClick.AddListener(Show);
        }

        public void Show()
        {
            RefreshUI();
            panelRoot.SetActive(true);
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }

        public void Hide()
        {
            panelRoot.SetActive(false);
        }

        private void RefreshUI()
        {
            if (DialogueLogManager.Instance == null) return;
            var logs = DialogueLogManager.Instance.GetLogs();

            StringBuilder sb = new StringBuilder();

            foreach (var entry in logs)
            {
                if (!string.IsNullOrEmpty(entry.DisplayName))
                {
                    sb.Append($"{entry.DisplayName}\n");
                }
                sb.Append(entry.Content);

                for (int i = 0; i < spacingLines; i++)
                {
                    sb.Append("\n");
                }
            }

            // 1. 更新 UI 文本
            fullLogText.text = sb.ToString();

            // 2. [核心修复] 通知 SelectionHandler 文本变了，清空它的旧缓存
            var handler = fullLogText.GetComponent<FrontendEngine.UI.RichTextSelectionHandler>();
            if (handler != null)
            {
                handler.ResetCache();
            }
        }
    }
}