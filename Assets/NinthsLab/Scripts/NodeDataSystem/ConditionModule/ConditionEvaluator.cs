using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace LogicEngine
{
    public static class ConditionEvaluator
    {
        // 定义一个委托类型，用于查询状态
        public delegate bool StatusChecker(string argumentName);

        /// <summary>
        /// 解析 JSON 字符串并评估结果
        /// </summary>
        /// <param name="jsonContent">JSON 规则字符串</param>
        /// <param name="customChecker">
        /// [可选] 测试用的状态检查函数。
        /// 如果不传(null)，则默认使用类内部的 CheckArgumentStatus 方法（正式环境）。
        /// </param>
        public static bool Evaluate(string jsonContent, StatusChecker customChecker = null)
        {
            if (string.IsNullOrWhiteSpace(jsonContent)) return true;

            try
            {
                var root = JObject.Parse(jsonContent);
                // 根节点视为 AND 组，传入 customChecker 以便向下传递
                return EvaluateAndGroup(root, customChecker);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConditionEvaluator] JSON 解析错误: {ex.Message}");
                return false;
            }
        }

        // =====================================================
        // 核心逻辑处理 (全部 Static)
        // =====================================================

        private static bool EvaluateAndGroup(JToken token, StatusChecker checker)
        {
            if (token.Type != JTokenType.Object) return false;

            var obj = (JObject)token;

            foreach (var property in obj.Properties())
            {
                if (!EvaluateItem(property.Name, property.Value, checker))
                {
                    return false; // AND 短路
                }
            }
            return true;
        }

        private static bool EvaluateOrGroup(JToken token, StatusChecker checker)
        {
            if (token.Type != JTokenType.Object) return false;

            var obj = (JObject)token;
            int threshold = 1; 
            int passCount = 0;

            if (obj.ContainsKey("any_of"))
            {
                var anyOfToken = obj["any_of"];
                if (anyOfToken != null && int.TryParse(anyOfToken.ToString(), out int val))
                {
                    threshold = val;
                }
            }

            foreach (var property in obj.Properties())
            {
                string key = property.Name;
                if (key.Equals("any_of", StringComparison.OrdinalIgnoreCase)) continue;

                if (EvaluateItem(key, property.Value, checker))
                {
                    passCount++;
                }
            }

            return passCount >= threshold;
        }

        private static bool EvaluateItem(string key, JToken value, StatusChecker checker)
        {
            // --- 1. 逻辑组判定 ---
            if (value.Type == JTokenType.Object)
            {
                if (key.StartsWith("or", StringComparison.OrdinalIgnoreCase))
                {
                    return EvaluateOrGroup(value, checker);
                }
                
                if (key.StartsWith("and", StringComparison.OrdinalIgnoreCase))
                {
                    return EvaluateAndGroup(value, checker);
                }
            }

            // --- 2. 论点判定 (Leaf Node) ---
            if (value.Type == JTokenType.Boolean)
            {
                bool expectedStatus = (bool)value;
                
                // 关键点：如果有注入的测试Checker，用测试的；否则用内部正式的
                bool actualStatus = (checker != null) 
                    ? checker(key) 
                    : CheckArgumentStatus(key);

                return actualStatus == expectedStatus;
            }

            return false;
        }

        // =====================================================
        // 正式环境用的判定方法
        // =====================================================

        /// <summary>
        /// 内部静态方法：正式游戏逻辑获取状态的地方
        /// </summary>
        private static bool CheckArgumentStatus(string argumentName)
        {
            // TODO: 这里写正式环境的逻辑，比如访问 LevelFlowMap.Instance...
            // 目前先暂时留空
            Debug.Log($"[正式环境] 检查论点: {argumentName}");
            return false; 
        }
    }
}