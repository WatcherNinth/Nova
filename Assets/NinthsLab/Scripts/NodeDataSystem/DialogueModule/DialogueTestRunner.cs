using UnityEngine;
using LogicEngine; // 引用逻辑脚本的命名空间

#if UNITY_EDITOR
using UnityEditor; // 仅在编辑器环境下引用，防止打包报错
#endif

public class DialogueTestRunner : MonoBehaviour
{
    [Header("测试配置")]
    [Tooltip("在这里直接编辑或粘贴JSON对话数据")]
    [TextArea(10, 25)] 
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
    public void PerformTest()
    {
        if (string.IsNullOrWhiteSpace(jsonInput))
        {
            Debug.LogWarning("[DialogueTestRunner] JSON输入为空！");
            return;
        }

        Debug.Log($"<color=cyan>[DialogueTestRunner] 开始解析...</color>");

        float t0 = Time.realtimeSinceStartup;

        // 调用 LogicEngine.DialogueParser
        string resultJson = DialogueParser.ParseDialogue(jsonInput);

        Debug.Log(DialogueParser.ValidateDialogue(jsonInput).ToString());

        float t1 = Time.realtimeSinceStartup;

        Debug.Log($"<b>[解析结果 (耗时: {(t1 - t0) * 1000:F2}ms)]</b>:\n{resultJson}");
    }
}

// --------------------------------------------------------------------------
// 自定义编辑器扩展部分 (Custom Editor)
// --------------------------------------------------------------------------
#if UNITY_EDITOR
[CustomEditor(typeof(DialogueTestRunner))]
public class DialogueTestRunnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 1. 绘制默认的Inspector（包括脚本引用、TextArea文本框等）
        DrawDefaultInspector();

        // 获取目标脚本对象
        DialogueTestRunner runner = (DialogueTestRunner)target;

        // 2. 添加空行，美观一点
        EditorGUILayout.Space(10);

        // 3. 绘制按钮
        // 只有在 Play Mode (运行模式) 下按钮才有效，或者根据需求在编辑模式下也能跑（纯逻辑通常可以在编辑模式跑）
        // 这里没有限制 Application.isPlaying，所以你在不运行游戏时点击也能看到Console输出
        if (GUILayout.Button("Run Test Again (手动执行)", GUILayout.Height(30)))
        {
            runner.PerformTest();
        }
    }
}
#endif