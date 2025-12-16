using UnityEngine;
using System.Collections.Generic;
using Interrorgation.MidLayer;

namespace LogicEngine.Tests
{
    public class DialogueTester : MonoBehaviour
    {
        [Header("1. 模拟操作")]
        [Tooltip("输入你要点击的选项/节点 ID (必须是已发现的)")]
        public string targetNodeIdToSubmit = "fifteenth_floor_bloodstain_falsified";

        [Header("2. 接收到的反馈")]
        [Tooltip("这里会显示后端返回的剧情文本")]
        public List<string> receivedDialogueLog = new List<string>();

        private void OnEnable()
        {
            // 监听对话生成事件
            GameEventDispatcher.OnDialogueGenerated += OnDialogueReceived;
        }

        private void OnDisable()
        {
            GameEventDispatcher.OnDialogueGenerated -= OnDialogueReceived;
        }

        [ContextMenu("👉 点击提交选项 (Submit Node)")]
        public void SubmitNode()
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("请先运行游戏 (Play Mode)！");
                return;
            }

            Debug.Log($"[DialogueTester] 模拟 UI 点击: {targetNodeIdToSubmit}");
            
            // 清空旧日志，准备接收新对话
            receivedDialogueLog.Clear();
            receivedDialogueLog.Add($"--- 开始请求: {System.DateTime.Now:HH:mm:ss} ---");

            // 发送事件：模拟 UI 点击
            GameEventDispatcher.DispatchNodeOptionSubmitted(targetNodeIdToSubmit);
        }

        // 回调处理
        private void OnDialogueReceived(List<string> lines)
        {
            Debug.Log($"<color=green>[DialogueTester] 收到 {lines.Count} 行对话。</color>");
            
            foreach (var line in lines)
            {
                // 将对话添加到 Inspector 面板的列表中
                receivedDialogueLog.Add(line);
            }
            
            // 为了方便看，如果是 Dirty 的，可以在这里强制刷新 Inspector (但在运行时通常不需要)
        }
    }
}