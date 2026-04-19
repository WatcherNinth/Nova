using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using LogicEngine.Tests; // 引用 LevelTestManager
using LogicEngine.Validation;
using Interrorgation.MidLayer; // 引用你提供的验证系统命名空间

namespace LogicEngine
{
    public enum NodeDependency
    {
        RequiredTrue,
        RequiredFalse,
        Related,
        Irrelevant
    }

    public static class ConditionEvaluator
    {
        // =====================================================
        // 工具方法
        // =====================================================

        public static HashSet<string> ExtractNodeIds(string jsonContent)
        {
            var nodeIds = new HashSet<string>();
            if (string.IsNullOrWhiteSpace(jsonContent)) return nodeIds;

            try
            {
                var root = JObject.Parse(jsonContent);
                ExtractNodeIdsRecursive(root, nodeIds);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConditionEvaluator] JSON 解析错误: {ex.Message}");
            }

            return nodeIds;
        }

        private static void ExtractNodeIdsRecursive(JToken token, HashSet<string> nodeIds)
        {
            if (token.Type != JTokenType.Object) return;
            var obj = (JObject)token;

            foreach (var property in obj.Properties())
            {
                string key = property.Name;
                JToken value = property.Value;

                if (value.Type == JTokenType.Object)
                {
                    ExtractNodeIdsRecursive(value, nodeIds);
                }
                else if (value.Type == JTokenType.Boolean)
                {
                    nodeIds.Add(key);
                }
            }
        }

        // =====================================================
        // 验证逻辑 (Validation)
        // =====================================================

        /// <summary>
        /// 验证 JSON 规则的有效性
        /// </summary>
        /// <param name="jsonContent">JSON 字符串</param>
        /// <param name="context">从外部传入的验证上下文</param>
        public static void Validate(string jsonContent, ValidationContext context)
        {
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                // 空规则通常视为合法的“无条件通过”，但也记录一条 Info
                context.LogInfo("检测到空的条件组（也算合法）。");
                return;
            }

            // 1. 尝试获取当前的 ID 列表
            // 注意：LevelTestManager 可能是单例，在非 Play 模式或未初始化时可能为空
            List<string> validIds = null;
            if (LevelGraphContext.CurrentGraph != null)
            {
                // 修正：去掉了重复的 CurrentLevelGraph
                validIds = LevelGraphContext.CurrentGraph.allIds;
            }

            if (validIds == null)
            {
                context.LogWarning("没有找到合法的LevelGraph，检查Manager是否存在。也可能是LevelGraphContext出了问题。");
                // 即使没有 ID 列表，也可以继续验证 JSON 格式是否正确
            }

            // 2. 解析 JSON
            JObject root;
            try
            {
                root = JObject.Parse(jsonContent);
            }
            catch (Exception ex)
            {
                context.LogError($"JSON Parse Error: {ex.Message}");
                return;
            }

            // 3. 启动递归验证
            // 由于 ConditionEvaluator 是静态的，我们创建一个临时的包装对象进入上下文
            // 这样 context 里的路径栈就会正确记录 (例如: Root -> depends_on -> and_1)
            var validator = new JsonStructureValidator(root, validIds);

