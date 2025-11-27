using System;
using UnityEditor;
using Newtonsoft.Json.Linq;
using UnityEngine; // 必须引用 Newtonsoft.Json

namespace LogicEngine
{
    public class ConditionEvaluator
    {
        /// <summary>
        /// 解析 JSON 字符串并评估结果
        /// </summary>
        public bool Evaluate(string jsonContent)
        {
            if (string.IsNullOrWhiteSpace(jsonContent)) return true;

            try
            {
                var root = JObject.Parse(jsonContent);
                // 最外层依然视为 AND 关系处理
                return EvaluateAndGroup(root);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"JSON 解析错误: {ex.Message}");
                return false;
            }
        }

        // =====================================================
        // 核心逻辑处理
        // =====================================================

        /// <summary>
        /// 处理 AND 逻辑组
        /// </summary>
        private bool EvaluateAndGroup(JToken token)
        {
            if (token.Type != JTokenType.Object) return false;

            var obj = (JObject)token;

            foreach (var property in obj.Properties())
            {
                // 如果是 AND 组，必须所有子项都通过
                if (!EvaluateItem(property.Name, property.Value))
                {
                    return false; // 短路：只要有一个失败，整体失败
                }
            }

            return true;
        }

        /// <summary>
        /// 处理 OR 逻辑组
        /// </summary>
        private bool EvaluateOrGroup(JToken token)
        {
            if (token.Type != JTokenType.Object) return false;

            var obj = (JObject)token;
            int threshold = 1; // 默认只要满足 1 个
            int passCount = 0;

            // 1. 获取阈值设置 (any_of)
            if (obj.ContainsKey("any_of"))
            {
                var anyOfToken = obj["any_of"];
                if (anyOfToken != null && int.TryParse(anyOfToken.ToString(), out int val))
                {
                    threshold = val;
                }
            }

            // 2. 遍历子项
            foreach (var property in obj.Properties())
            {
                string key = property.Name;

                // 跳过配置字段，不参与计数
                if (key.Equals("any_of", StringComparison.OrdinalIgnoreCase)) continue;

                // 计数满足条件的项
                if (EvaluateItem(key, property.Value))
                {
                    passCount++;
                }
            }

            // 检查是否达到阈值
            return passCount >= threshold;
        }

        /// <summary>
        /// 路由评估：决定当前项是“逻辑容器”还是“具体论点”
        /// </summary>
        private bool EvaluateItem(string key, JToken value)
        {
            // --- 判定规则修改 ---
            // 只有当 Value 是 Object 时，才去检查 Key 是否是逻辑关键字。
            // 这样即使有一个论点叫 "orange": true，因为它不是 Object，也不会被当作 OR 组。
            
            if (value.Type == JTokenType.Object)
            {
                // 检查 OR 变种：or, or_1, or_special...
                if (key.StartsWith("or", StringComparison.OrdinalIgnoreCase))
                {
                    return EvaluateOrGroup(value);
                }
                
                // 检查 AND 变种：and, and_1, depends_on...
                if (key.StartsWith("and", StringComparison.OrdinalIgnoreCase) || 
                    key.Equals("depends_on", StringComparison.OrdinalIgnoreCase))
                {
                    return EvaluateAndGroup(value);
                }
            }

            // --- 论点判定 ---
            // 如果 Value 是 Boolean，说明这是叶子节点（具体的论点）
            if (value.Type == JTokenType.Boolean)
            {
                bool expectedStatus = (bool)value;
                bool actualStatus = CheckArgumentStatus(key); // 调用内部方法
                
                return actualStatus == expectedStatus;
            }

            // 既不是逻辑组对象，也不是布尔值论点，视为无效或忽略（根据需求可返回 true 或 false）
            // 这里为了严格起见，无法识别的条件视为 false
            return false; 
        }

        // =====================================================
        // 论点状态检查方法
        // =====================================================

        /// <summary>
        /// 获取特定论点的实际状态
        /// </summary>
        /// <param name="argumentName">论点名称</param>
        /// <returns>论点是否成立</returns>
        protected virtual bool CheckArgumentStatus(string argumentName)
        {
            // TODO: 在这里编写实际的业务逻辑
            // 访问levelflowmap来获取相关论点/阶段的状态
            
            Debug.Log($"[检查条件] 请求获取 '{argumentName}' 的状态...");

            // 暂时先写死返回 false 或者抛出未实现异常，之后你可以修改这里
            return false; 
        }
    }
}