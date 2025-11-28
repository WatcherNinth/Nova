using System.Collections.Generic;
using System.Linq;


namespace LogicEngine
{
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