using System.Collections.Generic;

namespace FrontendEngine.Dialogue.Models
{
    /// <summary>
    /// 对话选项数据模型 (纯数据, 无逻辑)
    /// 职责: 封装用户可选的选项
    /// 来源: DialogueLogicAdapter 从后端选项数据转换
    /// 用途: ChoiceButtonGroup 使用此数据渲染选项按钮
    /// </summary>
    public class DialogueChoice
    {
        /// <summary>
        /// 选项唯一标识符 (用于追踪用户选择)
        /// </summary>
        public string Id { get; set; } = "";

        /// <summary>
        /// 选项显示文本
        /// </summary>
        public string DisplayText { get; set; } = "";

        /// <summary>
        /// 目标阶段ID (用户选择后应跳转到的阶段)
        /// 后端逻辑: 该值由 DialogueLogicAdapter 从 NodeData 的选项中提取
        /// </summary>
        public string TargetPhaseId { get; set; } = "";

        /// <summary>
        /// 是否为禁用状态 (灰显)
        /// </summary>
        public bool IsDisabled { get; set; } = false;

        /// <summary>
        /// 禁用原因 (显示在tooltip中)
        /// </summary>
        public string DisabledReason { get; set; } = "";

        /// <summary>
        /// 选项权重 (用于排序, 越大越靠前)
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// 其他元数据 (扩展用)
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        public override string ToString()
        {
            return $"Choice({Id}: {DisplayText}, Target={TargetPhaseId}, Disabled={IsDisabled})";
        }
    }
}
