using UnityEngine;
using UnityEditor;
using LogicEngine;
class ConditionTestClass: MonoBehaviour
{
    [Header("这里写判断条件")]
    [SerializeField]
    string jsonRule;
    public void Start()
    {
        Main();
    }
    void Main()
        {
            // 为了测试，我们需要创建一个继承类来重写 CheckArgumentStatus，
            // 或者直接修改上面的类。这里为了演示方便，我直接继承并模拟数据。
            var evaluator = new MockEvaluator();

            jsonRule = @"
            {
                ""depends_on"": {
                    ""前置论点A"": true,
                    ""and_1"": { 
                        ""And组论点1"": true 
                    },
                    ""or_group_special"": {
                        ""any_of"": 2,
                        ""Or论点1"": true,
                        ""Or论点2"": true
                    },
                    ""android_user"": true  
                }
            }";
            // 注意上面 ""android_user"": true 是为了测试防止被识别成 ""and"" 组。
            // 因为它的值是 true (bool) 而不是 Object，所以会被正确当作普通论点处理。

            bool result = evaluator.Evaluate(jsonRule);
            Debug.Log($"----------------\n最终判定结果: {result}");
        }
    }

    // 模拟用的子类，用来填充那个“暂时不写”的逻辑
    public class MockEvaluator : ConditionEvaluator
    {
        protected override bool CheckArgumentStatus(string argumentName)
        {
            // 模拟数据
            if (argumentName == "前置论点A") return true;
            if (argumentName == "And组论点1") return true;
            if (argumentName == "Or论点1") return false;
            if (argumentName == "Or论点2") return true; // OR组里有一个true就够了
            if (argumentName == "android_user") return true;

            return false;
        }
    }