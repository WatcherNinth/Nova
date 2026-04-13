using System.Collections.Generic;
using Newtonsoft.Json;
using LogicEngine.Validation;
using System.Linq;

namespace LogicEngine
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

        /// <summary>
        /// 根据名称或别名尝试获取实体 ID (忽略大小写)
        /// </summary>
        /// <param name="name">玩家输入的名称</param>
        /// <returns>找到的 KeywordID，否则返回 null</returns>
        public string TryGetEntityIdByName(string name)
        {
            if (Data == null || string.IsNullOrEmpty(name)) return null;

            foreach (var kvp in Data)
            {
                var entity = kvp.Value;
                if (entity == null) continue;

                // 1. 匹配主名称
                if (string.Equals(entity.Name, name, System.StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Key;
                }

                // 2. 匹配别名列表
                if (entity.Alias != null && entity.Alias.Any(a => string.Equals(a, name, System.StringComparison.OrdinalIgnoreCase)))
                {
                    return kvp.Key;
                }
            }

            return null;
        }

        public void OnValidate(ValidationContext context)
        {
            // 校验逻辑待实现
        }
    }
}
