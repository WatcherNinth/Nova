using UnityEngine;
using System.Collections.Generic;
using FrontendEngine.Dialogue.Events;
using FrontendEngine.Dialogue.Models;

namespace FrontendEngine.Dialogue
{
    /// <summary>
    /// 对话UI主面板控制器
    /// 职责:
    ///   1. 管理所有对话UI子组件 (文本框、立绘、背景、选项)
    ///   2. 订阅 FrontendDialogueEventBus 事件
    ///   3. 协调各子组件的显示和隐藏
    ///   4. 控制对话流程 (显示→等待→清除)
    /// </summary>
    public class DialogueUIPanel : MonoBehaviour
    {
        [Header("子组件引用")]
        [SerializeField]
        [Tooltip("文本显示组件")]
        private DialogueTextBox textBox;

        [SerializeField]
        [Tooltip("角色立绘组件 (可以有多个，用于多角色显示)")]
        private CharacterView[] characterViews;

        [SerializeField]
        [Tooltip("场景背景组件")]
        private SceneView sceneView;

        [SerializeField]
        [Tooltip("选项按钮组")]
        private ChoiceButtonGroup choiceButtonGroup;

        [Header("显示控制")]
        [SerializeField]
        [Tooltip("是否自动隐藏对话框 (当没有对话时)")]
        private bool autoHide = true;

        [SerializeField]
        [Tooltip("对话框容器 (用于整体显示/隐藏)")]
        private CanvasGroup panelCanvasGroup;

        [Header("调试")]
        [SerializeField]
        private bool debugLogging = true;

        private Queue<DialogueDisplayData> dialogueQueue = new Queue<DialogueDisplayData>();
        private bool isDisplaying = false;
        private DialogueDisplayData currentDialogue;

        void Awake()
        {
            // 初始化子组件引用
            InitializeComponents();

            // 初始化时隐藏面板
            if (autoHide && panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 0f;
                panelCanvasGroup.blocksRaycasts = false;
            }
        }

        void OnEnable()
        {
            // 订阅前端事件总线
            FrontendDialogueEventBus.OnRequestDialogueDisplay += OnDialogueDisplayRequested;
            FrontendDialogueEventBus.OnRequestChoicesDisplay += OnChoicesDisplayRequested;
            FrontendDialogueEventBus.OnRequestDialogueClear += OnDialogueClearRequested;

            if (debugLogging)
                Debug.Log("[DialogueUIPanel] 已订阅前端事件");
        }

        void OnDisable()
        {
            // 取消订阅
            FrontendDialogueEventBus.OnRequestDialogueDisplay -= OnDialogueDisplayRequested;
            FrontendDialogueEventBus.OnRequestChoicesDisplay -= OnChoicesDisplayRequested;
            FrontendDialogueEventBus.OnRequestDialogueClear -= OnDialogueClearRequested;
        }

        /// <summary>
        /// 初始化子组件引用
        /// </summary>
        private void InitializeComponents()
        {
            // 自动查找子组件 (如果未在Inspector中设置)
            if (textBox == null)
            {
                textBox = GetComponentInChildren<DialogueTextBox>();
                if (textBox == null)
                    Debug.LogWarning("[DialogueUIPanel] 未找到 DialogueTextBox 组件");
            }

            if (characterViews == null || characterViews.Length == 0)
            {
                characterViews = GetComponentsInChildren<CharacterView>();
                if (characterViews.Length == 0)
                    Debug.LogWarning("[DialogueUIPanel] 未找到 CharacterView 组件");
            }

            if (sceneView == null)
            {
                sceneView = GetComponentInChildren<SceneView>();
                if (sceneView == null)
                    Debug.LogWarning("[DialogueUIPanel] 未找到 SceneView 组件");
            }

            if (choiceButtonGroup == null)
            {
                choiceButtonGroup = GetComponentInChildren<ChoiceButtonGroup>();
                if (choiceButtonGroup == null)
                    Debug.LogWarning("[DialogueUIPanel] 未找到 ChoiceButtonGroup 组件");
            }

            if (panelCanvasGroup == null)
            {
                panelCanvasGroup = GetComponent<CanvasGroup>();
            }
        }

        #region 事件处理器

        /// <summary>
        /// 处理对话显示请求
        /// </summary>
        private void OnDialogueDisplayRequested(DialogueDisplayData data)
        {
            if (data == null)
            {
                Debug.LogError("[DialogueUIPanel] 接收到 null 的 DialogueDisplayData");
                return;
            }

            if (debugLogging)
                Debug.Log($"[DialogueUIPanel] 接收到对话: {data.Character.Name}: {data.Text}");

            // 加入队列
            dialogueQueue.Enqueue(data);

            // 如果当前没有正在显示的对话，立即显示
            if (!isDisplaying)
            {
                DisplayNextDialogue();
            }
        }

