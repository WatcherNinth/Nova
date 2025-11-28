using System.Collections.Generic;
using Newtonsoft.Json;
using LogicEngine.Validation;

namespace LogicEngine.LevelGraph
{
    public class EntityItem : IValidatable
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("prompt")]
        public string Prompt { get; set; }

        [JsonProperty("alias")]
        public List<string> Alias { get; set; } = new List<string>();

        public void OnValidate(ValidationContext context)
        {
            // 校验逻辑待实现
        }
    }

    public class EntityListData : IValidatable
    {
        // 核心数据：<KeywordID, EntityInfo>
        public Dictionary<string, EntityItem> Data { get; set; } 
            = new Dictionary<string, EntityItem>();

        public void OnValidate(ValidationContext context)
        {
            // 校验逻辑待实现
        }
    }
}