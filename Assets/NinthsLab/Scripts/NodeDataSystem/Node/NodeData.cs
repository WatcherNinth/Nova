using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using LogicEngine.Validation;
using LogicEngine.Nodes;

namespace LogicEngine
{
    // 1. 根节点实现接口
    public class NodeData : IValidatable
    {
        public string Id { get; set; }
        public NodeBasicInfo Basic { get; set; }
        public NodeAIInfo AI { get; set; }
        public NodeLogicInfo Logic { get; set; }
        public NodeTemplateInfo Template { get; set; }
        public NodeDialogueInfo Dialogue { get; set; }

        public void OnValidate(ValidationContext context)
        {
            // --- 自身属性检查 ---
            if (string.IsNullOrEmpty(Id))
                context.LogError("节点 ID (Id) 不能为空。");

            // --- 跨模块逻辑检查 (Cross-Module Validation) ---
            CheckLogicAndDialogueConsistency(context);

            // --- 递归检查子模块 ---
            context.ValidateChild("BasicModule", Basic);
            context.ValidateChild("AIModule", AI);
            context.ValidateChild("LogicModule", Logic);
            // Template 允许为空 (使用开关)
            context.ValidateChild("TemplateModule", Template, allowNull: true);
            context.ValidateChild("DialogueModule", Dialogue);
        }

        /// <summary>
        /// 专门处理 Logic 和 Dialogue 之间的关联性检查
        /// </summary>
        private void CheckLogicAndDialogueConsistency(ValidationContext context)
        {
            // 1. 判断是否有前置条件 (DependsOn)
            // JToken 判定不为空且有内容
            bool hasDependsOn = Logic != null && Logic.DependsOn != null && Logic.DependsOn.HasValues;

            // 2. 判断是否有 Pending 对话 (OnPending)
            bool hasPendingDialogue = Dialogue != null && Dialogue.OnPending != null && Dialogue.OnPending.HasValues;

            // 3. 执行判定规则
            if (!hasDependsOn && hasPendingDialogue)
            {
                // 没逻辑，却有等待对话 -> 永远不会触发
                context.LogWarning("检测到配置了 'on_pending' 对话，但 'depends_on' 前置条件为空。该对话永远不会被触发。");
            }
            else if (hasDependsOn && !hasPendingDialogue)
            {
                // 有逻辑，却没等待对话 -> 玩家等待时没反馈
                context.LogWarning("检测到配置了 'depends_on' 前置条件，但缺少 'on_pending' 对话。建议添加以给予玩家等待反馈。");
            }
        }

        public JObject GetAIContent()
        {
            // 构建内容部分
            var content = new JObject
            {
                // 从 Basic 获取描述
                ["description"] = Basic?.Description,

                // 从 AI 获取属性 (带空值保护)
                ["prompt"] = AI?.Prompt,
                ["entities"] = AI?.Entities != null ? JToken.FromObject(AI.Entities) : new JArray(),
                ["extra_input_sample"] = AI?.ExtraInputSamples != null ? JToken.FromObject(AI.ExtraInputSamples) : new JArray()
            };

            // 构建根对象，以 Id 为 Key
            var root = new JObject
            {
                [Id] = content
            };

            return root;
        }
    }
}

namespace LogicEngine.Nodes
{
    // 2. 基础信息自检
    public class NodeBasicInfo : IValidatable
    {
        public string Description { get; set; }
        public bool IsWrong { get; set; } = false;

        public void OnValidate(ValidationContext context)
        {
            if (string.IsNullOrWhiteSpace(Description))
                context.LogError("必须填写描述 (description)。");
        }
    }

    // 3. AI 信息自检
    public class NodeAIInfo : IValidatable
    {
        public string Prompt { get; set; }
        public List<string> Entities { get; set; } = new List<string>();
        public List<string> ExtraInputSamples { get; set; } = new List<string>();

        public void OnValidate(ValidationContext context)
        {

        }
    }

    // 4. 逻辑层自检
    public class NodeLogicInfo : IValidatable
    {
        public string MutexGroup { get; set; }
        public List<string> ExtraMutexList { get; set; }
        public JToken OverrideMutexTrigger { get; set; }
        public JToken DependsOn { get; set; }
        public bool IsAutoVerified { get; set; }

        public void OnValidate(ValidationContext context)
        {
            // 逻辑检查：互斥组不能包含自己
            if (!string.IsNullOrEmpty(MutexGroup) && ExtraMutexList != null && ExtraMutexList.Contains(MutexGroup))
            {
                context.LogError($"互斥组名 '{MutexGroup}' 不应同时出现在 'extra_mutex_list' 中。");
            }

            if (OverrideMutexTrigger != null)
            {
                if (!string.IsNullOrEmpty(MutexGroup) || ExtraMutexList != null)
                {
                    context.LogWarning("检测到OverrideMutexTrigger跟MutexGroup或ExtraMutexList同时存在，它会*完全*覆盖后两者的设置。");
                }
            }
            // 自动验证检查
            if (IsAutoVerified && (DependsOn == null || !DependsOn.HasValues))
            {
                context.LogError("设置为自动验证 (is_auto_verified: true) 的节点必须包含 'depends_on' 逻辑，否则无法判定何时验证。");
            }

            // 条件依赖检查
            using (context.Scope("depend_on"))
            {
                ConditionEvaluator.Validate(DependsOn.ToString(), context);
            }
        }

        public bool GetDependOnResult()
        {
            return ConditionEvaluator.Evaluate(DependsOn.ToString());
        }
    }

    // 5. 模板层自检
    public class NodeTemplateInfo : IValidatable
    {
        public string SpecialTemplateId { get; set; }
        public TemplateData Template { get; set; }

        public void OnValidate(ValidationContext context)
        {
            // 歧义检查
            if (!string.IsNullOrEmpty(SpecialTemplateId) && Template != null)
            {
                context.LogWarning("存在歧义：同时检测到 'special_node_template' 和 'template' 数据。程序将优先使用 'special_node_template'。");
            }

            if (Template is IValidatable validatableData)
            {
                context.ValidateChild("TemplateData", validatableData);
            }

            if (!string.IsNullOrEmpty(SpecialTemplateId))
            {
                if (!LevelGraphContext.CurrentGraph.allTemplates.ContainsKey(SpecialTemplateId))
                {
                    context.LogError($"special_node_template: {SpecialTemplateId}模板未找到。请检查模板ID是否正确。");
                }
            }
        }
    }

    // 6. 对话层自检
    public class NodeDialogueInfo : IValidatable
    {
        public JToken OnProven { get; set; }
        public JToken OnPending { get; set; }
        public JToken OnMutex { get; set; }

        public void OnValidate(ValidationContext context)
        {
            // 必须要有 Proven 对应的对话
            if (OnProven == null || !OnProven.HasValues)
            {
                context.LogError("缺少必要的 'on_proven' (论证成功) 对话配置。");
            }
            if (OnProven != null)
            {
                validateDialogueByKey(OnProven, "on_proven", context);
            }
            if (OnPending != null)
            {
                validateDialogueByKey(OnPending, "on_pending", context);
            }
            if (OnMutex != null)
            {
                validateDialogueByKey(OnProven, "on_mutex", context);
            }
        }
        private void validateDialogueByKey(JToken dialogue, string scope, ValidationContext context)
        {
            using (context.Scope(scope))
            {
                DialogueParser.ValidateDialogue(dialogue, context);
            }
        }
    }
}