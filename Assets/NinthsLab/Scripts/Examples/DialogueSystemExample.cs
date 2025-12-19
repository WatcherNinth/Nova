using System.Collections.Generic;
using UnityEngine;
using Interrorgation.MidLayer;
using FrontendEngine.Dialogue.Events;
using FrontendEngine.Dialogue.Models;

namespace NinthsLab.Examples
{
    /// <summary>
    /// 对话系统使用示例
    /// 这是一个演示脚本，展示如何在实际游戏中使用对话系统
    /// 
    /// 包含:
    ///   1. 后端触发对话
    ///   2. 前端接收并显示对话
    ///   3. 用户交互处理
    /// </summary>
    public class DialogueSystemExample : MonoBehaviour
    {
        [SerializeField]
        private bool autoPlayExample = true;

        #region 示例 1: 后端生成对话 (模拟 NodeLogicManager)

        /// <summary>
        /// 示例: 后端逻辑生成对话并触发事件
        /// 这应该在 NodeLogicManager.TryProveNode() 或类似地方调用
        /// </summary>
        public void ExampleBackendGenerateDialogue()
        {
            Debug.Log("[Example] === 后端生成对话 ===");

            // 模拟后端获取 NodeData 中的对话信息
            var dialogueLines = new List<string>
            {
                "[FadeInOut] Alice: Welcome to the investigation room.",
                "Alice: We have several pieces of evidence to examine.",
                "[Shake|intensity=0.5] Bob: That looks suspicious!",
                "[SlideIn] Carol: Let me take a closer look.",
                "Alice: What do you think this means?",
                "[ScaleUp] Bob: It could be important!"
            };

            // 后端触发事件 - 这是数据流的起点
            Debug.Log($"[Example] 触发 GameEventDispatcher.DispatchDialogueGenerated() ({dialogueLines.Count} 行)");
            GameEventDispatcher.DispatchDialogueGenerated(dialogueLines);

            // 幕后发生的事:
            // 1. GameEventDispatcher.OnDialogueGenerated 事件触发
            // 2. Game_UI_Coordinator.HandleDialogueGenerated() 被调用
            // 3. DialogueLogicAdapter.ProcessDialogue() 解析每一行
            //    - 提取 [特效] 标记
            //    - 解析 "角色: 文本"
            //    - 生成 DialogueDisplayData 对象
            // 4. FrontendDialogueEventBus.RaiseRequestDialogueDisplay() 触发
            // 5. 前端 UI 组件接收 DialogueDisplayData
            // 6. 显示对话内容
        }

        #endregion

        #region 示例 2: 前端接收并显示对话 (UI 层)

        /// <summary>
        /// 示例: 前端 UI 组件如何订阅和显示对话
        /// 这段代码应该在 DialogueUIPanel 或 DialogueTextBox 中
        /// </summary>
        public void ExampleFrontendReceiveDialogue()
        {
            Debug.Log("[Example] === 前端接收对话 ===");

            // 订阅对话显示事件
            FrontendDialogueEventBus.OnRequestDialogueDisplay += OnDialogueDisplayRequested;
            FrontendDialogueEventBus.OnRequestChoicesDisplay += OnChoicesDisplayRequested;
            FrontendDialogueEventBus.OnRequestDialogueClear += OnDialogueClearRequested;

            Debug.Log("[Example] 前端已订阅对话事件");
        }

        /// <summary>
        /// 当前端收到对话显示请求时的处理
        /// </summary>
        private void OnDialogueDisplayRequested(DialogueDisplayData displayData)
        {
            Debug.Log("[Example] 前端收到对话显示请求");
            Debug.Log($"  角色: {displayData.Character.Name}");
            Debug.Log($"  文本: {displayData.Text}");
            Debug.Log($"  位置: {displayData.Character.Position}");
            Debug.Log($"  特效: {displayData.Effects.Count} 个");

            foreach (var effect in displayData.Effects)
            {
                Debug.Log($"    - {effect.Type} (持续: {effect.Duration}s)");
            }

            // 实际游戏中的处理:
            // 1. 加载角色立绘 (通过 Character.SpriteResourcePath)
            // 2. 显示角色名 (Character.Name)
            // 3. 按特效播放文本 (Effects)
            // 4. 显示对话文本 (Text)
            // 5. 等待用户点击以推进
        }

