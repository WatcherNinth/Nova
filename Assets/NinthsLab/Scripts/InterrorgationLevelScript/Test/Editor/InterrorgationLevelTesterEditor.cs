using UnityEngine;
using UnityEditor;
using LogicEngine.Test;

namespace LogicEngine.EditorScripts
{
    [CustomEditor(typeof(InterrorgationLevelTester))]
    public class InterrorgationLevelTesterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // 绘制默认的公有变量（ID，列表等）
            DrawDefaultInspector();

            InterrorgationLevelTester tester = (InterrorgationLevelTester)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Level Operations", EditorStyles.boldLabel);
            if (GUILayout.Button("Load Level", GUILayout.Height(30)))
            {
                tester.TestLoadLevel();
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Template Test Section (Logic Side)", EditorStyles.boldLabel);
            
            GUI.color = Color.cyan;
            if (GUILayout.Button("Trigger: Dispatch Discovered Templates", GUILayout.Height(25)))
            {
                tester.TestDispatchDiscoveredTemplates();
            }
            
            GUI.color = Color.yellow;
            if (GUILayout.Button("Trigger: Player Submit Template Answer", GUILayout.Height(25)))
            {
                tester.TestDispatchPlayerSubmitTemplate();
            }

            GUI.color = Color.white;
            EditorGUILayout.HelpBox("提示: 触发 Dispatch Discovered 会通过 GameEventDispatcher 广播，UI 应响应并显示。\n" +
                                    "提交答案会广播给 LevelManager，后端应验证并打印结果。", MessageType.Info);
        }
    }
}
