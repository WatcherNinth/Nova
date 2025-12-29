using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace DialogueSystem
{
    public static class NovaScriptParser
    {
        private static readonly Regex LazyBlockRegex = new Regex(@"^<\|\s*(.+)\s*\|>$");
        private static readonly Regex EagerBlockRegex = new Regex(@"^@<\|\s*(.+)\s*\|>$");
        
        // 匹配 funcName(args...) 
        // 这里的 (.*) 会捕获括号内的所有内容，包括嵌套的括号
        private static readonly Regex CommandFuncRegex = new Regex(@"^(\w+)\s*\((.*)\)$");

        public static DialogueBatch ParseBatch(List<string> rawInputList)
        {
            var batch = new DialogueBatch();
            DialogueEntry currentEntry = null;

            // 1. 预处理：扁平化
            List<string> processedLines = new List<string>();
            if (rawInputList != null)
            {
                foreach (var rawBlock in rawInputList)
                {
                    if (string.IsNullOrEmpty(rawBlock))
                    {
                        processedLines.Add(""); 
                        continue;
                    }
                    string[] subLines = rawBlock.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);
                    processedLines.AddRange(subLines);
                }
            }

            // 2. 逐行解析
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

            foreach (var line in processedLines)
            {
                string trimmed = line.Trim();

                // 空行处理
                if (string.IsNullOrEmpty(trimmed))
                {
                    CommitCurrentEntry();
                    continue;
                }

                // 提前执行块
                Match eagerMatch = EagerBlockRegex.Match(trimmed);
                if (eagerMatch.Success)
                {
                    string content = eagerMatch.Groups[1].Value.Trim();
                    batch.EagerCommands.Add(ParseCommand(content));
                    continue;
                }

                // 延迟代码块
                Match cmdMatch = LazyBlockRegex.Match(trimmed);
                if (cmdMatch.Success)
                {
                    string content = cmdMatch.Groups[1].Value.Trim();
                    GetOrCreateEntry().Commands.Add(ParseCommand(content));
                    continue;
                }

                // 文本行处理
                var entry = GetOrCreateEntry();
                if (!string.IsNullOrEmpty(entry.Content)) entry.Content += "\n";

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
                    entry.Content += trimmed;
                }
            }

            CommitCurrentEntry();
            return batch;
        }

        // =========================================================
        // [核心修改] 指令解析逻辑
        // =========================================================
        private static ScriptCommand ParseCommand(string cmdStr)
        {
            Match match = CommandFuncRegex.Match(cmdStr);
            if (match.Success)
            {
                string funcName = match.Groups[1].Value;
                string argsRaw = match.Groups[2].Value; // 例如: AnQiao, Smile, {0,0,0}
                
                // 1. 使用智能分割，支持嵌套结构
                List<string> argList = SplitArgs(argsRaw);
                string[] args = new string[argList.Count];

                // 2. 清理参数
                for (int i = 0; i < argList.Count; i++)
                {
                    // 只去除首尾空格，不再去除引号
                    // 输入:  AnQiao  -> 输出: AnQiao
                    // 输入: {0,0,0}  -> 输出: {0,0,0}
                    args[i] = argList[i].Trim();
                }

                CommandType type = CommandType.Unknown;
                string lowerFunc = funcName.ToLower();
                if (lowerFunc == "show") type = CommandType.Show;
                else if (lowerFunc == "hide") type = CommandType.Hide;

                return new ScriptCommand(type, args);
            }
            
            // 简单的容错处理：如果正则没匹配上（可能有多余空格等），视为空指令
            return new ScriptCommand(CommandType.Unknown);
        }

        /// <summary>
        /// 智能参数分割器
        /// 能够正确处理大括号 {} 和小括号 () 的嵌套，避免被逗号错误切分
        /// </summary>
        private static List<string> SplitArgs(string rawArgs)
        {
            List<string> list = new List<string>();
            StringBuilder currentArg = new StringBuilder();
            int depth = 0; // 嵌套深度

            foreach (char c in rawArgs)
            {
                if (c == '{' || c == '(') depth++;
                if (c == '}' || c == ')') depth--;

                // 只有当深度为0时，逗号才作为参数分隔符
                if (c == ',' && depth == 0)
                {
                    list.Add(currentArg.ToString());
                    currentArg.Clear();
                }
                else
                {
                    currentArg.Append(c);
                }
            }

            // 添加最后一个参数
            if (currentArg.Length > 0)
            {
                list.Add(currentArg.ToString());
            }

            return list;
        }
    }
}