        /// <summary>
        /// 当前端收到选项显示请求时的处理
        /// </summary>
        private void OnChoicesDisplayRequested(List<DialogueChoice> choices)
        {
            Debug.Log("[Example] 前端收到选项显示请求");
            Debug.Log($"  选项数量: {choices.Count}");

            for (int i = 0; i < choices.Count; i++)
            {
                var choice = choices[i];
                Debug.Log($"  [{i}] {choice.DisplayText}");
                if (choice.IsDisabled)
                    Debug.Log($"       (禁用: {choice.DisabledReason})");
            }

            // 实际游戏中的处理:
            // 1. 为每个选项创建一个按钮
            // 2. 设置按钮文本 (DisplayText)
            // 3. 根据 IsDisabled 设置按钮状态
            // 4. 绑定点击事件到 FrontendDialogueEventBus.RaiseUserSelectChoice()
        }

        /// <summary>
        /// 当前端收到清除对话请求时的处理
        /// </summary>
        private void OnDialogueClearRequested()
        {
            Debug.Log("[Example] 前端收到清除对话请求");

            // 实际游戏中的处理:
            // 1. 播放对话框隐藏动画
            // 2. 隐藏角色立绘
            // 3. 清除文本内容
            // 4. 准备下一段对话或流程
        }

        #endregion

        #region 示例 3: 用户交互处理 (点击选项)

        /// <summary>
        /// 示例: 用户点击了一个选项，如何处理
        /// 这段代码应该在 ChoiceButtonGroup 中
        /// </summary>
        public void ExampleUserSelectChoice()
        {
            Debug.Log("[Example] === 用户选择选项 ===");

            // 模拟用户点击的选项
            var selectedChoice = new DialogueChoice
            {
                Id = "choice_accept_mission",
                DisplayText = "Accept the mission",
                TargetPhaseId = "phase_2_investigation",
                Priority = 10
            };

            Debug.Log($"[Example] 用户点击了: {selectedChoice.DisplayText}");
            Debug.Log($"[Example] 选项ID: {selectedChoice.Id}");
            Debug.Log($"[Example] 目标阶段: {selectedChoice.TargetPhaseId}");

            // 前端触发选择事件 - 这是数据流的折返点
            FrontendDialogueEventBus.RaiseUserSelectChoice(selectedChoice);

            // 幕后发生的事:
            // 1. FrontendDialogueEventBus.OnUserSelectChoice 事件触发
            // 2. DialogueUIAdapter.HandleUserSelectChoice() 被调用
            // 3. 验证选项数据的有效性
            // 4. DialogueLogicAdapter.ClearDialogue() 清除UI
            // 5. GameEventDispatcher.DispatchPlayerInputString(selectedChoice.Id) 转发给后端
            // 6. 后端逻辑处理用户选择:
            //    - NodeLogicManager 获取选项ID
            //    - 查找对应的目标节点
            //    - 执行相关逻辑
            //    - 生成新的对话 (回到步骤 1)
        }

        #endregion

        #region 示例 4: 完整流程演示

