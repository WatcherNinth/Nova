using System.Collections.Generic;
using Newtonsoft.Json;
using LogicEngine.Validation;

namespace LogicEngine.LevelGraph
{
    public class EntityItem : IValidatable
    {
        private string _name;

        [JsonProperty("name")]
        public string Name { 
            get
            {
                // TODO：因为玩家可以手动输入实体名称，这里注意之后对接本地化。应该可以换成一个新的类？
                return _name;
            }
            set
            {
                _name = value;
            } 
        }

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