using UnityEngine;
using System.Collections.Generic;
using DialogueSystem; // 引用对话系统
using System.Linq;

namespace FrontendEngine.Tests
{
    /// <summary>
    /// 前端模拟测试器
    /// 作用：充当“虚假的后端”，直接向对话系统投喂文本，用于测试 UI 表现和指令解析。
    /// </summary>
    public class FrontendMockTester : MonoBehaviour
    {
        [Header("核心组件引用")]
        [Tooltip("场景中的对话运行时管理器")]
        public DialogueRuntimeManager runtimeManager;

        [Header("模拟剧本输入")]
        [Tooltip("在这里写 Nova 格式的脚本。支持多行、空行切换、指令等。")]
        [TextArea(10, 20)]
        public string rawScriptInput = @"<| show('AnQiao', 'Normal') |>
安乔::你好！这是前端测试模式。

这是一个没有后端的纯 UI 测试。
点击屏幕继续...

<| show('AnQiao', 'Smile') |>
安乔::看，我现在切换到了笑脸差分！

<| hide('AnQiao') |>
安乔::我现在隐身了（虽然名字还在）。

旁白::测试结束。";

        [Header("控制")]
        public bool autoPlayOnStart = false;

        [Header("头像测试")]
        [Tooltip("设置谁是主角 (对应 StandardDialogueUI 的 ProtagonistId)")]
        public string testProtagonistId = "AnLee";

        [Tooltip("场景中的 UI 控制器 (需要拖拽赋值)")]
        public FrontendEngine.StandardDialogueUI dialogueUI;

        private void Start()
        {
            // [新增] 在开始时将测试的主角ID注入到 UI 中
            if (dialogueUI != null)
            {
                dialogueUI.protagonistId = testProtagonistId;
            }

            if (autoPlayOnStart)
            {
                RunSimulation();
            }
        }


        [ContextMenu("▶ 运行模拟 (Push Script)")]
        public void RunSimulation()
        {
            if (runtimeManager == null)
            {
                Debug.LogError("❌ [FrontendMockTester] 未绑定 DialogueRuntimeManager！");
                return;
            }

            Debug.Log($"<color=cyan>[Mock] 开始向前端推送模拟数据...</color>");

            // 1. 将 Inspector 中的大段文本转换为 List<string>
            // 我们模拟后端行为：后端可能发来一行行的，也可能发来带换行符的一大块
            // 为了测试 Parser 的健壮性，我们直接把整个文本作为一个元素发过去，
            // 让 NovaScriptParser 去处理内部的换行符。
            List<string> mockBatch = new List<string> { rawScriptInput };

            // 2. 推送给对话系统
            runtimeManager.PushNewBatch(mockBatch);
        }

        // 辅助测试：清空当前对话
        [ContextMenu("⏹ 强制停止/清空")]
        public void ClearDialogue()
        {
            // 如果 DialogueRuntimeManager 有清空功能可以调，或者重新加载场景
            // 目前简单做法是打印日志
            Debug.Log("[Mock] 模拟结束 (UI 状态需手动重置或等待播放完毕)");
        }
    }
}