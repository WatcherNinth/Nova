using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FrontendEngine.Dialogue.Events;
using FrontendEngine.Dialogue.Models;

namespace Interrorgation.MidLayer.Dialogue
{
    /// <summary>
    /// 对话逻辑适配器 - 后端 → 前端数据转换
    /// 职责: 
    ///   1. 将后端 GameEventDispatcher 的 List&lt;string&gt; 对话转换为前端数据模型
    ///   2. 从后端 NodeData 提取角色、场景、选项等信息
    ///   3. 发送转换后的数据到 FrontendDialogueEventBus
    /// 数据流: GameEventDispatcher.OnDialogueGenerated → ProcessDialogue() → FrontendDialogueEventBus
    /// 安全性: 不改变后端数据，仅读取和转换
    /// 扩展性: 可轻松添加新的UI特效或数据字段
    /// </summary>
    public class DialogueLogicAdapter : MonoBehaviour
    {
        [SerializeField]
        private bool debugLogging = true;

        /// <summary>
        /// 处理后端生成的对话列表
        /// 适配 demo_v2.json 格式：支持多行文本中包含多个角色对话
        /// 例如："安·李：\n对话内容\n\n安乔：\n对话内容"
        /// </summary>
        public void ProcessDialogue(List<string> dialogueLinesFromBackend)
        {
            if (dialogueLinesFromBackend == null || dialogueLinesFromBackend.Count == 0)
            {
                Debug.LogWarning("[DialogueLogicAdapter] 接收到空对话列表");
                return;
            }

            try
            {
                if (debugLogging)
                    Debug.Log($"[DialogueLogicAdapter] 处理对话 ({dialogueLinesFromBackend.Count} 段)");

                // 处理每一段文本（每段可能包含多个角色对话）
                for (int i = 0; i < dialogueLinesFromBackend.Count; i++)
                {
                    string segment = dialogueLinesFromBackend[i];
                    
                    // 分割多角色对话
                    var dialogueList = SplitMultiCharacterDialogue(segment);
                    
                    // 发送每一条对话到前端
                    foreach (var displayData in dialogueList)
                    {
                        FrontendDialogueEventBus.RaiseRequestDialogueDisplay(displayData);
                        
                        if (debugLogging)
                            Debug.Log($"[DialogueLogicAdapter] → {displayData.Character?.Name ?? "[旁白]"}: {displayData.Text}");
                    }
                }

                if (debugLogging)
                    Debug.Log($"[DialogueLogicAdapter] 对话处理完成");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DialogueLogicAdapter] 处理对话时出错: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 分割包含多个角色对话的文本段
        /// demo_v2.json 格式：角色名：\n对话内容\n\n下一个角色名：\n对话内容
        /// </summary>
        private List<DialogueDisplayData> SplitMultiCharacterDialogue(string textSegment)
        {
            var result = new List<DialogueDisplayData>();
            
            if (string.IsNullOrWhiteSpace(textSegment))
                return result;

            // 按双换行符分割段落
            var paragraphs = textSegment.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var paragraph in paragraphs)
            {
                string trimmed = paragraph.Trim();
                if (string.IsNullOrEmpty(trimmed))
                    continue;

                var displayData = ParseSingleDialogue(trimmed);
                if (displayData != null)
                    result.Add(displayData);
            }

            return result;
        }

