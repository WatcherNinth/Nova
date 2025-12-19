using System;
using System.Collections.Generic;
using FrontendEngine.Dialogue.Models;

namespace FrontendEngine.Dialogue.Events
{
    /// <summary>
    /// 前端对话事件总线
    /// 职责: 完全解耦前端UI与中间层逻辑
    /// 数据流: 
    ///   后端 → Game_UI_Coordinator → DialogueLogicAdapter → FrontendDialogueEventBus → UI组件
    ///   UI组件 → FrontendDialogueEventBus → DialogueUIAdapter → Game_UI_Coordinator → 后端逻辑
    /// </summary>
    public static class FrontendDialogueEventBus
    {
        // ==========================================
        // 后端 → 前端的事件 (单向)
        // ==========================================

        /// <summary>
        /// 请求显示对话
        /// 发送者: DialogueLogicAdapter
        /// 接收者: DialogueUIPanel 及其子组件
        /// </summary>
        public static event Action<DialogueDisplayData> OnRequestDialogueDisplay;

        /// <summary>
        /// 请求显示选项按钮组
        /// 发送者: DialogueLogicAdapter
        /// 接收者: ChoiceButtonGroup
        /// </summary>
        public static event Action<List<DialogueChoice>> OnRequestChoicesDisplay;

        /// <summary>
        /// 清除对话UI (过渡动画)
        /// 发送者: DialogueLogicAdapter
        /// 接收者: DialogueUIPanel
        /// </summary>
        public static event Action OnRequestDialogueClear;

        // ==========================================
        // 前端 → 后端的事件 (单向)
        // ==========================================

        /// <summary>
        /// 用户选择了一个选项
        /// 发送者: ChoiceButtonGroup
        /// 接收者: DialogueUIAdapter
        /// </summary>
        public static event Action<DialogueChoice> OnUserSelectChoice;

        /// <summary>
        /// 用户要求推进对话 (跳过当前对话)
        /// 发送者: DialogueTextBox
        /// 接收者: DialogueUIAdapter
        /// </summary>
        public static event Action OnUserRequestAdvance;

        // ==========================================
        // 发送事件方法 (供中间层调用)
        // ==========================================

        /// <summary>
        /// 请求显示对话
        /// </summary>
        public static void RaiseRequestDialogueDisplay(DialogueDisplayData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data), "[FrontendDialogueEventBus] DialogueDisplayData 不能为 null");
            }
            OnRequestDialogueDisplay?.Invoke(data);
        }

        /// <summary>
        /// 请求显示选项列表
        /// </summary>
        public static void RaiseRequestChoicesDisplay(List<DialogueChoice> choices)
        {
            if (choices == null)
            {
                throw new ArgumentNullException(nameof(choices), "[FrontendDialogueEventBus] DialogueChoice 列表不能为 null");
            }
            OnRequestChoicesDisplay?.Invoke(choices);
        }

        /// <summary>
        /// 请求清除对话UI
        /// </summary>
        public static void RaiseRequestDialogueClear()
        {
            OnRequestDialogueClear?.Invoke();
        }

        /// <summary>
        /// 用户选择了一个选项
        /// </summary>
        public static void RaiseUserSelectChoice(DialogueChoice choice)
        {
            if (choice == null)
            {
                throw new ArgumentNullException(nameof(choice), "[FrontendDialogueEventBus] DialogueChoice 不能为 null");
            }
            OnUserSelectChoice?.Invoke(choice);
        }

        /// <summary>
        /// 用户请求推进对话
        /// </summary>
        public static void RaiseUserRequestAdvance()
        {
            OnUserRequestAdvance?.Invoke();
        }

        // ==========================================
        // 清理方法 (用于单元测试)
        // ==========================================

        /// <summary>
        /// 清除所有订阅 (仅供测试使用)
        /// </summary>
        public static void ClearAllSubscriptions()
        {
            OnRequestDialogueDisplay = null;
            OnRequestChoicesDisplay = null;
            OnRequestDialogueClear = null;
            OnUserSelectChoice = null;
            OnUserRequestAdvance = null;
        }
    }
}
