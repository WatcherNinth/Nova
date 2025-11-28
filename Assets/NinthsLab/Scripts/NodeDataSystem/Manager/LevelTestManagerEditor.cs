#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace LogicEngine.Tests
{
    [CustomEditor(typeof(LevelTestManager))]
    public class LevelTestManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // 绘制默认的 Inspector 属性
            DrawDefaultInspector();

            LevelTestManager manager = (LevelTestManager)target;

            GUILayout.Space(10);

            // 绘制自定义按钮
            // 使用 GUI.backgroundColor 让按钮显眼一点
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.green;

            if (GUILayout.Button("Run Validation for All JSONs", GUILayout.Height(40)))
            {
                // 确保单例被正确赋值（如果是 Edit Mode）
                if (LevelTestManager.Instance == null)
                {
                    // 强制刷新一下
                    var temp = manager.gameObject.GetComponent<LevelTestManager>();
                    // 这里虽然无法直接设 private set 的 Instance，
                    // 但 manager.RunAllTests() 内部使用的是 this 实例的上下文，
                    // 只要 RunAllTests 里赋值 CurrentLevelGraph，
                    // 且 Awake/OnEnable 已经跑过，Instance 就没问题。
                    // 为了保险，通常 EditMode 下建议手动触发一次 OnEnable 逻辑
                    manager.SendMessage("OnEnable", SendMessageOptions.DontRequireReceiver);
                }

                manager.RunAllTests();
            }

            GUI.backgroundColor = originalColor;
        }
    }
}
#endif