using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System;

[CustomEditor(typeof(Interrorgation_Topic))]
public class TopicInspectorExtension : Editor
{
    private Type _baseType = typeof(Interrorgation_Deduction);

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        Interrorgation_Topic topic = (Interrorgation_Topic)target;

        // 自动绘制基类所有序列化属性
        DrawBaseClassProperties();

        // 子类特有属性
        EditorGUILayout.PropertyField(serializedObject.FindProperty("ProofPhases"), true);

        // 操作按钮
        EditorGUILayout.Space(15);
        if (GUILayout.Button("创建一个论据阶段"))
        {
            AddNewProofPhase(topic);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawBaseClassProperties()
    {
        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;

        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;

            // 跳过脚本引用和子类自有属性
            if (prop.name == "m_Script" ||
                IsPropertyDefinedInChildClass(prop.propertyPath)) continue;

            EditorGUILayout.PropertyField(serializedObject.FindProperty(prop.propertyPath), true);
        }
    }

    private bool IsPropertyDefinedInChildClass(string propertyPath)
    {
        // 获取目标类的字段声明信息
        FieldInfo field = target.GetType().GetField(
            propertyPath,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );

        // 若字段声明类型不是基类，则为子类自有属性
        return field != null && field.DeclaringType != _baseType;
    }

    private void AddNewProofPhase(Interrorgation_Topic topic)
    {
        Undo.RecordObject(topic, "Add Proof Phase");
        topic.ProofPhases.Add(new Interrorgation_Topic.ProofPhase());
        EditorUtility.SetDirty(topic);
    }
}

[CustomPropertyDrawer(typeof(Interrorgation_Topic.ProofPhase))]
public class ProofPhaseDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property) + 24; // 增加按钮区域高度
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // 原始属性绘制
        Rect contentRect = new Rect(position.x, position.y, position.width, position.height - 24);
        EditorGUI.PropertyField(contentRect, property, label, true);
        // 按钮区域
        Rect buttonRect = new Rect(position.x + position.width - 200, position.yMax - 22, 200, 20);
        if (GUI.Button(buttonRect, "+ 添加论据到本阶段"))
        {
            CreateLinkedProof(property);
        }

        EditorGUI.EndProperty();
    }

    private void CreateLinkedProof(SerializedProperty phaseProperty)
    {
        var parentTopic = (Interrorgation_Topic)phaseProperty.serializedObject.targetObject;
        var level = parentTopic.Level;
        var typeName = "Proof";
        string levelPath = AssetDatabase.GetAssetPath(level);
        string directory = Path.GetDirectoryName(levelPath);

        Interrorgation_Proof newProof = ScriptableObject.CreateInstance<Interrorgation_Proof>();
        newProof.Level = level;
        newProof.DeductionText = $"新的{typeName}";
        // 创建新论据资产
        string levelName = level.LevelID.Replace("Level_", "");
        int deductionCount = level.Deductions?.Count + 1 ?? 1;
        string assetName = $"{levelName}_{typeName}_{deductionCount}.asset";
        string assetPath = Path.Combine(directory, assetName);

        newProof.DeductionID = assetName.Replace(".asset", "");
        AssetDatabase.CreateAsset(newProof, assetPath);

        if (level.Deductions == null)
        {
            level.Deductions = new List<Interrorgation_Deduction>();
        }
        level.Deductions.Add(newProof);

        // Extract array index from property path using regex
        var indexMatch = System.Text.RegularExpressions.Regex.Match(
            phaseProperty.propertyPath,
            @"\.Array\.data\[(\d+)\]"
        );
        if (!indexMatch.Success)
        {
            Debug.LogError("Failed to find phase index in property path: " + phaseProperty.propertyPath);
            return;
        }

        int phaseIndex = int.Parse(indexMatch.Groups[1].Value);
        // 在对应阶段添加关联
        var phase = parentTopic.ProofPhases[phaseIndex];
        phase.ProofDialogues.Add(new Interrorgation_Topic.ProofDialoguePair
        {
            Proof = newProof,
            PostProofDialogue = "默认对话内容"
        });

        // 设置Proof的对话映射
        newProof.PostTopicDialogues = new List<Interrorgation_Proof.TopicDialoguePair>
        {
            new Interrorgation_Proof.TopicDialoguePair
            {
                Topic = parentTopic,
                PostTopicDialogue = "默认对话内容"
            }
        };

        EditorUtility.SetDirty(phaseProperty.serializedObject.targetObject);
        EditorUtility.SetDirty(newProof);
        AssetDatabase.SaveAssets();
    }
}