        /// <summary>
        /// 处理选项显示请求
        /// </summary>
        private void OnChoicesDisplayRequested(List<DialogueChoice> choices)
        {
            if (choices == null || choices.Count == 0)
            {
                Debug.LogWarning("[DialogueUIPanel] 接收到空的选项列表");
                return;
            }

            if (debugLogging)
                Debug.Log($"[DialogueUIPanel] 显示 {choices.Count} 个选项");

            // 显示面板
            ShowPanel();

            // 转发给选项按钮组
            if (choiceButtonGroup != null)
            {
                choiceButtonGroup.DisplayChoices(choices);
            }
            else
            {
                Debug.LogError("[DialogueUIPanel] ChoiceButtonGroup 未设置，无法显示选项");
            }
        }

        /// <summary>
        /// 处理对话清除请求
        /// </summary>
        private void OnDialogueClearRequested()
        {
            if (debugLogging)
                Debug.Log("[DialogueUIPanel] 清除对话");

            ClearDialogue();
        }

        #endregion

        #region 对话显示逻辑

        /// <summary>
        /// 显示下一条对话
        /// </summary>
        private void DisplayNextDialogue()
        {
            if (dialogueQueue.Count == 0)
            {
                isDisplaying = false;
                
                // 如果设置了自动隐藏，隐藏面板
                if (autoHide)
                {
                    HidePanel();
                }
                
                return;
            }

            isDisplaying = true;
            currentDialogue = dialogueQueue.Dequeue();

            // 显示面板
            ShowPanel();

            // 更新场景 (如果有场景信息)
            if (sceneView != null && currentDialogue.Scene != null)
            {
                sceneView.UpdateScene(currentDialogue.Scene);
            }

            // 更新角色立绘
            UpdateCharacterDisplay(currentDialogue.Character);

            // 显示文本
            if (textBox != null)
            {
                textBox.DisplayText(
                    currentDialogue.Character.Name,
                    currentDialogue.Text,
                    OnTextDisplayComplete
                );
            }
            else
            {
                Debug.LogError("[DialogueUIPanel] TextBox 未设置");
                OnTextDisplayComplete();
            }
        }

        /// <summary>
        /// 更新角色立绘显示
        /// </summary>
        private void UpdateCharacterDisplay(CharacterDisplayInfo character)
        {
            if (characterViews == null || characterViews.Length == 0)
            {
                Debug.LogWarning("[DialogueUIPanel] 没有可用的 CharacterView");
                return;
            }

            // 简单策略：使用第一个 CharacterView
            // 后续可以根据 character.Position 选择不同的 View
            CharacterView targetView = GetCharacterViewForPosition(character.Position);

            if (targetView != null)
            {
                targetView.UpdateCharacter(character);
            }
            else
            {
                Debug.LogWarning($"[DialogueUIPanel] 未找到位置 {character.Position} 的 CharacterView");
            }
        }

        /// <summary>
        /// 根据位置获取对应的 CharacterView
        /// </summary>
        private CharacterView GetCharacterViewForPosition(CharacterPosition position)
        {
            // 简单实现：返回第一个可用的
            // TODO: 后续可以根据position标签或命名查找特定view
            if (characterViews.Length > 0)
                return characterViews[0];

            return null;
        }

        /// <summary>
        /// 文本显示完成回调
        /// </summary>
        private void OnTextDisplayComplete()
        {
            if (debugLogging)
                Debug.Log("[DialogueUIPanel] 文本显示完成");

            // 如果是自动推进的对话
            if (currentDialogue != null && currentDialogue.IsAutoAdvance)
            {
                // 延迟后自动显示下一条
                Invoke(nameof(DisplayNextDialogue), currentDialogue.AutoAdvanceDelay);
            }
            // 否则等待用户点击推进 (在 DialogueTextBox 中处理)
        }

        /// <summary>
        /// 用户点击推进对话 (由 TextBox 调用)
        /// </summary>
        public void OnUserClickAdvance()
        {
            if (debugLogging)
                Debug.Log("[DialogueUIPanel] 用户推进对话");

            // 显示下一条对话
            DisplayNextDialogue();
        }

        #endregion

        #region 面板显示/隐藏

        /// <summary>
        /// 显示面板
        /// </summary>
        private void ShowPanel()
        {
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 1f;
                panelCanvasGroup.blocksRaycasts = true;
            }

            gameObject.SetActive(true);
        }

        /// <summary>
        /// 隐藏面板
        /// </summary>
        private void HidePanel()
        {
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 0f;
                panelCanvasGroup.blocksRaycasts = false;
            }
        }

        /// <summary>
        /// 清除对话
        /// </summary>
        private void ClearDialogue()
        {
            // 清空队列
            dialogueQueue.Clear();
            isDisplaying = false;
            currentDialogue = null;

            // 清除文本
            if (textBox != null)
            {
                textBox.Clear();
            }

            // 隐藏角色
            if (characterViews != null)
            {
                foreach (var view in characterViews)
                {
                    view.Hide();
                }
            }

            // 隐藏选项
            if (choiceButtonGroup != null)
            {
                choiceButtonGroup.HideChoices();
            }

            // 隐藏面板
            if (autoHide)
            {
                HidePanel();
            }
        }

        #endregion
    }
}
