using UnityEngine;
using UnityEditor;
using System.IO;
using Nova;

namespace Nova.Editor
{
    [CustomEditor(typeof(Interrorgation_Level))]
    public class LevelInspectorExtension : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // 绘制默认的Inspector
            DrawDefaultInspector();
            
            // 获取当前编辑的Level对象
            Interrorgation_Level level = (Interrorgation_Level)target;
            
            // 添加创建Deduction的按钮
            EditorGUILayout.Space(10);
            if (GUILayout.Button("创建新的推理(Deduction)"))
            {
                CreateNewDeduction(level);
            }
        }
        
        private void CreateNewDeduction(Interrorgation_Level level)
        {
            // 获取Level资源的路径
            string levelPath = AssetDatabase.GetAssetPath(level);
            string directory = Path.GetDirectoryName(levelPath);
            
            // 创建新的Deduction资源
            Interrorgation_Deduction newDeduction = ScriptableObject.CreateInstance<Interrorgation_Deduction>();
            
            // 设置新Deduction的基本属性
            newDeduction.Level = level;
            newDeduction.DeductionText = "新的推理";
            newDeduction.ChoiceText = "新的选项";
            
            // 生成一个唯一的文件名
            string levelName = level.name.Replace("Level_", "");
            int deductionCount = level.Deductions != null ? level.Deductions.Count + 1 : 1;
            string assetName = $"Deduction_{levelName}_{deductionCount}.asset";
            string assetPath = Path.Combine(directory, assetName);
            
            // 创建资源文件
            AssetDatabase.CreateAsset(newDeduction, assetPath);
            
            // 将新创建的Deduction添加到Level的Deductions列表中
            if (level.Deductions == null)
            {
                level.Deductions = new System.Collections.Generic.List<Interrorgation_Deduction>();
            }
            level.Deductions.Add(newDeduction);
            
            // 标记Level为已修改，以便Unity保存更改
            EditorUtility.SetDirty(level);
            AssetDatabase.SaveAssets();
            
            // 选中新创建的Deduction资源
            Selection.activeObject = newDeduction;
            
            Debug.Log($"成功创建推理：{assetPath}，并关联到Level：{level.name}");
        }
    }
}