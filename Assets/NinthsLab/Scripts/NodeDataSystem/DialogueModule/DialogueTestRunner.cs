using UnityEngine;
using LogicEngine; // 引用逻辑脚本的命名空间

public class DialogueTestRunner : MonoBehaviour
{
    [Header("测试配置")]
    [Tooltip("在这里直接编辑或粘贴JSON对话数据")]
    [TextArea(10, 25)] // 设置文本框高度，方便查看长JSON
    public string jsonInput = @"
    {
        ""on_proven"": {
            ""text_intro"": ""[测试] 这是一个测试文本块。"",
            ""first_valid_check"": {
                ""triggered_text_vip"": {
                    ""limit"": { ""is_vip"": true },
                    ""text"": ""尊贵的VIP用户，你好。""
                },
                ""fallback_text"": ""普通用户，你好。""
            },
            ""triggered_text_extra"": {
                ""limit"": { ""has_quest_item"": true },
                ""text"": ""我看你骨骼惊奇，这本秘籍就卖给你了。""
            },
            ""call_choice_group_1"": ""choice_start_001"",
            ""text_ignored"": ""这句话应该被截断，不会出现在结果里。""
        }
    }";

    private void Start()
    {
        PerformTest();
    }

    /// <summary>
    /// 执行解析并打印结果
    /// </summary>
    [ContextMenu("Run Test Again")] // 运行游戏时，可以在组件右上角齿轮菜单里手动再次点击触发
    public void PerformTest()
    {
        if (string.IsNullOrWhiteSpace(jsonInput))
        {
            Debug.LogWarning("[DialogueTestRunner] JSON输入为空！");
            return;
        }

        Debug.Log($"<color=cyan>[DialogueTestRunner] 开始解析...</color>");

        // 记录一下耗时（可选，用于简单的性能参考）
        float t0 = Time.realtimeSinceStartup;

        // 调用核心静态方法
        string resultJson = DialogueParser.ParseDialogue(jsonInput);

        float t1 = Time.realtimeSinceStartup;

        // 输出结果
        Debug.Log($"<b>[解析结果 (耗时: {(t1 - t0) * 1000:F2}ms)]</b>:\n{resultJson}");
    }
}