using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json.Linq; 
using LogicEngine;

public class ConditionTestClass : MonoBehaviour
{
    [Header("【规则配置】")]
    [Tooltip("填入标准 JSON 格式的逻辑树")]
    [TextArea(5, 20)] 
    public string jsonRule;

    [Header("【当前状态】")]
    [Tooltip("填入当前所有论点的状态 (JSON 格式)\n例如: { \"前置论点A\": true }")]
    [TextArea(5, 20)]
    public string jsonLevelCondition;

    void Start()
    {
        // 自动运行一次，或者你可以手动调用 RunTest
        RunTest();
    }

    [ContextMenu("执行测试")]
    public void RunTest()
    {
        Debug.Log(">>> 开始执行静态类条件判定测试...");

        // 1. 准备测试数据：将 Inspector 的字符串转为字典
        Dictionary<string, bool> testStates = ParseStateJson(jsonLevelCondition);

        // 2. 定义测试用的查询函数 (Closure)
        // 这个函数会在 ConditionEvaluator 内部被调用，代替原本的 CheckArgumentStatus
        ConditionEvaluator.StatusChecker testChecker = (string name) => 
        {
            if (testStates.TryGetValue(name, out bool val))
            {
                return val;
            }
            Debug.LogWarning($"[测试数据] 未找到论点 '{name}'，默认为 false");
            return false;
        };

        // 3. 调用静态方法 Evaluate，并传入我们的测试 Checker
        bool result = ConditionEvaluator.Evaluate(jsonRule, testChecker);

        // 4. 输出结果
        string color = result ? "<color=green>通过 (True)</color>" : "<color=red>拒绝 (False)</color>";
        Debug.Log($"----------------\n最终判定结果: {color}");
    }

    // --- 辅助工具：解析 JSON 到字典 ---
    private Dictionary<string, bool> ParseStateJson(string json)
    {
        var states = new Dictionary<string, bool>();
        if (string.IsNullOrWhiteSpace(json)) return states;

        try
        {
            var root = JObject.Parse(json);
            foreach (var prop in root.Properties())
            {
                if (prop.Value.Type == JTokenType.Boolean)
                {
                    states[prop.Name] = (bool)prop.Value;
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"测试状态 JSON 格式错误: {ex.Message}");
        }
        return states;
    }
}