using LogicEngine.Validation;
using System.Collections.Generic;
using LogicEngine.Templates;

namespace LogicEngine.Templates
{
    /// <summary>
    /// 答案定义的运行时结构
    /// </summary>
    public class AnswerData
    {
        /// <summary>
        /// 答案指向的目标ID（论点ID，已通过parser完成this的转换，所以不需要知道属于者是谁也可以直接用）
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
}

namespace LogicEngine
{
    /// <summary>
    /// Template的运行时数据实体。
    /// 不包含MonoBehaviour，纯C#类。
    /// </summary>
    public class TemplateData : IValidatable
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
            var graph = LevelGraphContext.CurrentGraph;
            foreach (var answer in Answers)
            {
                for (var i = 0; i < answer.RequiredInputs.Count; i++)
                {
                    if (DropdownOptions.ContainsKey(i))
                    {
                        if (!DropdownOptions[i].Contains(answer.RequiredInputs[i]))
                        {
                            context.LogError($"在{answer.TargetId}的答案中检测到无效的下拉选项值: {answer.RequiredInputs[i]}");
                        }
                    }
                    else
                    {
                        if (!graph.entityListData.Data.ContainsKey(answer.RequiredInputs[i]))
                        {
                            context.LogError($"在{answer.TargetId}的答案中检测到无效的关键词ID: {answer.RequiredInputs[i]}");
                        }
                    }
                }

                if (!graph.nodeLookup.ContainsKey(answer.TargetId))
                {
                    context.LogError($"{answer.TargetId}指向的论点不存在!");
                }
            }
        }
    }
}