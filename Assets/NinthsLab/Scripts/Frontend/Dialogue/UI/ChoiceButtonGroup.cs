using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using FrontendEngine.Dialogue.Models;
using FrontendEngine.Dialogue.Events;

namespace FrontendEngine.Dialogue
{
    /// <summary>
    /// 对话选项按钮组
    /// 职责:
    ///   1. 动态生成选项按钮
    ///   2. 处理用户点击选项
    ///   3. 管理选项的启用/禁用状态
    ///   4. 支持选项动画和视觉反馈
    /// </summary>
    public class ChoiceButtonGroup : MonoBehaviour
    {
        [Header("UI引用")]
        [SerializeField]
        [Tooltip("选项按钮的容器 (布局组)")]
        private Transform buttonContainer;

        [SerializeField]
        [Tooltip("选项按钮预制体")]
        private GameObject choiceButtonPrefab;

        [SerializeField]
        [Tooltip("整体容器的CanvasGroup")]
        private CanvasGroup canvasGroup;

        [Header("按钮样式")]
        [SerializeField]
        [Tooltip("正常状态颜色")]
        private Color normalColor = Color.white;

        [SerializeField]
        [Tooltip("禁用状态颜色")]
        private Color disabledColor = Color.gray;

        [SerializeField]
        [Tooltip("悬停状态颜色")]
        private Color hoverColor = new Color(0.9f, 0.9f, 1f);

        [Header("动画设置")]
        [SerializeField]
        [Tooltip("按钮淡入时长")]
        private float buttonFadeInDuration = 0.2f;

        [SerializeField]
        [Tooltip("按钮间隔时间 (逐个显示)")]
        private float buttonShowInterval = 0.1f;

        [Header("调试")]
        [SerializeField]
        private bool debugLogging = true;

        private List<GameObject> activeButtons = new List<GameObject>();
        private List<DialogueChoice> currentChoices = new List<DialogueChoice>();

        void Awake()
        {
            InitializeComponents();

            // 初始化为隐藏
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
            }
        }

        private void InitializeComponents()
        {
            // 自动查找容器
            if (buttonContainer == null)
            {
                buttonContainer = transform.Find("ButtonContainer");
                if (buttonContainer == null)
                {
                    // 使用自身作为容器
                    buttonContainer = transform;
                }
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }
        }

        /// <summary>
        /// 显示选项列表
        /// </summary>
        public void DisplayChoices(List<DialogueChoice> choices)
        {
            if (choices == null || choices.Count == 0)
            {
                Debug.LogWarning("[ChoiceButtonGroup] 选项列表为空");
                return;
            }

            if (debugLogging)
                Debug.Log($"[ChoiceButtonGroup] 显示 {choices.Count} 个选项");

            // 清除旧按钮
            ClearButtons();

            // 保存当前选项
            currentChoices = new List<DialogueChoice>(choices);

            // 生成新按钮
            CreateButtons(choices);

            // 显示容器
            ShowContainer();
        }

        /// <summary>
        /// 创建选项按钮
        /// </summary>
        private void CreateButtons(List<DialogueChoice> choices)
        {
            if (choiceButtonPrefab == null)
            {
                Debug.LogError("[ChoiceButtonGroup] choiceButtonPrefab 未设置");
                return;
            }

            // 按优先级排序
            var sortedChoices = new List<DialogueChoice>(choices);
            sortedChoices.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            for (int i = 0; i < sortedChoices.Count; i++)
            {
                var choice = sortedChoices[i];
                CreateButton(choice, i);
            }
        }

        /// <summary>
        /// 创建单个按钮
        /// </summary>
        private void CreateButton(DialogueChoice choice, int index)
        {
            // 实例化按钮
            GameObject buttonObj = Instantiate(choiceButtonPrefab, buttonContainer);
            activeButtons.Add(buttonObj);

            // 获取按钮组件
            Button button = buttonObj.GetComponent<Button>();
            if (button == null)
            {
                button = buttonObj.AddComponent<Button>();
            }

            // 设置文本
            TextMeshProUGUI textComponent = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = choice.DisplayText;
            }
            else
            {
                // 尝试使用普通Text
                Text legacyText = buttonObj.GetComponentInChildren<Text>();
                if (legacyText != null)
                {
                    legacyText.text = choice.DisplayText;
                }
            }

