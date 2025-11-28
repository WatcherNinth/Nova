using System.Collections.Generic;
using Newtonsoft.Json;
using LogicEngine.Validation;

namespace LogicEngine.LevelGraph
{
    public class NodeChoiceItem : IValidatable
    {
        [JsonProperty("text_override")]
        public string TextOverride { get; set; }

        [JsonProperty("is_hidden")]
        public bool IsHidden { get; set; } = false;

        [JsonProperty("display_anyway")]
        public bool DisplayAnyway { get; set; } = false;

        public void OnValidate(ValidationContext context)
        {
            // 校验逻辑待实现
        }
    }

    public class NodeChoiceGroupData : IValidatable
    {
        // 核心数据：<GroupID, <ArgID, OptionInfo>>
        public Dictionary<string, Dictionary<string, NodeChoiceItem>> Data { get; set; } 
            = new Dictionary<string, Dictionary<string, NodeChoiceItem>>();

        public void OnValidate(ValidationContext context)
        {
            // 校验逻辑待实现
        }
    }
}