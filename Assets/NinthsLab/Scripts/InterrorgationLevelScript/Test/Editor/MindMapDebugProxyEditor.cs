using UnityEditor;
using UnityEngine;
using LogicEngine.LevelLogic;
using System.Collections.Generic;

namespace Interrorgation.Test
{
    [CustomEditor(typeof(MindMapDebugProxy))]
    public class MindMapDebugProxyEditor : Editor
    {
        private bool nodesFolded = true;
        private bool entitiesFolded = true;
        private bool templatesFolded = true;

        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI(); // 绘制公有字段

            var proxy = (MindMapDebugProxy)target;
            
            EditorGUILayout.BeginVertical("box");
            proxy.autoRefresh = EditorGUILayout.Toggle("实时刷新", proxy.autoRefresh);
            EditorGUILayout.EndVertical();

            var manager = proxy.TargetManager;

            if (manager == null)
            {
                EditorGUILayout.HelpBox("未能从 InterrorgationLevelManager 找到 MindMapManager 实例。\n请确保游戏正在运行且已加载 Level。", MessageType.Warning);
                return;
            }

            // 绘制节点状态
            DrawMap(manager.RunTimeNodeDataMap, ref nodesFolded, "Nodes (逻辑节点)", (id, data) => 
            {
                var statusText = data.Status.ToString();
                var color = data.IsInvalidated ? Color.red : GetStatusColor(statusText);
                
                GUI.color = color;
                EditorGUILayout.LabelField($"[{statusText}]", GUILayout.Width(80));
                
                string label = $"{id} - {data.r_NodeData?.Basic.Description}";
                if (data.IsInvalidated) label = "[X] " + label;
                EditorGUILayout.LabelField(label);
                GUI.color = Color.white;
            });

            // 绘制实体状态
            DrawMap(manager.RunTimeEntityItemDataMap, ref entitiesFolded, "Entities (实体/线索)", (id, data) => 
            {
                var color = GetStatusColor(data.Status.ToString());
                GUI.color = color;
                EditorGUILayout.LabelField($"[{data.Status}]", GUILayout.Width(80));
                GUI.color = Color.white;
                EditorGUILayout.LabelField($"{id} - {data.r_EntityItemData?.Name}");
            });

            // 绘制模板状态
            DrawMap(manager.RunTimeTemplateDataMap, ref templatesFolded, "Templates (逻辑模板)", (id, data) => 
            {
                var color = GetStatusColor(data.Status.ToString());
                GUI.color = color;
                EditorGUILayout.LabelField($"[{data.Status}]", GUILayout.Width(80));
                GUI.color = Color.white;
                EditorGUILayout.LabelField($"{id} - {data.r_TemplateData?.RawText}");
            });

            if (proxy.autoRefresh && Application.isPlaying)
            {
                Repaint(); // 运行时强制 Inspector 重绘
            }
        }

        private void DrawMap<T>(Dictionary<string, T> map, ref bool folded, string label, System.Action<string, T> drawElement)
        {
            EditorGUILayout.Space(5);
            folded = EditorGUILayout.BeginFoldoutHeaderGroup(folded, $"{label} [Count: {map.Count}]");
            if (folded)
            {
                EditorGUI.indentLevel++;
                foreach (var kvp in map)
                {
                    EditorGUILayout.BeginHorizontal();
                    drawElement(kvp.Key, kvp.Value);
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private Color GetStatusColor(string status)
        {
            switch (status)
            {
                case "Hidden": return new Color(0.7f, 0.7f, 0.7f);
                case "Discovered": return Color.green;
                case "Submitted": return Color.cyan;
                case "Used": return Color.yellow;
                case "Completed": return Color.green;
                default: return Color.white;
            }
        }
    }
}