        /// <summary>
        /// 解析单条对话
        /// 支持格式：
        /// 1. "角色名：\n对话内容" 或 "角色名：对话内容"
        /// 2. "纯文本" (无角色名，作为旁白)
        /// 3. "[画面外]角色名：对话内容" - 有角色名但不显示立绘
        /// 4. "[立绘:表情名]角色名：对话内容" - 指定表情
        /// 5. "[隐藏立绘]" - 隐藏当前所有立绘
        /// </summary>
        private DialogueDisplayData ParseSingleDialogue(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var data = new DialogueDisplayData();

            // 检查特殊标记
            bool showSprite = true;        // 是否显示立绘
            string expressionName = "default";  // 表情名称
            
            // [画面外] 标记 - 有角色名但不显示立绘
            if (text.StartsWith("[画面外]"))
            {
                text = text.Substring(5).TrimStart();
                showSprite = false;
            }
            
            // [立绘:表情名] 标记 - 指定表情
            if (text.StartsWith("[立绘:") && text.Contains("]"))
            {
                int endIndex = text.IndexOf(']');
                string expressionTag = text.Substring(4, endIndex - 4); // 提取表情名
                expressionName = expressionTag.Trim();
                text = text.Substring(endIndex + 1).TrimStart();
            }
            
            // [隐藏立绘] 标记 - 清除所有立绘
            if (text.StartsWith("[隐藏立绘]"))
            {
                data.Character = null;
                data.Text = text.Substring(6).TrimStart();
                return data;
            }

            // 查找第一个冒号（中文或英文）
            int colonIndex = text.IndexOfAny(new[] { '：', ':' });
            
            if (colonIndex > 0)
            {
                // 提取角色名（冒号前的内容）
                string characterName = text.Substring(0, colonIndex).Trim();
                
                // 提取对话内容（冒号后的内容，去掉开头的换行符）
                string dialogueText = text.Substring(colonIndex + 1).TrimStart('\n', '\r', ' ');
                
                // 填充数据
                data.Text = dialogueText;
                
                // 根据 showSprite 决定是否创建立绘信息
                if (showSprite)
                {
                    data.Character = new CharacterDisplayInfo
                    {
                        Name = characterName,
                        Id = characterName.ToLower().Replace(" ", "_"),
                        SpriteResourcePath = GetCharacterSpritePath(characterName, expressionName),
                        Position = CharacterPosition.Center,
                        IsVisible = true,
                        Alpha = 1.0f,
                        Scale = 1.0f
                    };
                }
                else
                {
                    // 有角色名但不显示立绘（画面外旁白）
                    data.Character = new CharacterDisplayInfo
                    {
                        Name = characterName,
                        Id = characterName.ToLower().Replace(" ", "_")
                    };
                }
            }
            else
            {
                // 没有冒号，作为旁白处理
                data.Character = null;
                data.Text = text;
            }

            return data;
        }

        /// <summary>
        /// 根据角色名和表情获取立绘资源路径
        /// 直接使用角色名作为文件夹名，支持任意剧本的任意角色
        /// 例如：角色名"安·李"，表情"happy" → "Characters/安·李/happy"
        /// </summary>
        private string GetCharacterSpritePath(string characterName, string expressionName = "default")
        {
            // 直接使用角色名，不做任何映射
            // 这样不同剧本只需要准备对应的资源文件夹即可
            return $"Characters/{characterName}/{expressionName}";
        }

        /// <summary>
        /// 处理后端生成的选项 (future-ready)
        /// </summary>
        public void ProcessChoices(List<(string id, string displayText)> choicesFromBackend, string targetPhaseIdTemplate = "")
        {
            if (choicesFromBackend == null || choicesFromBackend.Count == 0)
            {
                Debug.LogWarning("[DialogueLogicAdapter] 接收到空选项列表");
                return;
            }

            try
            {
                if (debugLogging)
                    Debug.Log($"[DialogueLogicAdapter] 处理选项 ({choicesFromBackend.Count} 项)");

                var choices = new List<DialogueChoice>();
                for (int i = 0; i < choicesFromBackend.Count; i++)
                {
                    var (id, displayText) = choicesFromBackend[i];
                    var choice = new DialogueChoice
                    {
                        Id = id,
                        DisplayText = displayText,
                        TargetPhaseId = targetPhaseIdTemplate, // 实际版本会从NodeData提取
                        Priority = choicesFromBackend.Count - i // 保持顺序
                    };
                    choices.Add(choice);
                }

                FrontendDialogueEventBus.RaiseRequestChoicesDisplay(choices);

                if (debugLogging)
                    Debug.Log($"[DialogueLogicAdapter] 选项处理完成");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DialogueLogicAdapter] 处理选项时出错: {ex.Message}\n{ex.StackTrace}");
            }
        }



        /// <summary>
        /// 清除对话显示 (过渡/退出时)
        /// </summary>
        public void ClearDialogue()
        {
            if (debugLogging)
                Debug.Log("[DialogueLogicAdapter] 请求清除对话UI");
            FrontendDialogueEventBus.RaiseRequestDialogueClear();
        }
    }
}
