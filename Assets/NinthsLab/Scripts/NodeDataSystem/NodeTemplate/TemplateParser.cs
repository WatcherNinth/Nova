using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace LogicEngine.Templates
{
    /// <summary>
    /// 静态工厂/解析器。
    /// 负责将复杂的JSON结构转换为规整的TemplateData。
    /// </summary>
    public static class TemplateParser
    {
        private const string KEY_TEXT = "text";
        private const string KEY_ANSWER_ROOT = "answer";
        private const string KEY_THIS = "this";
        private const string PREFIX_DROPDOWN = "dropdown_blank_";

        /// <summary>
        /// 解析Json并生成TemplateData
        /// </summary>
        /// <param name="templateToken">"template" 对应的 JObject</param>
        /// <param name="ownerId">隶属的论点ID。如果是SpecialTemplate，传null或空字符串</param>
        /// <returns>解析后的数据对象，如果数据无效返回null</returns>
        public static TemplateData Parse(JToken templateToken, string ownerId = null)
        {
            if (templateToken == null || templateToken.Type != JTokenType.Object)
                return null;

            JObject json = (JObject)templateToken;
            TemplateData result = new TemplateData();

            // 1. 解析文本
            if (json.TryGetValue(KEY_TEXT, out JToken textToken))
            {
                result.SetText(textToken.ToString());
            }

            // 2. 遍历所有Key寻找 dropdown_blank_X
            foreach (var property in json.Properties())
            {
                if (property.Name.StartsWith(PREFIX_DROPDOWN))
                {
                    // 提取数字 X
                    string indexStr = property.Name.Substring(PREFIX_DROPDOWN.Length);
                    if (int.TryParse(indexStr, out int logicIndex))
                    {
                        int codeIndex = logicIndex;

                        List<string> options = property.Value.ToObject<List<string>>();
                        result.AddDropdown(codeIndex, options);
                    }
                }
            }

            // 3. 解析答案
            if (json.TryGetValue(KEY_ANSWER_ROOT, out JToken answerRoot) && answerRoot.Type == JTokenType.Object)
            {
                JObject answerObj = (JObject)answerRoot;
                foreach (var prop in answerObj.Properties())
                {
                    string rawKey = prop.Name;
                    string targetId = ResolveTargetId(rawKey, ownerId, result.RawText);

                    // 如果解析不出TargetId（比如在SpecialTemplate用了this），则跳过或报错
                    if (string.IsNullOrEmpty(targetId))
                    {
                        //这个错在ResolveTargetId里报过了
                        continue;
                    }

                    List<string> inputs = prop.Value.ToObject<List<string>>();
                    result.AddAnswer(new AnswerData(targetId, inputs));
                }
            }

            return result;
        }

        /// <summary>
        /// 处理 answer 中的键值逻辑 (移除 _1, _2 后缀，处理 this)
        /// </summary>
        private static string ResolveTargetId(string rawKey, string ownerId, string rawText)
        {
            // 移除后缀 "_数字"
            // Regex: 匹配末尾的 _\d+
            string cleanKey = Regex.Replace(rawKey, @"_\d+$", "");

            if (cleanKey == KEY_THIS)
            {
                // 如果是 special template (ownerId is null)，则不允许使用 this
                if (string.IsNullOrEmpty(ownerId))
                {
                    // 这里可以选择记录日志
                    Debug.LogError($"检测到special template的答案中出现了this！这个模板的描述是：{rawText}");
                    return null;
                }
                return ownerId;
            }

            return cleanKey;
        }

        /// <summary>
        /// 解析一组 Special Node Templates。
        /// </summary>
        /// <param name="rootJson">包含多个特殊模板定义的 Json 对象，Key 为 TemplateID，Value 为 Template 结构</param>
        /// <returns>以 TemplateID 为键的字典</returns>
        public static Dictionary<string, TemplateData> ParseSpecialTemplates(JObject rootJson)
        {
            var result = new Dictionary<string, TemplateData>();

            if (rootJson == null)
                return result;

            foreach (var property in rootJson.Properties())
            {
                string templateId = property.Name;
                JToken templateToken = property.Value;

                // 调用现有的单个解析逻辑
                // ownerId 传入 null，因为 SpecialTemplate 不隶属于特定论点
                // 现有的 Parse 方法逻辑中，当 ownerId 为空时会自动忽略 'this' 关键字，符合需求预期
                TemplateData data = Parse(templateToken, null);

                if (data != null)
                {
                    result[templateId] = data;
                }
            }

            return result;
        }
    }
}