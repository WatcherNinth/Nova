using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LogicEngine
{
    /// <summary>
    /// 对话解析核心逻辑类（静态）
    /// </summary>
    public static class DialogueParser
    {
        /// <summary>
        /// 主解析入口
        /// </summary>
        /// <param name="jsonInput">原始JSON字符串</param>
        /// <returns>解析后的JSON字符串</returns>
        public static string ParseDialogue(string jsonInput)
        {
            JObject root;
            try
            {
                root = JObject.Parse(jsonInput);
            }
            catch
            {
                return "{}";
            }

            // 规则2: 忽视对话头，直接取第一个有效的内容对象
            if (!root.HasValues) return "{}";
            
            JProperty firstProp = root.Properties().First();
            JObject dialogueContent = firstProp.Value as JObject;

            if (dialogueContent == null) return "{}";

            JObject resultObject = new JObject();
            StringBuilder textBuffer = new StringBuilder();

            // 规则4: 流程从上往下执行
            foreach (var property in dialogueContent.Properties())
            {
                string key = property.Name;
                JToken value = property.Value;
                bool shouldStop = false;

                // 路由分发
                if (key.StartsWith("text"))
                {
                    string txt = value.ToString();
                    AppendText(textBuffer, txt);
                }
                else if (key.StartsWith("first_valid")) 
                {
                    string txt = ResolveFirstValid(value);
                    AppendText(textBuffer, txt);
                }
                else if (key.StartsWith("triggered_text"))
                {
                    string txt = ResolveTriggeredText(value);
                    AppendText(textBuffer, txt);
                }
                else if (key.StartsWith("call_choice_group"))
                {
                    // 遇到选项组，先结算之前的文本，再处理选项，然后截断
                    FlushBufferToOutput(resultObject, textBuffer);
                    ResolveCallChoiceGroup(key, value, resultObject);
                    shouldStop = true;
                }

                if (shouldStop) break;
            }

            // 如果循环结束且没有被选项截断，确保缓冲区内的文本被写入
            FlushBufferToOutput(resultObject, textBuffer);

            return resultObject.ToString(Formatting.Indented);
        }

        // --------------------------------------------------------------------------
        // 功能模块实现
        // --------------------------------------------------------------------------

        /// <summary>
        /// 将解析到的片段加入缓冲区
        /// </summary>
        private static void AppendText(StringBuilder buffer, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            
            // 如果缓冲区不为空，添加换行符连接多段对话
            if (buffer.Length > 0)
            {
                buffer.Append("\n");
            }
            buffer.Append(text);
        }

        /// <summary>
        /// 将缓冲区内容写入结果JSON对象，并清空缓冲区
        /// </summary>
        private static void FlushBufferToOutput(JObject output, StringBuilder buffer)
        {
            if (buffer.Length == 0) return;

            // 规则3变体: 多个连续text写成一个text (key固定为text_1，因为只会输出一次合并后的文本)
            // 如果未来需要支持多段输出，可在这里增加计数器逻辑
            if (!output.ContainsKey("text_1"))
            {
                output.Add("text_1", buffer.ToString());
            }
            else 
            {
                // 防御性编码：理论上遇call_choice_group会截断，不会出现第二次Flush
                // 但如果逻辑变更，这里做追加处理
                string existing = output["text_1"].ToString();
                output["text_1"] = existing + "\n" + buffer.ToString();
            }
            
            buffer.Clear();
        }

        /// <summary>
        /// 处理 first_valid 逻辑：返回满足条件的文本字符串
        /// </summary>
        private static string ResolveFirstValid(JToken token)
        {
            JObject validGroup = token as JObject;
            if (validGroup == null) return null;

            // 遍历内部 triggered_text
            foreach (var prop in validGroup.Properties())
            {
                if (prop.Name.StartsWith("triggered_text"))
                {
                    if (CheckCondition(prop.Value))
                    {
                        return ExtractText(prop.Value);
                    }
                }
            }

            // Fallback
            if (validGroup.ContainsKey("fallback_text"))
            {
                return validGroup["fallback_text"]?.ToString();
            }

            return null;
        }

        /// <summary>
        /// 处理 triggered_text 逻辑：返回满足条件的文本字符串
        /// </summary>
        private static string ResolveTriggeredText(JToken token)
        {
            if (CheckCondition(token))
            {
                return ExtractText(token);
            }
            return null;
        }

        /// <summary>
        /// 处理 call_choice_group 逻辑：直接写入 Output
        /// </summary>
        private static void ResolveCallChoiceGroup(string originalKey, JToken token, JObject output)
        {
            string groupId = token.ToString();
            output.Add(originalKey, groupId);
        }

        // --------------------------------------------------------------------------
        // 辅助工具函数
        // --------------------------------------------------------------------------

        /// <summary>
        /// 提取并验证条件
        /// </summary>
        private static bool CheckCondition(JToken token)
        {
            JObject obj = token as JObject;
            // 如果不是对象，视为无条件通过
            if (obj == null) return true; 

            if (obj.ContainsKey("limit"))
            {
                JToken limitToken = obj["limit"];
                // 规则5: 字符串化整个limit object，并调用 ConditionEvaluator
                string limitStr = limitToken.ToString(Formatting.None);
                return ConditionEvaluator.Evaluate(limitStr);
            }

            return true;
        }

        /// <summary>
        /// 提取 text 字段
        /// </summary>
        private static string ExtractText(JToken token)
        {
            JObject obj = token as JObject;
            if (obj != null && obj.ContainsKey("text"))
            {
                return obj["text"]?.ToString();
            }
            return null;
        }
    }
}