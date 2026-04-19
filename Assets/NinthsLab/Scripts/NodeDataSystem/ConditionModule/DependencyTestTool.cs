using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using LogicEngine;

namespace LogicEngine.Tests
{
    [ExecuteAlways]
    public class DependencyTestTool : MonoBehaviour
    {
        [Header("测试参数")]
        [Tooltip("要测试依赖关系的节点ID")]
        public string nodeId = "TestNode";

        [Tooltip("条件规则JSON")]
        [TextArea(5, 20)]
        public string jsonContent = @"
{
    ""condition"": true,
    ""or"": {
        ""optional"": true
    }
}";

        [Header("测试结果")]
        [Tooltip("依赖关系检测结果")]
        [SerializeField]
        private NodeDependency dependencyResult;

        [SerializeField]
        private string dependencyResultText;

        [SerializeField]
        private string resultColor;

        private void OnValidate()
        {
            RunTest();
        }

        [ContextMenu("运行测试")]
        public void RunTest()
        {
            dependencyResult = ConditionEvaluator.CheckDependency(nodeId, jsonContent);

            switch (dependencyResult)
            {
                case NodeDependency.RequiredTrue:
                    dependencyResultText = "RequiredTrue (必要成立)";
                    resultColor = "#FF6B6B";
                    break;
                case NodeDependency.RequiredFalse:
                    dependencyResultText = "RequiredFalse (必要不成立)";
                    resultColor = "#4ECDC4";
                    break;
                case NodeDependency.Related:
                    dependencyResultText = "Related (相关)";
                    resultColor = "#FFE66D";
                    break;
                case NodeDependency.Irrelevant:
                    dependencyResultText = "Irrelevant (无关)";
                    resultColor = "#95E1D3";
                    break;
                default:
                    dependencyResultText = "Unknown";
                    resultColor = "#CCCCCC";
                    break;
            }

            Debug.Log($"[DependencyTest] 节点 '{nodeId}' 的依赖关系: {dependencyResultText}");
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                GUILayout.BeginArea(new Rect(20, 80, 400, 100));
                GUI.backgroundColor = new Color(0, 0, 0, 0.8f);
                GUILayout.Box("", GUILayout.Width(400), GUILayout.Height(100));
                GUI.backgroundColor = Color.white;

                GUILayout.BeginVertical();
                GUILayout.Space(5);
                GUI.color = ColorUtility.TryParseHtmlString(resultColor, out var color) ? color : Color.white;
                GUILayout.Label($"节点: {nodeId}", EditorStyles.boldLabel);
                GUILayout.Label($"依赖关系: {dependencyResultText}");
                GUI.color = Color.white;
                GUILayout.Space(5);
                GUILayout.EndVertical();
                GUILayout.EndArea();
            }
        }
    }
}
