using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using LogicEngine.LevelLogic;
using LogicEngine.LevelGraph;

namespace LogicEngine.Tests
{
    public class MindMapRuntimeDebugger : MonoBehaviour
    {
        [Header("--- 实时监控面板 ---")]
        public List<string> discoveredNodesDesc = new List<string>();
        public List<string> unlockedEntities = new List<string>();
        public List<string> activePhases = new List<string>();
        public bool autoRefresh = true;

        private void Update()
        {
            if (autoRefresh) RefreshData();
        }

        [ContextMenu("手动刷新数据")]
        public void RefreshData()
        {
            var levelManager = InterrorgationLevelManager.Instance;
            if (levelManager == null) return;

            // [修改] 分别反射获取两个 Manager
            var type = typeof(InterrorgationLevelManager);
            var mindMap = type.GetField("playerMindMapManager", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(levelManager) as PlayerMindMapManager;
            var phaseMgr = type.GetField("gamePhaseManager", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(levelManager) as GamePhaseManager;

            if (mindMap == null) return;

            // 1. 抓取 Nodes
            discoveredNodesDesc.Clear();
            foreach (var kvp in mindMap.RunTimeNodeDataMap)
            {
                if (kvp.Value.Status != RunTimeNodeStatus.Hidden)
                {
                    string status = kvp.Value.Status.ToString();
                    string desc = kvp.Value.r_NodeData.Basic?.Description ?? kvp.Key;
                    discoveredNodesDesc.Add($"[{status}] {desc}");
                }
            }

            // 2. 抓取 Entities
            unlockedEntities.Clear();
            foreach (var kvp in mindMap.RunTimeEntityItemDataMap)
            {
                if (kvp.Value.Status != RunTimeEntityItemStatus.Hidden)
                {
                    unlockedEntities.Add(kvp.Value.r_EntityItemData.Name);
                }
            }

            // 3. 抓取 Phases
            activePhases.Clear();
            if (phaseMgr != null)
            {
                foreach (var kvp in phaseMgr.RunTimePhaseStatusMap)
                {
                    if (kvp.Value != RuntimePhaseStatus.Locked)
                    {
                        activePhases.Add($"{kvp.Key}: {kvp.Value}");
                    }
                }
            }
        }
    }
}