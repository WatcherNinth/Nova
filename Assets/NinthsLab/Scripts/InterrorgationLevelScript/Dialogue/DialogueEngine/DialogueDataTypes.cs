using System;
using System.Collections.Generic;

namespace DialogueSystem
{
    // =========================================================
    // 1. 指令结构
    // =========================================================

    /// <summary>
    /// 支持的脚本指令类型
    /// </summary>
    public enum CommandType
    {
        Unknown = 0,
        Show,   // 显示或切换差分：<| show('ID', 'Variant') |>
        Hide    // 关闭立绘：<| hide('ID') |>
    }

    /// <summary>
    /// 代表单条解析出的指令
    /// 对应 <| ... |> 中的一个函数调用
    /// </summary>
    public struct ScriptCommand
    {
        public CommandType Type;
        public string[] Args; // 参数列表

        public ScriptCommand(CommandType type, params string[] args)
        {
            Type = type;
            Args = args;
        }
    }

    // =========================================================
    // 2. 对话条目结构
    // =========================================================

    /// <summary>
    /// 解析后的单条对话/演出数据
    /// 对应后端发来的一行或多行文本解析后的结果
    /// </summary>
    public class DialogueEntry
    {
        /// <summary>
        /// 角色内部ID (用于查找立绘/语音)，对应 // 左边的值或直接ID
        /// </summary>
        public string CharacterId { get; set; }

        /// <summary>
        /// 界面显示的角色名字，对应 :: 左边的值
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 实际对话内容，对应 :: 右边的值
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 伴随这条对话的指令列表 (解析 <| ... |> 得到)
        /// </summary>
        public List<ScriptCommand> Commands { get; set; } = new List<ScriptCommand>();

        /// <summary>
        /// 是否是纯指令节点（没有文本，只有演出，通常应自动跳过等待点击）
        /// </summary>
        public bool IsPureCommand => string.IsNullOrEmpty(Content) && Commands.Count > 0;

        /// <summary>
        /// 原始文本（用于调试）
        /// </summary>
        public string RawOriginText { get; set; }
    }
    
    /// <summary>
    /// 代表一批对话数据（对应后端发来的一整个 List<string>）
    /// </summary>
    public class DialogueBatch
    {
        // 这一批里的所有对话条目
        public Queue<DialogueEntry> Entries { get; private set; } = new Queue<DialogueEntry>();
        
        // 提前执行指令（对应 @<| ... |>），在开始播放 Entries 前执行
        public List<ScriptCommand> EagerCommands { get; set; } = new List<ScriptCommand>();

        public void Enqueue(DialogueEntry entry) => Entries.Enqueue(entry);
        public DialogueEntry Dequeue() => Entries.Count > 0 ? Entries.Dequeue() : null;
        public bool HasNext => Entries.Count > 0;
    }
}