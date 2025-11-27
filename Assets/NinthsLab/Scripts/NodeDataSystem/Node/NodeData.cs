using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using LogicEngine.Templates;
using LogicEngine.Nodes;

namespace LogicEngine
{
    /// <summary>
    /// 论点节点的主数据类，对应 json 中的 "test_node" 这一层级
    /// </summary>
    public class NodeData
    {
        // 节点的唯一ID (json的Key)
        public string Id { get; set; }

        // 基础信息：描述、实体关联
        public NodeBasicInfo Basic { get; set; }

        // AI信息：Prompt、输入样例
        public NodeAIInfo AI { get; set; }

        // 前置条件与逻辑：互斥、依赖、自动验证、错误标记
        public NodeLogicInfo Logic { get; set; }

        // 模板：填空模板数据
        public NodeTemplateInfo Template { get; set; }

        // 对话：各个状态下的对话配置
        public NodeDialogueInfo Dialogue { get; set; }
    }
}

namespace LogicEngine.Nodes
{
    /// <summary>
    /// 1. 基础信息区块
    /// 包含描述和用于搜索/索引的实体标签
    /// </summary>
    public class NodeBasicInfo
    {
        // 对应 "description"
        public string Description { get; set; }
        
        // 对应 "is_wrong" (默认为 false)
        public bool IsWrong { get; set; } = false;
    }

    /// <summary>
    /// 2. AI信息区块
    /// 包含传递给LLM的提示词和样例
    /// </summary>
    public class NodeAIInfo
    {
        // 对应 "prompt"
        public string Prompt { get; set; }
        
        // 对应 "entities"
        // 如果json中不存在或为空，这里初始化为空列表
        public List<string> Entities { get; set; } = new List<string>();

        // 对应 "extra_input_sample"
        public List<string> ExtraInputSamples { get; set; } = new List<string>();
    }

    /// <summary>
    /// 3. 前置条件区块 (Logic)
    /// 包含互斥逻辑、依赖树(depends_on)以及论点自身的属性标记
    /// </summary>
    public class NodeLogicInfo
    {
        // 对应 "mutex_group"
        public string MutexGroup { get; set; }

        // 对应 "extra_mutex_list"
        public List<string> ExtraMutexList { get; set; } = new List<string>();

        // 对应 "override_mutex_trigger"
        // 这是一个条件模块，暂存为JToken，运行时解析
        public JToken OverrideMutexTrigger { get; set; }

        // 对应 "depends_on"
        // 这是一个复杂的嵌套逻辑树，按照要求仅存储JToken，不进行解析
        public JToken DependsOn { get; set; }

        // 对应 "is_auto_verified" (默认为 false)
        public bool IsAutoVerified { get; set; } = false;
    }

    /// <summary>
    /// 4. 模板区块
    /// 处理填空题相关的逻辑
    /// </summary>
    public class NodeTemplateInfo
    {
        // 对应 "special_node_template"
        // 如果存在，通过ID引用外部模板
        public string SpecialTemplateId { get; set; }

        // 对应 "template"
        // 这是一个被处理过的数据对象。
        // 在读取时会调用 TemplateParser.Parse(json) 赋值给此属性
        public TemplateData Template { get; set; }
    }

    /// <summary>
    /// 5. 对话区块
    /// 包含论点在不同状态下触发的对话配置
    /// </summary>
    public class NodeDialogueInfo
    {
        // 对应 "dialogue" -> "on_proven"
        // 内部包含 text_1, first_valid_1, call_choice_group_1 等复杂结构
        // 存储为 JToken 以便交给通用的 DialogueParser 处理
        public JToken OnProven { get; set; }

        // 对应 "dialogue" -> "on_pending"
        public JToken OnPending { get; set; }

        // 对应 "dialogue" -> "on_mutex"
        public JToken OnMutex { get; set; }
    }
}