        /// <summary>
        /// 演示完整的对话流程: 后端 → 前端 → 用户 → 后端
        /// </summary>
        public void ExampleCompleteFlow()
        {
            Debug.Log("[Example] ========== 完整流程演示开始 ==========");

            // 第一阶段: 后端生成初始对话
            Debug.Log("\n[阶段1] 后端生成初始对话");
            var initialDialogues = new List<string>
            {
                "Alice: We've found an interesting clue.",
                "Alice: What do you make of it?",
                "[TypewriterEffect] Bob: Let me examine it closer."
            };

            GameEventDispatcher.DispatchDialogueGenerated(initialDialogues);
            Debug.Log("✓ 对话已发送到前端");

            // 第二阶段: 生成选项
            Debug.Log("\n[阶段2] 生成用户选项");
            var choices = new List<DialogueChoice>
            {
                new DialogueChoice
                {
                    Id = "analyze_evidence",
                    DisplayText = "Analyze the evidence further",
                    TargetPhaseId = "phase_analysis",
                    Priority = 10
                },
                new DialogueChoice
                {
                    Id = "ask_for_help",
                    DisplayText = "Ask for help from the team",
                    TargetPhaseId = "phase_team_help",
                    Priority = 20
                },
                new DialogueChoice
                {
                    Id = "record_evidence",
                    DisplayText = "Record this evidence",
                    TargetPhaseId = "phase_record",
                    Priority = 5
                }
            };

            FrontendDialogueEventBus.RaiseRequestChoicesDisplay(choices);
            Debug.Log("✓ 选项已发送到前端");

            // 第三阶段: 用户选择
            Debug.Log("\n[阶段3] 用户选择了: Ask for help from the team");
            var userChoice = choices[1];
            FrontendDialogueEventBus.RaiseUserSelectChoice(userChoice);
            Debug.Log($"✓ 选择已转发到后端: {userChoice.TargetPhaseId}");

            // 第四阶段: 后端响应
            Debug.Log("\n[阶段4] 后端根据用户选择生成新对话");
            var responseDialogues = new List<string>
            {
                "[FadeInOut] Alice: Good thinking!",
                "Team: We'll help you investigate.",
                "[Shake] Carol: Wait, I see something else!"
            };

            GameEventDispatcher.DispatchDialogueGenerated(responseDialogues);
            Debug.Log("✓ 新对话已发送到前端\n");

            Debug.Log("[Example] ========== 完整流程演示结束 ==========");
        }

        #endregion

        #region 示例 5: 特效标记使用示例

        /// <summary>
        /// 展示如何在对话中使用特效标记
        /// </summary>
        public void ExampleEffectTags()
        {
            Debug.Log("[Example] === 特效标记示例 ===");

            var dialoguesWithEffects = new List<string>
            {
                // 单个特效
                "[FadeInOut] Alice: I'm fading in...",
                "[Shake] Bob: This is shaking!",
                "[ScaleUp] Carol: I'm growing!",

                // 多个特效
                "[FadeInOut][Shake] Alice: Fading and shaking!",
                "[SlideIn][ScaleUp] Bob: Sliding and scaling!",

                // 带参数的特效
                "[FadeInOut|duration=2.0] Alice: Slow fade",
                "[Shake|intensity=0.8] Bob: Strong shake",
                "[TypewriterEffect|speed=0.1] Carol: Slow typing effect",

                // 无特效 (普通对话)
                "Alice: This is a normal dialogue.",

                // 旁白 (无冒号)
                "The room fell silent."
            };

            Debug.Log($"处理 {dialoguesWithEffects.Count} 条对话...");
            GameEventDispatcher.DispatchDialogueGenerated(dialoguesWithEffects);

            Debug.Log("\n特效类型参考:");
            Debug.Log("  • None - 无特效");
            Debug.Log("  • FadeInOut - 渐隐渐显 (Alpha淡入淡出)");
            Debug.Log("  • BounceIn - 弹跳入场");
            Debug.Log("  • SlideIn - 平移进入");
            Debug.Log("  • Shake - 抖动特效");
            Debug.Log("  • ScaleUp - 放大动画");
            Debug.Log("  • TypewriterEffect - 打字效果");
            Debug.Log("  • Flash - 闪光特效");
            Debug.Log("  • RotateIn - 旋转进入");
        }

        #endregion

        void Start()
        {
            if (autoPlayExample)
            {
                ExampleFrontendReceiveDialogue();
                ExampleCompleteFlow();
                ExampleEffectTags();
            }
        }

        void OnDestroy()
        {
            // 清理订阅
            FrontendDialogueEventBus.OnRequestDialogueDisplay -= OnDialogueDisplayRequested;
            FrontendDialogueEventBus.OnRequestChoicesDisplay -= OnChoicesDisplayRequested;
            FrontendDialogueEventBus.OnRequestDialogueClear -= OnDialogueClearRequested;
        }
    }
}
