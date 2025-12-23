using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace DialogueSystem
{
    public static class NovaScriptParser
    {
        // 匹配 <| command('arg1', 'arg2') |>
        // 这是一个简化正则，假设指令在一行内
        private static readonly Regex LazyBlockRegex = new Regex(@"^<\|\s*(.+)\s*\|>$");
        
        // 匹配指令函数名和参数: show('A', 'B')
        private static readonly Regex CommandFuncRegex = new Regex(@"^(\w+)\s*\((.*)\)$");

        public static DialogueBatch ParseBatch(List<string> rawLines)
        {
            var batch = new DialogueBatch();
            DialogueEntry currentEntry = null;

            foreach (var line in rawLines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                // 1. 检查是否是指令行 <| ... |>
                Match cmdMatch = LazyBlockRegex.Match(trimmed);
                if (cmdMatch.Success)
                {
                    string content = cmdMatch.Groups[1].Value.Trim();
                    var cmd = ParseCommand(content);
                    
                    // 如果当前还没有 Entry，或者当前 Entry 已经有文本了(意味着这是下一句的指令)，则新建 Entry
                    if (currentEntry == null || !string.IsNullOrEmpty(currentEntry.Content))
                    {
                        currentEntry = new DialogueEntry();
                        batch.Enqueue(currentEntry);
                    }
                    currentEntry.Commands.Add(cmd);
                    continue;
                }
                
                // 2. 检查是否是特殊事件标记 (后端传来的 [EVENT:...])
                if (trimmed.StartsWith("[EVENT:"))
                {
                    // 这里可以处理特殊逻辑，或者视为一条特殊的指令
                    // 暂时忽略或作为特殊指令处理
                    continue; 
                }

                // 3. 解析文本行 (Name::Content)
                // 如果当前没有 Entry，新建一个
                if (currentEntry == null)
                {
                    currentEntry = new DialogueEntry();
                    batch.Enqueue(currentEntry);
                }

                // 检查 :: 分隔符
                int sepIndex = trimmed.IndexOf("::");
                if (sepIndex >= 0)
                {
                    string namePart = trimmed.Substring(0, sepIndex);
                    string textPart = trimmed.Substring(sepIndex + 2);
                    
                    // 处理 Name//InternalName
                    int slashIndex = namePart.IndexOf("//");
                    if (slashIndex >= 0)
                    {
                        currentEntry.DisplayName = namePart.Substring(0, slashIndex);
                        currentEntry.CharacterId = namePart.Substring(slashIndex + 2);
                    }
                    else
                    {
                        currentEntry.DisplayName = namePart;
                        currentEntry.CharacterId = namePart; // 默认 ID = Name
                    }
                    currentEntry.Content = textPart;
                }
                else
                {
                    // 旁白或无名字对话
                    currentEntry.Content = trimmed;
                }
            }

            return batch;
        }

        private static ScriptCommand ParseCommand(string cmdStr)
        {
            // 简单解析: show('id', 'variant')
            // 实际项目中建议用更健壮的 Split 逻辑处理引号
            Match match = CommandFuncRegex.Match(cmdStr);
            if (match.Success)
            {
                string funcName = match.Groups[1].Value;
                string argsRaw = match.Groups[2].Value;
                
                // 简单的参数分割，去除引号
                string[] args = argsRaw.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < args.Length; i++)
                {
                    args[i] = args[i].Trim().Trim('\'', '"');
                }

                CommandType type = CommandType.Unknown;
                if (funcName == "show") type = CommandType.Show;
                else if (funcName == "hide") type = CommandType.Hide;

                return new ScriptCommand(type, args);
            }
            return new ScriptCommand(CommandType.Unknown);
        }
    }
}