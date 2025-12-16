using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using LogicEngine.LevelLogic;
using LogicEngine.LevelGraph;

namespace LogicEngine.Tests
{
    public class MindMapRuntimeDebugger : MonoBehaviour
    {
        [Header("--- 实时监控面板 (点击右键 Refresh) ---")]
        
        [Header("1. 已发现的选项/节点 (Nodes)")]
        public List<DebugNodeInfo> discoveredNodes = new List<DebugNodeInfo>();

        [Header("2. 已解锁的关键词 (Entities)")]
        public List<DebugEntityInfo> unlockedEntities = new List<DebugEntityInfo>();

        [Header("3. 已发现的模板 (Templates)")]
        public List<DebugTemplateInfo> discoveredTemplates = new List<DebugTemplateInfo>();

        [Header("4. 当前激活的阶段")]
        public List<string> activePhases = new List<string>();

        // 自动刷新开关
        public bool autoRefresh = true;

        private void Update()
        {
            if (autoRefresh)
            {
                RefreshData();
            }
        }

        [ContextMenu("手动刷新数据")]
        public void RefreshData()
        {
            var levelManager = InterrorgationLevelManager.Instance;
            if (levelManager == null) return;

            // 1. 使用反射获取 PlayerMindMapManager
            var field = typeof(InterrorgationLevelManager).GetField("playerMindMapManager", BindingFlags.NonPublic | BindingFlags.Instance);
            var mindMap = field.GetValue(levelManager) as PlayerMindMapManager;

            if (mindMap == null) return;

            // 2. 抓取 Node
            discoveredNodes.Clear();
            foreach (var kvp in mindMap.RunTimeNodeDataMap)
            {
                // 只显示已发现或已提交的
                if (kvp.Value.Status != RunTimeNodeStatus.Hidden)
                {
                    string desc = kvp.Value.r_NodeData.Basic?.Description ?? "无描述";
                    discoveredNodes.Add(new DebugNodeInfo 
                    { 
                        Id = kvp.Key, 
                        Status = kvp.Value.Status.ToString(),
                        Description = desc
                    });
                }
            }

            // 3. 抓取 Entity
            unlockedEntities.Clear();
            foreach (var kvp in mindMap.RunTimeEntityItemDataMap)
            {
                if (kvp.Value.Status != RunTimeEntityItemStatus.Hidden)
                {
                    string name = kvp.Value.r_EntityItemData.Name;
                    unlockedEntities.Add(new DebugEntityInfo 
                    { 
                        Id = kvp.Key, 
                        Name = name 
                    });
                }
            }

            // 4. 抓取 Template
            discoveredTemplates.Clear();
            foreach (var kvp in mindMap.RunTimeTemplateDataMap)
            {
                if (kvp.Value.Status != RunTimeTemplateDataStatus.Hidden)
                {
                    string text = kvp.Value.r_TemplateData.RawText;
                    discoveredTemplates.Add(new DebugTemplateInfo 
                    { 
                        Id = kvp.Key, 
                        TextContent = text 
                    });
                }
            }

            // 5. 抓取 Phase
            activePhases.Clear();
            foreach (var kvp in mindMap.RunTimePhaseStatusMap)
            {
                if (kvp.Value == RuntimePhaseStatus.Active)
                {
                    activePhases.Add(kvp.Key);
                }
            }
        }

        [System.Serializable]
        public class DebugNodeInfo
        {
            public string Id;
            public string Status;
            [TextArea] public string Description;
        }

        [System.Serializable]
        public class DebugEntityInfo
        {
            public string Id;
            public string Name;
        }

        [System.Serializable]
        public class DebugTemplateInfo
        {
            public string Id;
            public string TextContent;
        }
    }
}