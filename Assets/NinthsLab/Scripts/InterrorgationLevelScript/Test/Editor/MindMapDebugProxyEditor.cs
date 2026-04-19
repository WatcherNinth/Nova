using UnityEditor;
using UnityEngine;
using LogicEngine.LevelLogic;
using Interrorgation.MidLayer;
using System.Collections.Generic;
using System.Linq;

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
            },
            (id, data) =>
            {
                var nodeData = data as RuntimeNodeData;
                if (nodeData?.Status == RunTimeNodeStatus.Hidden)
                {
                    if (GUILayout.Button("发现", GUILayout.Width(80)))
                    {
                        GameEventDispatcher.DispatchDiscoverNewNodes(new List<string> { id },
                        new GameEventDispatcher.NodeDiscoverContext(GameEventDispatcher.NodeDiscoverContext.e_DiscoverNewNodeMethod.PlayerInput));
                    }
                }
            }, manager);

            // 绘制实体状态
            DrawMap(manager.RunTimeEntityItemDataMap, ref entitiesFolded, "Entities (实体/线索)", (id, data) => 
            {
                var color = GetStatusColor(data.Status.ToString());
                GUI.color = color;
                EditorGUILayout.LabelField($"[{data.Status}]", GUILayout.Width(80));
                GUI.color = Color.white;
                EditorGUILayout.LabelField($"{id} - {data.r_EntityItemData?.Name}");
            },
            (id, data) =>
            {
                var entityData = data as RuntimeEntityItemData;
                if (entityData?.Status == RunTimeEntityItemStatus.Hidden)
                {
                    if (GUILayout.Button("发现", GUILayout.Width(80)))
                    {
                        GameEventDispatcher.DispatchDiscoveredNewEntityItems(new List<string> { id });
                    }
                }
            }, manager);

            // 绘制模板状态
            DrawMap(manager.RunTimeTemplateDataMap, ref templatesFolded, "Templates (逻辑模板)", (id, data) => 
            {
                var color = GetStatusColor(data.Status.ToString());
                GUI.color = color;
                EditorGUILayout.LabelField($"[{data.Status}]", GUILayout.Width(80));
                GUI.color = Color.white;
                EditorGUILayout.LabelField($"{id} - {data.r_TemplateData?.RawText}");
            },
            (id, data) =>
            {
                var templateData = data as RuntimeTemplateData;
                if (templateData?.Status == RunTimeTemplateDataStatus.Hidden)
                {
                    if (GUILayout.Button("发现", GUILayout.Width(80)))
                    {
                        GameEventDispatcher.DispatchDiscoveredNewTemplates(new List<string> { id });
                    }
                }
            }, manager);

            if (proxy.autoRefresh && Application.isPlaying)
            {
                Repaint(); // 运行时强制 Inspector 重绘
            }
        }

        private void DrawMap<T>(Dictionary<string, T> map, ref bool folded, string label, System.Action<string, T> drawElement, System.Action<string, T> drawActionButton = null, PlayerMindMapManager manager = null)
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
                    if (drawActionButton != null)
                    {
                        EditorGUILayout.Separator();
                        drawActionButton(kvp.Key, kvp.Value);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }
            
            // Add batch discover button after the foldout group
            if (drawActionButton != null)
            {
                var hiddenCount = map.Values.Count(v => 
                {
                    if (typeof(T) == typeof(RuntimeNodeData))
                    {
                        var nodeData = v as RuntimeNodeData;
                        return nodeData?.Status == RunTimeNodeStatus.Hidden;
                    }
                    else if (typeof(T) == typeof(RuntimeEntityItemData))
                    {
                        var entityData = v as RuntimeEntityItemData;
                        return entityData?.Status == RunTimeEntityItemStatus.Hidden;
                    }
                    else if (typeof(T) == typeof(RuntimeTemplateData))
                    {
                        var templateData = v as RuntimeTemplateData;
                        return templateData?.Status == RunTimeTemplateDataStatus.Hidden;
                    }
                    return false;
                });
                
                if (hiddenCount > 0)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button($"发现所有 {label.Split(' ')[0]} ({hiddenCount})", GUILayout.Width(200)))
                    {
                        if (typeof(T) == typeof(RuntimeNodeData))
                        {
                            var hiddenNodeIds = new List<string>();
                            foreach (var kvp in map)
                            {
                                var nodeData = kvp.Value as RuntimeNodeData;
                                if (nodeData?.Status == RunTimeNodeStatus.Hidden)
                                {
                                    hiddenNodeIds.Add(kvp.Key);
                                    manager.SetNodeStatus(kvp.Key, RunTimeNodeStatus.Discovered);
                                }
                            }
                            if (hiddenNodeIds.Count > 0)
                            {
                                GameEventDispatcher.DispatchDiscoverNewNodes(hiddenNodeIds, new GameEventDispatcher.NodeDiscoverContext(GameEventDispatcher.NodeDiscoverContext.e_DiscoverNewNodeMethod.PlayerInput));
                            }
                        }
                        else if (typeof(T) == typeof(RuntimeEntityItemData))
                        {
                            var hiddenEntityIds = new List<string>();
                            foreach (var kvp in map)
                            {
                                var entityData = kvp.Value as RuntimeEntityItemData;
                                if (entityData?.Status == RunTimeEntityItemStatus.Hidden)
                                {
                                    hiddenEntityIds.Add(kvp.Key);
                                    entityData.Status = RunTimeEntityItemStatus.Discovered;
                                }
                            }
                            if (hiddenEntityIds.Count > 0)
                            {
                                GameEventDispatcher.DispatchDiscoveredNewEntityItems(hiddenEntityIds);
                            }
                        }
                        else if (typeof(T) == typeof(RuntimeTemplateData))
                        {
                            var hiddenTemplateIds = new List<string>();
                            foreach (var kvp in map)
                            {
                                var templateData = kvp.Value as RuntimeTemplateData;
                                if (templateData?.Status == RunTimeTemplateDataStatus.Hidden)
                                {
                                    hiddenTemplateIds.Add(kvp.Key);
                                    templateData.Status = RunTimeTemplateDataStatus.Discovered;
                                }
                            }
                            if (hiddenTemplateIds.Count > 0)
                            {
                                GameEventDispatcher.DispatchDiscoveredNewTemplates(hiddenTemplateIds);
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
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