            // 设置按钮状态
            button.interactable = !choice.IsDisabled;

            // 设置颜色
            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = choice.IsDisabled ? disabledColor : normalColor;
            }

            // 绑定点击事件
            button.onClick.AddListener(() => OnChoiceClicked(choice));

            // 如果有禁用原因，添加悬停提示 (可选)
            if (choice.IsDisabled && !string.IsNullOrEmpty(choice.DisabledReason))
            {
                // TODO: 添加 Tooltip 组件显示禁用原因
                if (debugLogging)
                    Debug.Log($"[ChoiceButtonGroup] 选项禁用: {choice.DisplayText} ({choice.DisabledReason})");
            }

            // 初始化为透明 (用于淡入动画)
            CanvasGroup buttonCanvasGroup = buttonObj.GetComponent<CanvasGroup>();
            if (buttonCanvasGroup == null)
            {
                buttonCanvasGroup = buttonObj.AddComponent<CanvasGroup>();
            }
            buttonCanvasGroup.alpha = 0f;

            // 延迟淡入动画
            float delay = index * buttonShowInterval;
            StartCoroutine(FadeInButton(buttonCanvasGroup, delay));
        }

        /// <summary>
        /// 按钮淡入协程
        /// </summary>
        private System.Collections.IEnumerator FadeInButton(CanvasGroup buttonGroup, float delay)
        {
            // 等待延迟
            yield return new WaitForSeconds(delay);

            // 淡入
            float elapsed = 0f;
            while (elapsed < buttonFadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / buttonFadeInDuration;
                buttonGroup.alpha = Mathf.Lerp(0f, 1f, t);
                yield return null;
            }

            buttonGroup.alpha = 1f;
        }

        /// <summary>
        /// 处理选项点击
        /// </summary>
        private void OnChoiceClicked(DialogueChoice choice)
        {
            if (choice == null)
            {
                Debug.LogError("[ChoiceButtonGroup] 点击的选项为 null");
                return;
            }

            if (debugLogging)
                Debug.Log($"[ChoiceButtonGroup] 用户选择: {choice.DisplayText}");

            // 禁用所有按钮 (防止重复点击)
            DisableAllButtons();

            // 触发前端事件
            FrontendDialogueEventBus.RaiseUserSelectChoice(choice);

            // 隐藏选项组
            HideChoices();
        }

        /// <summary>
        /// 禁用所有按钮
        /// </summary>
        private void DisableAllButtons()
        {
            foreach (var buttonObj in activeButtons)
            {
                Button button = buttonObj.GetComponent<Button>();
                if (button != null)
                {
                    button.interactable = false;
                }
            }
        }

        /// <summary>
        /// 清除所有按钮
        /// </summary>
        private void ClearButtons()
        {
            foreach (var button in activeButtons)
            {
                Destroy(button);
            }

            activeButtons.Clear();
            currentChoices.Clear();
        }

        /// <summary>
        /// 显示容器
        /// </summary>
        private void ShowContainer()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            gameObject.SetActive(true);
        }

        /// <summary>
        /// 隐藏选项组
        /// </summary>
        public void HideChoices()
        {
            if (debugLogging)
                Debug.Log("[ChoiceButtonGroup] 隐藏选项");

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
            }

            // 延迟清除按钮 (等待动画完成)
            Invoke(nameof(ClearButtons), 0.3f);
        }

        /// <summary>
        /// 立即隐藏并清除
        /// </summary>
        public void HideImmediate()
        {
            HideChoices();
            ClearButtons();
        }

        /// <summary>
        /// 获取当前显示的选项数量
        /// </summary>
        public int GetChoiceCount()
        {
            return currentChoices.Count;
        }

        /// <summary>
        /// 检查是否正在显示选项
        /// </summary>
        public bool IsShowingChoices()
        {
            return canvasGroup != null && canvasGroup.alpha > 0f && activeButtons.Count > 0;
        }

        void OnDestroy()
        {
            // 清理所有按钮
            ClearButtons();
        }
    }
}
