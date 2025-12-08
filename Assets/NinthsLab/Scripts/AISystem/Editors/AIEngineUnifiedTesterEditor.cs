#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace AIEngine.Tests
{
    [CustomEditor(typeof(AIEngineUnifiedTester))]
    public class AIEngineUnifiedTesterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // 绘制原本的属性变量
            DrawDefaultInspector();

            AIEngineUnifiedTester tester = (AIEngineUnifiedTester)target;

            GUILayout.Space(15);
            EditorGUILayout.LabelField("测试操作流程", EditorStyles.boldLabel);

            // --- 按钮 1 ---
            GUI.backgroundColor = new Color(0.6f, 0.8f, 1f); // 浅蓝色
            if (GUILayout.Button("1. 组合 Prompts & Request 并验证", GUILayout.Height(35)))
            {
                tester.GenerateAndVerify();
            }

            GUILayout.Space(5);

            // --- 按钮 2 ---
            // 只有在 Play 模式下才允许点击发送网络请求（因为依赖 Coroutine）
            if (Application.isPlaying)
            {
                GUI.backgroundColor = new Color(0.6f, 1f, 0.6f); // 浅绿色
                if (GUILayout.Button("2. 发送请求到 AI 服务器", GUILayout.Height(35)))
                {
                    tester.SendRequest();
                }
            }
            else
            {
                GUI.backgroundColor = Color.gray;
                if (GUILayout.Button("2. 发送请求 (仅运行时可用)", GUILayout.Height(35)))
                {
                    Debug.LogWarning("请先点击 Unity 的 Play 按钮运行游戏，才能发送网络请求。");
                }
            }
            
            // 恢复颜色
            GUI.backgroundColor = Color.white;
        }
    }
}
#endif