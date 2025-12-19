using System;
using UnityEngine;
using FrontendEngine.Dialogue.Events;
using FrontendEngine.Dialogue.Models;

namespace Interrorgation.MidLayer.Dialogue
{
    /// <summary>
    /// 对话UI适配器 - 前端 → 后端逻辑转换
    /// 职责:
    ///   1. 监听前端UI事件 (用户选择、推进等)
    ///   2. 将前端数据转换为后端逻辑能理解的形式
    ///   3. 调用后端 GameEventDispatcher 通知逻辑层
    /// 数据流: FrontendDialogueEventBus → HandleUserAction() → GameEventDispatcher
    /// 安全性: 验证所有用户输入，确保数据有效性
    /// 扩展性: 可轻松添加新的UI→逻辑映射
    /// </summary>
    public class DialogueUIAdapter : MonoBehaviour
    {
        [SerializeField]
        private bool debugLogging = true;

        private DialogueLogicAdapter dialogueLogicAdapter;

        void Awake()
        {
            // 查找配对的逻辑适配器
            dialogueLogicAdapter = GetComponent<DialogueLogicAdapter>();
            if (dialogueLogicAdapter == null)
            {
                Debug.LogError("[DialogueUIAdapter] 未找到 DialogueLogicAdapter，请将其添加到同一 GameObject");
            }
        }

        void OnEnable()
        {
            // 订阅前端UI事件
            FrontendDialogueEventBus.OnUserSelectChoice += HandleUserSelectChoice;
            FrontendDialogueEventBus.OnUserRequestAdvance += HandleUserRequestAdvance;
        }

        void OnDisable()
        {
            // 取消订阅
            FrontendDialogueEventBus.OnUserSelectChoice -= HandleUserSelectChoice;
            FrontendDialogueEventBus.OnUserRequestAdvance -= HandleUserRequestAdvance;
        }

        /// <summary>
        /// 处理用户选择了一个选项
        /// </summary>
        private void HandleUserSelectChoice(DialogueChoice choice)
        {
            if (choice == null)
            {
                Debug.LogError("[DialogueUIAdapter] DialogueChoice 为 null");
                return;
            }

            try
            {
                if (debugLogging)
                    Debug.Log($"[DialogueUIAdapter] 用户选择: {choice.Id} -> {choice.DisplayText}");

                // 验证选项数据
                ValidateChoice(choice);

                // 转换为后端理解的形式
                string selectedOptionId = choice.Id;
                string targetPhaseId = choice.TargetPhaseId;

                if (string.IsNullOrEmpty(selectedOptionId))
                {
                    Debug.LogError("[DialogueUIAdapter] 选项ID为空");
                    return;
                }

                // 清除前端UI
                dialogueLogicAdapter?.ClearDialogue();

                // 通知后端逻辑: 用户提交了选项
                // 注: 实际版本会调用 GameEventDispatcher.DispatchUserSelectOption(selectedOptionId, targetPhaseId)
                // 或直接调用 NodeLogicManager.SubmitOption(optionId)
                GameEventDispatcher.DispatchPlayerInputString(selectedOptionId);

                if (debugLogging)
                    Debug.Log($"[DialogueUIAdapter] 已转发选项到后端: {selectedOptionId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DialogueUIAdapter] 处理用户选择时出错: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 处理用户要求推进对话 (跳过当前行)
        /// </summary>
        private void HandleUserRequestAdvance()
        {
            try
            {
                if (debugLogging)
                    Debug.Log("[DialogueUIAdapter] 用户请求推进对话");

                // 可选: 通知后端有一个"推进"事件 (用于统计、动画等)
                // GameEventDispatcher.DispatchDialogueAdvance();

                if (debugLogging)
                    Debug.Log("[DialogueUIAdapter] 推进请求已处理");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DialogueUIAdapter] 处理推进请求时出错: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 验证选项数据的有效性
        /// </summary>
        private void ValidateChoice(DialogueChoice choice)
        {
            if (string.IsNullOrEmpty(choice.Id))
                throw new ArgumentException("DialogueChoice.Id 不能为空");

            if (string.IsNullOrEmpty(choice.DisplayText))
                throw new ArgumentException("DialogueChoice.DisplayText 不能为空");

            if (choice.IsDisabled)
                Debug.LogWarning($"[DialogueUIAdapter] 用户选择了禁用的选项: {choice.Id}");
        }

        /// <summary>
        /// 请求后端生成选项 (future-ready)
        /// </summary>
        public void RequestChoicesFromBackend()
        {
            if (debugLogging)
                Debug.Log("[DialogueUIAdapter] 请求后端生成选项");

            // 实际版本: GameEventDispatcher.DispatchRequestChoices();
        }

        /// <summary>
        /// 请求后端推进对话 (future-ready)
        /// </summary>
        public void RequestAdvanceFromBackend()
        {
            if (debugLogging)
                Debug.Log("[DialogueUIAdapter] 请求后端推进对话");

            // 实际版本: GameEventDispatcher.DispatchDialogueAdvanceRequest();
        }
    }
}
