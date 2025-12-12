#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AIFullFlowDebug))]
public class AIFullFlowDebugEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        AIFullFlowDebug tester = (AIFullFlowDebug)target;

        GUILayout.Space(20);

        // 设置按钮颜色
        GUI.backgroundColor = new Color(0.4f, 1f, 0.4f); 

        if (GUILayout.Button("🔥 运行全链路测试 (Run Test)", GUILayout.Height(40)))
        {
            if (Application.isPlaying)
            {
                tester.RunTest();
            }
            else
            {
                Debug.LogWarning("请先点击 Unity 的 Play 按钮运行游戏！");
            }
        }
        
        GUI.backgroundColor = Color.white;
    }
}
#endif