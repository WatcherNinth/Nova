using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LogicEngine.Validation;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace LogicEngine.Templates
{
    /// <summary>
    /// 答案定义的运行时结构
    /// </summary>
    public class AnswerData
    {
        /// <summary>
        /// 答案指向的目标ID（论点ID或其他结果ID）
        /// </summary>
        public string TargetId { get; private set; }

        /// <summary>
        /// 触发该结果所需的正确输入序列
        /// </summary>
        public List<string> RequiredInputs { get; private set; }

        public AnswerData(string targetId, List<string> inputs)
        {
            TargetId = targetId;
            RequiredInputs = inputs;
        }
    }

    /// <summary>
    /// Template的运行时数据实体。
    /// 不包含MonoBehaviour，纯C#类。
    /// </summary>
    public class TemplateData: IValidatable
    {
        public enum e_TemplateConditionType
        {
            Hide,
            Discovered,
            Used
        }
        public e_TemplateConditionType TemplateCondition;
        /// <summary>
        /// 原始文本（包含{0}等占位符）
        /// </summary>
        public string RawText { get; private set; }

        /// <summary>
        /// 下拉菜单配置。
        /// Key: 占位符索引 (0对应{0}, 1对应{1})
        /// Value: 选项列表
        /// </summary>
        public Dictionary<int, List<string>> DropdownOptions { get; private set; }

        /// <summary>
        /// 所有可能的答案组合
        /// </summary>
        public List<AnswerData> Answers { get; private set; }

        public TemplateData()
        {
            DropdownOptions = new Dictionary<int, List<string>>();
            Answers = new List<AnswerData>();
        }

        public void SetText(string text) => RawText = text;
        
        public void AddDropdown(int index, List<string> options)
        {
            if (!DropdownOptions.ContainsKey(index))
            {
                DropdownOptions[index] = options;
            }
        }

        public void AddAnswer(AnswerData answer)
        {
            Answers.Add(answer);
        }

        public void OnValidate(ValidationContext context)
        {
            
        }
    }

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
                        continue;

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
    }

    /// <summary>
    /// 业务逻辑处理器。
    /// 负责处理用户的输入并校验结果。
    /// </summary>
    public static class TemplateLogic
    {
        /// <summary>
        /// 校验用户填写的答案。
        /// </summary>
        /// <param name="data">模板数据</param>
        /// <param name="userInputs">用户填入的字符串序列，需按空位顺序排列</param>
        /// <returns>匹配到的 TargetId，如果没有匹配则返回 null</returns>
        public static string CheckResult(TemplateData data, List<string> userInputs)
        {
            if (data == null || data.Answers == null || userInputs == null)
                return null;

            foreach (var answer in data.Answers)
            {
                if (IsMatch(answer.RequiredInputs, userInputs))
                {
                    return answer.TargetId;
                }
            }

            return null;
        }

        /// <summary>
        /// 比较两个列表内容是否一致
        /// </summary>
        private static bool IsMatch(List<string> required, List<string> input)
        {
            if (required.Count != input.Count) 
                return false;

            for (int i = 0; i < required.Count; i++)
            {
                // 这里进行简单的字符串匹配
                // 注意：dropdown里的文本和答案里的文本之后都会走本地化
                // 此时比较的是 Key 是否一致，所以直接用 String.Equals
                if (required[i] != input[i])
                    return false;
            }

            return true;
        }
        
        /// <summary>
        /// 获取某个空位的所有本地化后备选项（用于UI显示）
        /// </summary>
        public static List<string> GetLocalizedOptions(TemplateData data, int slotIndex)
        {
            if (data.DropdownOptions.TryGetValue(slotIndex, out List<string> keys))
            {
                // 将Key列表转换为本地化文本列表
                return keys.Select(k => LocaleHelper.GetText(k)).ToList();
            }
            return new List<string>(); // 如果该空位没有dropdown配置（可能是填空题或其他），返回空列表
        }
        
        /// <summary>
        /// 获取本地化后的模板主体文本
        /// </summary>
        public static string GetLocalizedBodyText(TemplateData data)
        {
            return LocaleHelper.GetText(data.RawText);
        }
    }
}