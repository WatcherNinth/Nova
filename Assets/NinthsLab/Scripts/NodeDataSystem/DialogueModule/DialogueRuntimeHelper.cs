using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace LogicEngine.LevelLogic
{
    public static class DialogueRuntimeHelper
    {
        /// <summary>
        /// 运行时对话生成器：调用 Parser 执行逻辑，并将结果转为 UI 可用的列表
        /// </summary>
        public static List<string> GenerateDialogueLines(JToken dialogueJsonToken)
        {
            List<string> lines = new List<string>();
            if (dialogueJsonToken == null) return lines;

            // 1. 将 JToken 转为 string，喂给 DialogueParser
            // DialogueParser.ParseDialogue 会执行 limit 检查和文本合并
            string jsonInput = dialogueJsonToken.ToString();
            string parsedJson = DialogueParser.ParseDialogue(jsonInput);

            // 2. 将返回的 JSON 字符串转回对象，提取内容
            JObject resultObj = JObject.Parse(parsedJson);

            // 3. 转换为 List<string> 给 UI
            foreach (var prop in resultObj.Properties())
            {
                if (prop.Name.StartsWith("text"))
                {
                    // DialogueParser 已经把换行符处理好了，直接加
                    lines.Add(prop.Value.ToString());
                }
                else if (prop.Name.StartsWith("call_choice_group"))
                {
                    // 特殊指令前缀，UI层解析
                    lines.Add($"[EVENT:CallChoiceGroup:{prop.Value}]");
                }
            }

            return lines;
        }
    }
}