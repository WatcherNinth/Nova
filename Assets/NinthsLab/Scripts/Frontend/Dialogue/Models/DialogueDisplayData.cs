using System.Collections.Generic;
using UnityEngine;

namespace FrontendEngine.Dialogue.Models
{
    /// <summary>
    /// 对话显示数据模型 (纯数据, 无逻辑)
    /// 职责: 封装后端对话数据用于前端显示
    /// 来源: DialogueLogicAdapter 从后端 List&lt;string&gt; 转换
    /// 用途: DialogueUIPanel 用此数据驱动UI更新
    /// 数据安全: 不包含后端逻辑相关信息, 仅包含UI呈现所需信息
    /// </summary>
    public class DialogueDisplayData
    {
        /// <summary>
        /// 讲话角色信息
        /// </summary>
        public CharacterDisplayInfo Character { get; set; }

        /// <summary>
        /// 场景信息
        /// </summary>
        public SceneDisplayInfo Scene { get; set; }

        /// <summary>
        /// 对话文本
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// 对话特效列表 (如渐隐渐显, 抖动等)
        /// </summary>
        public List<DialogueEffect> Effects { get; set; }

        /// <summary>
        /// 是否为自动推进的对话 (不需要用户点击)
        /// </summary>
        public bool IsAutoAdvance { get; set; }

        /// <summary>
        /// 自动推进延迟 (秒)
        /// </summary>
        public float AutoAdvanceDelay { get; set; } = 0f;

        /// <summary>
        /// 原始对话行号 (用于日志追踪)
        /// </summary>
        public int SourceLineIndex { get; set; } = -1;

        public DialogueDisplayData()
        {
            Character = new CharacterDisplayInfo();
            Scene = new SceneDisplayInfo();
            Effects = new List<DialogueEffect>();
        }

        /// <summary>
        /// 用于调试的字符串表示
        /// </summary>
        public override string ToString()
        {
            return $"[Dialogue] {Character.Name}: {Text} (Line#{SourceLineIndex})";
        }
    }
}
