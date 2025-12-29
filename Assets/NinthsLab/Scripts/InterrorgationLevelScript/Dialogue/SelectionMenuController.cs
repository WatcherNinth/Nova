using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FrontendEngine.UI
{
    public class SelectionMenuController : MonoBehaviour
    {
        // 单例模式，方便全局调用
        public static SelectionMenuController Instance { get; private set; }

        [Header("Components")]
        [SerializeField] private Button btnCopy;
        [SerializeField] private Button btnAddToJournal;
        [SerializeField] private RectTransform menuRect; // 菜单自身的 RectTransform

        // 缓存当前选中的纯文本
        private string _currentSelectedText;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            // 默认隐藏
            gameObject.SetActive(false);

            // 绑定事件
            btnCopy.onClick.AddListener(OnCopyClicked);
            btnAddToJournal.onClick.AddListener(OnJournalClicked);
        }

        /// <summary>
        /// 显示菜单
        /// </summary>
        /// <param name="worldPosition">菜单应该出现的世界坐标（通常是选区末尾字符的位置）</param>
        /// <param name="selectedText">选中的纯文本内容</param>
        public void Show(Vector3 worldPosition, string selectedText)
        {
            _currentSelectedText = selectedText;
            
            // 1. 设置位置
            transform.position = worldPosition;
            
            // 2. 简单的边界检查（防止菜单跑出屏幕，可选优化）
            // KeepInScreen(); 

            // 3. 显示
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void OnCopyClicked()
        {
            if (!string.IsNullOrEmpty(_currentSelectedText))
            {
                // Unity 自带的剪贴板 API
                GUIUtility.systemCopyBuffer = _currentSelectedText;
                Debug.Log($"[Menu] 已复制: {_currentSelectedText}");
            }
            Hide();
            
            // 这里通常还需要通知 Handler 清除高亮，稍后通过事件或回调处理
            // 目前先保持高亮，让玩家知道自己复制了啥
        }

        private void OnJournalClicked()
        {
            Debug.Log($"[Menu] 添加手账: {_currentSelectedText} (功能待实现)");
            Hide();
        }
    }
}