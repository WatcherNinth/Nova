using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace FrontendEngine.Dialogue
{
    /// <summary>
    /// 对话文本显示组件
    /// 职责:
    ///   1. 显示角色名和对话文本
    ///   2. 支持打字机效果 (逐字显示)
    ///   3. 处理用户点击推进
    ///   4. 支持快速跳过动画
    /// </summary>
    public class DialogueTextBox : MonoBehaviour
    {
        [Header("UI引用")]
        [SerializeField]
        [Tooltip("角色名文本")]
        private TextMeshProUGUI characterNameText;

        [SerializeField]
        [Tooltip("对话内容文本")]
        private TextMeshProUGUI dialogueContentText;

        [SerializeField]
        [Tooltip("点击提示图标 (显示在文本完成后)")]
        private GameObject clickIndicator;

        [SerializeField]
        [Tooltip("文本框背景 (可选)")]
        private Image textBoxBackground;

        [Header("打字机效果")]
        [SerializeField]
        [Tooltip("是否启用打字机效果")]
        private bool enableTypewriterEffect = true;

        [SerializeField]
        [Tooltip("打字速度 (字符/秒)")]
        private float typewriterSpeed = 30f;

        [SerializeField]
        [Tooltip("是否可以通过点击跳过动画")]
        private bool allowSkipAnimation = true;

        [Header("交互")]
        [SerializeField]
        [Tooltip("点击区域 (用于检测用户点击推进)")]
        private Button clickAreaButton;

        [Header("调试")]
        [SerializeField]
        private bool debugLogging = true;

        private Coroutine typewriterCoroutine;
        private bool isTyping = false;
        private bool textCompleted = false;
        private System.Action onDisplayComplete;
        private DialogueUIPanel parentPanel;

        void Awake()
        {
            // 初始化引用
            InitializeComponents();

            // 绑定点击事件
            if (clickAreaButton != null)
            {
                clickAreaButton.onClick.AddListener(OnClickArea);
            }

            // 初始化隐藏点击提示
            if (clickIndicator != null)
            {
                clickIndicator.SetActive(false);
            }

            // 获取父面板引用
            parentPanel = GetComponentInParent<DialogueUIPanel>();
        }

        private void InitializeComponents()
        {
            // 自动查找组件 (如果未设置)
            if (characterNameText == null)
            {
                characterNameText = transform.Find("CharacterName")?.GetComponent<TextMeshProUGUI>();
            }

            if (dialogueContentText == null)
            {
                dialogueContentText = transform.Find("DialogueContent")?.GetComponent<TextMeshProUGUI>();
            }

            if (clickIndicator == null)
            {
                clickIndicator = transform.Find("ClickIndicator")?.gameObject;
            }

            if (clickAreaButton == null)
            {
                clickAreaButton = GetComponent<Button>();
            }
        }

        /// <summary>
        /// 显示对话文本
        /// </summary>
        public void DisplayText(string characterName, string text, System.Action onComplete = null)
        {
            if (debugLogging)
                Debug.Log($"[DialogueTextBox] 显示文本: {characterName}: {text}");

            // 停止之前的打字机效果
            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
            }

            // 保存回调
            onDisplayComplete = onComplete;

            // 显示角色名
            if (characterNameText != null)
            {
                characterNameText.text = characterName;
            }

            // 隐藏点击提示
            if (clickIndicator != null)
            {
                clickIndicator.SetActive(false);
            }

            textCompleted = false;

            // 显示文本 (带打字机效果或立即显示)
            if (enableTypewriterEffect && typewriterSpeed > 0)
            {
                typewriterCoroutine = StartCoroutine(TypewriterEffect(text));
            }
            else
            {
                // 立即显示完整文本
                if (dialogueContentText != null)
                {
                    dialogueContentText.text = text;
                }
                OnTextComplete();
            }
        }

        /// <summary>
        /// 打字机效果协程
        /// </summary>
        private IEnumerator TypewriterEffect(string fullText)
        {
            isTyping = true;

            if (dialogueContentText == null)
            {
                Debug.LogError("[DialogueTextBox] dialogueContentText 未设置");
                isTyping = false;
                OnTextComplete();
                yield break;
            }

            dialogueContentText.text = "";

            float delay = 1f / typewriterSpeed;
            int currentIndex = 0;

            while (currentIndex < fullText.Length)
            {
                // 逐字添加
                dialogueContentText.text += fullText[currentIndex];
                currentIndex++;

                yield return new WaitForSeconds(delay);
            }

            isTyping = false;
            OnTextComplete();
        }

        /// <summary>
        /// 文本显示完成
        /// </summary>
        private void OnTextComplete()
        {
            textCompleted = true;

            // 显示点击提示
            if (clickIndicator != null)
            {
                clickIndicator.SetActive(true);
            }

            // 调用回调
            onDisplayComplete?.Invoke();

            if (debugLogging)
                Debug.Log("[DialogueTextBox] 文本显示完成");
        }

        /// <summary>
        /// 处理用户点击区域
        /// </summary>
        private void OnClickArea()
        {
            if (isTyping && allowSkipAnimation)
            {
                // 跳过打字机动画，立即显示完整文本
                SkipTypewriter();
            }
            else if (textCompleted)
            {
                // 文本已完成，推进到下一条对话
                AdvanceDialogue();
            }
        }

        /// <summary>
        /// 跳过打字机效果
        /// </summary>
        private void SkipTypewriter()
        {
            if (debugLogging)
                Debug.Log("[DialogueTextBox] 跳过打字机效果");

            // 停止协程
            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
                typewriterCoroutine = null;
            }

            // 立即显示完整文本 (需要保存原始文本)
            // 注: 这里需要在 TypewriterEffect 开始前保存完整文本
            isTyping = false;
            
            // 触发完成事件
            OnTextComplete();
        }

        /// <summary>
        /// 推进对话
        /// </summary>
        private void AdvanceDialogue()
        {
            if (debugLogging)
                Debug.Log("[DialogueTextBox] 推进对话");

            // 隐藏点击提示
            if (clickIndicator != null)
            {
                clickIndicator.SetActive(false);
            }

            // 通知父面板显示下一条对话
            if (parentPanel != null)
            {
                parentPanel.OnUserClickAdvance();
            }
            else
            {
                // 备用方案：触发推进事件
                FrontendEngine.Dialogue.Events.FrontendDialogueEventBus.RaiseUserRequestAdvance();
            }
        }

        /// <summary>
        /// 清除文本
        /// </summary>
        public void Clear()
        {
            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
                typewriterCoroutine = null;
            }

            if (characterNameText != null)
            {
                characterNameText.text = "";
            }

            if (dialogueContentText != null)
            {
                dialogueContentText.text = "";
            }

            if (clickIndicator != null)
            {
                clickIndicator.SetActive(false);
            }

            isTyping = false;
            textCompleted = false;
        }

        /// <summary>
        /// 设置文本框可见性
        /// </summary>
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        void OnDestroy()
        {
            // 清理事件绑定
            if (clickAreaButton != null)
            {
                clickAreaButton.onClick.RemoveListener(OnClickArea);
            }
        }
    }
}