            // 这里为了路径好看，我们不再次 Push "Root"，直接在当前层级验证，
            // 或者你可以调用 ValidateChild("Conditions", validator) 让它多一层路径
            validator.OnValidate(context);
        }

        /// <summary>
        /// 私有内部类，用于桥接 JSON 结构与 IValidatable 接口
        /// </summary>
        private class JsonStructureValidator : IValidatable
        {
            private readonly JToken _token;
            private readonly List<string> _validIds;

            public JsonStructureValidator(JToken token, List<string> validIds)
            {
                _token = token;
                _validIds = validIds;
            }

            public void OnValidate(ValidationContext context)
            {
                if (_token.Type != JTokenType.Object) return;

                var obj = (JObject)_token;

                foreach (var property in obj.Properties())
                {
                    string key = property.Name;
                    JToken value = property.Value;

                    // 忽略配置字段
                    if (key.Equals("any_of", StringComparison.OrdinalIgnoreCase)) continue;

                    // --- 情况 A: 这是一个嵌套的逻辑组 (Value 是 Object) ---
                    if (value.Type == JTokenType.Object)
                    {
                        // 递归：使用 context.ValidateChild 自动处理路径压栈和出栈
                        // 路径会变成: ... -> parent -> key (例如 "or_group")
                        context.ValidateChild(key, new JsonStructureValidator(value, _validIds));
                    }
                    // --- 情况 B: 这是一个具体的节点/论点判定 (Value 是 Boolean) ---
                    else if (value.Type == JTokenType.Boolean)
                    {
                        // 此时 Key 应该是 NodeID
                        ValidateNodeId(context, key);
                    }
                    else
                    {
                        // JSON 结构异常，既不是组也不是判定
                        context.LogWarning($"Unknown property type for key '{key}'. Expected Object or Boolean.");
                    }
                }
            }

            private void ValidateNodeId(ValidationContext context, string nodeId)
            {
                // 如果 ID 列表没获取到（比如在编辑器非运行模式），跳过检查
                if (_validIds == null) return;

                if (!_validIds.Contains(nodeId))
                {
                    // 使用 LogError，ValidationResult 会将其标记为 IsValid = false
                    // 路径会自动包含当前的层级结构
                    context.LogError($"Invalid Node ID: '{nodeId}'. Not found in LevelGraph.");
                }
            }
        }

        // =====================================================
        // 依赖检查逻辑 (Dependency Check)
        // =====================================================

        public static NodeDependency CheckDependency(string nodeId, string jsonContent)
        {
            if (string.IsNullOrWhiteSpace(jsonContent)) return NodeDependency.Irrelevant;

            try
            {
                var root = JObject.Parse(jsonContent);
                // 根节点通常隐式为一个 AND 组
                return CheckDependencyRecursive(nodeId, root, true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConditionEvaluator] JSON 解析错误: {ex.Message}");
                return NodeDependency.Irrelevant;
            }
        }

        private static NodeDependency CheckDependencyRecursive(string nodeId, JToken token, bool inAndGroup)
        {
            if (token.Type != JTokenType.Object) return NodeDependency.Irrelevant;
            var obj = (JObject)token;

            // 当前组的最终状态，默认无关
            NodeDependency groupResult = NodeDependency.Irrelevant;

            foreach (var property in obj.Properties())
            {
                string key = property.Name;
                JToken value = property.Value;

                NodeDependency childResult = NodeDependency.Irrelevant;

                if (value.Type == JTokenType.Object)
                {
                    bool isOrGroup = key.StartsWith("or", StringComparison.OrdinalIgnoreCase) ||
                    key.StartsWith("any_of", StringComparison.OrdinalIgnoreCase);
                    // 只要不是 OR 组，通常 JSON 结构中其他嵌套对象都是隐式的 AND
                    childResult = CheckDependencyRecursive(nodeId, value, !isOrGroup);
                }
                else if (value.Type == JTokenType.Boolean)
                {
                    if (key == nodeId)
                    {
                        bool expected = (bool)value;
                        if (inAndGroup)
                        {
                            // 在 AND 组中，节点状态直接决定它是“必要成立”还是“必要不成立”
                            childResult = expected ? NodeDependency.RequiredTrue : NodeDependency.RequiredFalse;
                        }
                        else
                        {
                            // 在 OR 组中，没有任何单一节点是“必要”的。
                            // 满足定义：为 true 时有助于条件成立 -> 相关
                            // 如果期望是 false（例如 Not A or B），A为true反而无助，视作无关
                            childResult = expected ? NodeDependency.Related : NodeDependency.Irrelevant;
                        }
                    }
                }

                // --- 核心逻辑：将子节点结果聚合到当前组 ---
                if (inAndGroup)
                {
                    // 在 AND 组中：必要条件会向上传递。优先级：Required > Related > Irrelevant
                    if (childResult == NodeDependency.RequiredTrue)
                        groupResult = NodeDependency.RequiredTrue;
                    else if (childResult == NodeDependency.RequiredFalse && groupResult != NodeDependency.RequiredTrue)
                        groupResult = NodeDependency.RequiredFalse;
                    else if (childResult == NodeDependency.Related && groupResult == NodeDependency.Irrelevant)
                        groupResult = NodeDependency.Related;
                }
                else
                {
                    // 在 OR 组中：内部的“必要成立”降级为“相关”（因为 OR 组有其他分支可以满足条件）
                    if (childResult == NodeDependency.RequiredTrue || childResult == NodeDependency.Related)
                    {
                        groupResult = NodeDependency.Related;
                    }
                    // 内部的“必要不成立”在 OR 组中对 Node 为 True 的情况没有帮助，保持 Irrelevant 即可
                }
            }

            return groupResult;
        }

        // =====================================================
        // 运行时评估逻辑 (Execution) - 保持不变
        // =====================================================

        public delegate bool StatusChecker(string nodeId);

        public static bool Evaluate(string jsonContent, StatusChecker customChecker = null)
        {
            if (string.IsNullOrWhiteSpace(jsonContent)) return true;

            try
            {
                var root = JObject.Parse(jsonContent);
                return EvaluateAndGroup(root, customChecker);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConditionEvaluator] JSON 解析错误: {ex.Message}");
                return false;
            }
        }

        private static bool EvaluateAndGroup(JToken token, StatusChecker checker)
        {
            if (token.Type != JTokenType.Object) return false;
            var obj = (JObject)token;

            foreach (var property in obj.Properties())
            {
                if (!EvaluateItem(property.Name, property.Value, checker)) return false;
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
                    threshold = val;
            }

            foreach (var property in obj.Properties())
            {
                string key = property.Name;
                if (key.Equals("any_of", StringComparison.OrdinalIgnoreCase)) continue;

                if (EvaluateItem(key, property.Value, checker)) passCount++;
            }

            return passCount >= threshold;
        }

        private static bool EvaluateItem(string key, JToken value, StatusChecker checker)
        {
            if (value.Type == JTokenType.Object)
            {
                if (key.StartsWith("or", StringComparison.OrdinalIgnoreCase))
                    return EvaluateOrGroup(value, checker);

                if (key.StartsWith("and", StringComparison.OrdinalIgnoreCase))
                    return EvaluateAndGroup(value, checker);
            }

            if (value.Type == JTokenType.Boolean)
            {
                bool expectedStatus = (bool)value;
                bool actualStatus = (checker != null) ? checker(key) : CheckNodeStatus(key);
                return actualStatus == expectedStatus;
            }

            return false;
        }

        private static bool CheckNodeStatus(string nodeId)
        {
            Debug.Log($"[正式环境] 检查节点状态: {nodeId}");
            var runtimeStatus = GameEventDispatcher.GetNodeStatus(nodeId);
            if (runtimeStatus == null)
            {
                Debug.LogWarning($"[ConditionEvaluator] 节点 ID '{nodeId}' 不存在于当前状态系统中，默认返回 false。");
                return false;
            }
            if (runtimeStatus.IsInvalidated) return false;
            if (runtimeStatus.Status == LevelLogic.RunTimeNodeStatus.Submitted) return true;
            return false;
        }
    }
}