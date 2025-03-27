using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Nova;

namespace Nova.Editor
{
    [CustomEditor(typeof(Interrorgation_Level))]
    public class LevelInspectorExtension : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            Interrorgation_Level level = (Interrorgation_Level)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("创建新的论点(Topic)"))
            {
                CreateNewTopic(level);
            }
            if (GUILayout.Button("创建新的论据(Proof)"))
            {
                CreateNewProof(level);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void CreateNewTopic(Interrorgation_Level level)
        {
            CreateDeductionAsset<Interrorgation_Topic>(level, "Topic");
        }

        private void CreateNewProof(Interrorgation_Level level)
        {
            CreateDeductionAsset<Interrorgation_Proof>(level, "Proof");
        }

        private void CreateDeductionAsset<T>(Interrorgation_Level level, string typeName) where T : Interrorgation_Deduction
        {
            string levelPath = AssetDatabase.GetAssetPath(level);
            string directory = Path.GetDirectoryName(levelPath);

            T newDeduction = ScriptableObject.CreateInstance<T>();
            newDeduction.Level = level;
            newDeduction.DeductionText = $"新的{typeName}";

            string levelName = level.LevelID.Replace("Level_", "");
            int deductionCount = level.Deductions?.Count + 1 ?? 1;
            string assetName = $"{levelName}_{typeName}_{deductionCount}.asset";
            string assetPath = Path.Combine(directory, assetName);

            newDeduction.DeductionID = assetName.Replace(".asset", "");

            AssetDatabase.CreateAsset(newDeduction, assetPath);

            if (level.Deductions == null)
            {
                level.Deductions = new List<Interrorgation_Deduction>();
            }
            level.Deductions.Add(newDeduction);

            EditorUtility.SetDirty(level);
            AssetDatabase.SaveAssets();
            Selection.activeObject = newDeduction;
        }
    }
}
