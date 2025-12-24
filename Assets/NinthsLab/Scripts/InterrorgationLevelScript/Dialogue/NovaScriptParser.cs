using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace DialogueSystem
{
    public static class NovaScriptParser
    {
        private static readonly Regex LazyBlockRegex = new Regex(@"^<\|\s*(.+)\s*\|>$");
        private static readonly Regex EagerBlockRegex = new Regex(@"^@<\|\s*(.+)\s*\|>$");
        private static readonly Regex CommandFuncRegex = new Regex(@"^(\w+)\s*\((.*)\)$");

        public static DialogueBatch ParseBatch(List<string> rawInputList)
        {
            var batch = new DialogueBatch();
            DialogueEntry currentEntry = null;

            // =========================================================
            // 1. [新增] 预处理：扁平化 (Flatten)
            // 后端可能会发来包含 \n 的长字符串，我们需要先把它炸开成逐行的 List
            // =========================================================
            List<string> processedLines = new List<string>();
            
            if (rawInputList != null)
            {
                foreach (var rawBlock in rawInputList)
                {
                    if (string.IsNullOrEmpty(rawBlock))
                    {
                        processedLines.Add(""); // 显式的空行保留
                        continue;
                    }

                    // 按换行符切割 (兼容 Windows \r\n 和 Unix \n)
                    string[] subLines = rawBlock.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);
                    
                    processedLines.AddRange(subLines);
                }
            }

            // =========================================================
            // 2. 逐行解析 (逻辑保持不变，但遍历源变成了 processedLines)
            // =========================================================

            DialogueEntry GetOrCreateEntry()
            {
                if (currentEntry == null) currentEntry = new DialogueEntry();
                return currentEntry;
            }

            void CommitCurrentEntry()
            {
                if (currentEntry != null)
                {
                    if (!string.IsNullOrEmpty(currentEntry.Content) || currentEntry.Commands.Count > 0)
                    {
                        batch.Enqueue(currentEntry);
                    }
                    currentEntry = null;
                }
            }

            foreach (var line in processedLines) // <--- 改为遍历预处理后的列表
            {
                string trimmed = line.Trim();

                // --- 空行处理 (切换回合的核心) ---
                if (string.IsNullOrEmpty(trimmed))
                {
                    // 遇到空行（包括 \n\n 切割出来的空字符串），提交当前对话，这就实现了切分
                    CommitCurrentEntry();
                    continue;
                }

                // --- 提前执行块 ---
                Match eagerMatch = EagerBlockRegex.Match(trimmed);
                if (eagerMatch.Success)
                {
                    string content = eagerMatch.Groups[1].Value.Trim();
                    batch.EagerCommands.Add(ParseCommand(content));
                    continue;
                }

                // --- 延迟代码块 ---
                Match cmdMatch = LazyBlockRegex.Match(trimmed);
                if (cmdMatch.Success)
                {
                    string content = cmdMatch.Groups[1].Value.Trim();
                    GetOrCreateEntry().Commands.Add(ParseCommand(content));
                    continue;
                }

                // --- 文本行处理 ---
                var entry = GetOrCreateEntry();

                // 如果同一条 Entry 有多行文本，手动补回换行符
                if (!string.IsNullOrEmpty(entry.Content))
                {
                    entry.Content += "\n";
                }

                // 检查 :: 分隔符
                int sepIndex = trimmed.IndexOf("::");
                if (sepIndex < 0) sepIndex = trimmed.IndexOf("：：");

                if (sepIndex >= 0)
                {
                    string namePart = trimmed.Substring(0, sepIndex).Trim();
                    string textPart = trimmed.Substring(sepIndex + 2).Trim();
                    
                    int slashIndex = namePart.IndexOf("//");
                    if (slashIndex >= 0)
                    {
                        entry.DisplayName = namePart.Substring(0, slashIndex);
                        entry.CharacterId = namePart.Substring(slashIndex + 2);
                    }
                    else
                    {
                        // 如果之前已经是同一个人说话(多行合并)，保留原名；否则覆盖
                        if (string.IsNullOrEmpty(entry.DisplayName))
                        {
                            entry.DisplayName = namePart;
                            entry.CharacterId = namePart; 
                        }
                    }
                    
                    entry.Content += textPart;
                }
                else
                {
                    // 纯文本行 (追加到上一行，或者旁白)
                    entry.Content += trimmed;
                }
            }

            CommitCurrentEntry(); // 提交最后一条

            return batch;
        }

        // ... (ParseCommand 方法保持不变) ...
        private static ScriptCommand ParseCommand(string cmdStr)
        {
            Match match = CommandFuncRegex.Match(cmdStr);
            if (match.Success)
            {
                string funcName = match.Groups[1].Value;
                string argsRaw = match.Groups[2].Value;
                string[] args = argsRaw.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < args.Length; i++) args[i] = args[i].Trim().Trim('\'', '"');

                CommandType type = CommandType.Unknown;
                if (funcName == "show") type = CommandType.Show;
                else if (funcName == "hide") type = CommandType.Hide;

                return new ScriptCommand(type, args);
            }
            return new ScriptCommand(CommandType.Unknown);
        }
    }
}