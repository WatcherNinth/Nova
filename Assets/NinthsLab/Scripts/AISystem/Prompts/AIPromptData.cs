using System;

namespace AIEngine.Prompts
{
    /// <summary>
    /// 存储构建好的提示词各部分组件，尚未组合成最终的 ChatMessage
    /// </summary>
    [Serializable]
    public class AIPromptData
    {
        /// <summary>
        /// 系统指令：包含身份定义、核心工作原则、输出格式要求 (对应 llm_service 中的模板)
        /// </summary>
        public string SystemInstruction;

        /// <summary>
        /// 动态生成的上下文：包含当前阶段的逻辑节点列表、实体列表
        /// </summary>
        public string DynamicContext;

        /// <summary>
        /// 玩家的原始输入
        /// </summary>
        public string UserInput;

        /// <summary>
        /// 获取完整的 System Message 内容 (指令 + 上下文)
        /// </summary>
        public string FullSystemMessage => $"{SystemInstruction}\n\n---\n{DynamicContext}";
    }
}