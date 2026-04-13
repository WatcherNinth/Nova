using System.Collections.Generic;
using System.Linq;
using AIEngine.Network;
using LogicEngine.LevelGraph;
using Interrorgation.MidLayer;
using LogicEngine.Templates;

namespace LogicEngine.LevelLogic
{
    public class PlayerMindMapManager
    {
        public readonly LevelGraphData levelGraph;

        // 数据存储
        public Dictionary<string, RuntimeNodeData> RunTimeNodeDataMap = new Dictionary<string, RuntimeNodeData>();
        public Dictionary<string, RuntimeEntityItemData> RunTimeEntityItemDataMap = new Dictionary<string, RuntimeEntityItemData>();
        public Dictionary<string, RuntimeTemplateData> RunTimeTemplateDataMap = new Dictionary<string, RuntimeTemplateData>();

        public PlayerMindMapManager(LevelGraphData inputLevelGraph)
        {
            levelGraph = inputLevelGraph;
            InitializeRuntimeData();
        }

        void InitializeRuntimeData()
        {
            // 1. Nodes
            RunTimeNodeDataMap.Clear();
            if (levelGraph.nodeLookup != null)
            {
                foreach (var kvp in levelGraph.nodeLookup)
                {
                    if (!string.IsNullOrEmpty(kvp.Key) && kvp.Value.Node != null)
                    {
                        RunTimeNodeDataMap[kvp.Key] = new RuntimeNodeData(kvp.Key, kvp.Value.Node, RunTimeNodeStatus.Hidden);
                    }
                }
            }

            // 2. Templates
            RunTimeTemplateDataMap.Clear();
            if (levelGraph.allTemplates != null)
            {
                foreach (var kvp in levelGraph.allTemplates)
                {
                    if (!string.IsNullOrEmpty(kvp.Key) && kvp.Value != null)
                    {
                        RunTimeTemplateDataMap[kvp.Key] = new RuntimeTemplateData(kvp.Key, kvp.Value, RunTimeTemplateDataStatus.Hidden);
                    }
                }
            }

            // 3. Entities
            RunTimeEntityItemDataMap.Clear();
            if (levelGraph.entityListData != null && levelGraph.entityListData.Data != null)
            {
                foreach (var kvp in levelGraph.entityListData.Data)
                {
                    if (!string.IsNullOrEmpty(kvp.Key) && kvp.Value != null)
                    {
                        RunTimeEntityItemDataMap[kvp.Key] = new RuntimeEntityItemData(kvp.Key, kvp.Value, RunTimeEntityItemStatus.Hidden);
                    }
                }
            }
        }

        // --- Data Access Methods ---

        public bool TryGetNode(string nodeId, out RuntimeNodeData node)
        {
            return RunTimeNodeDataMap.TryGetValue(nodeId, out node);
        }

        public void SetNodeStatus(string nodeId, RunTimeNodeStatus newStatus)
        {
            if (RunTimeNodeDataMap.TryGetValue(nodeId, out var node))
            {
                node.Status = newStatus;
                GameEventDispatcher.DispatchNodeStatusChanged(node);
            }
        }

        // --- Discovery Logic (AI 响应处理 - 已迁移到 Coordinator) ---

        public void SubscribeEvents()
        {
            GameEventDispatcher.OnDiscoverNewNodes += DiscoverNodes;
            GameEventDispatcher.OnDiscoveredNewEntity += DiscoverEntity;
            GameEventDispatcher.OnDiscoveredNewTemplates += DiscoverTemplates;

            GameEventDispatcher.OnGetNodeStatus += HandleGetNodeStatus;
            GameEventDispatcher.OnGetEntityStatus += HandleGetEntityStatus;
            GameEventDispatcher.OnGetTemplateStatus += HandleGetTemplateStatus;
        }

        public void UnsubscribeEvents()
        {
            GameEventDispatcher.OnDiscoverNewNodes -= DiscoverNodes;
            GameEventDispatcher.OnDiscoveredNewEntity -= DiscoverEntity;
            GameEventDispatcher.OnDiscoveredNewTemplates -= DiscoverTemplates;

            GameEventDispatcher.OnGetNodeStatus -= HandleGetNodeStatus;
            GameEventDispatcher.OnGetEntityStatus -= HandleGetEntityStatus;
            GameEventDispatcher.OnGetTemplateStatus -= HandleGetTemplateStatus;
        }

        private RuntimeNodeData HandleGetNodeStatus(string id) => RunTimeNodeDataMap.TryGetValue(id, out var data) ? data : null;
        private RuntimeEntityItemData HandleGetEntityStatus(string id) => RunTimeEntityItemDataMap.TryGetValue(id, out var data) ? data : null;
        private RuntimeTemplateData HandleGetTemplateStatus(string id) => RunTimeTemplateDataMap.TryGetValue(id, out var data) ? data : null;

        public void DiscoverNodes(List<string> nodeIds, GameEventDispatcher.NodeDiscoverContext context)
        {
            foreach (var id in nodeIds)
            {
                // 只有在 Hidden 状态下才允许被发现
                if (RunTimeNodeDataMap.TryGetValue(id, out var node) && node.Status == RunTimeNodeStatus.Hidden)
                {
                    node.Status = RunTimeNodeStatus.Discovered;
                }
            }
        }

        public void DiscoverEntity(List<string> entityIds)
        {
            foreach (var id in entityIds)
            {
                if (RunTimeEntityItemDataMap.TryGetValue(id, out var entity) && entity.Status == RunTimeEntityItemStatus.Hidden)
                {
                    entity.Status = RunTimeEntityItemStatus.Discovered;
                }
            }
        }

        public void DiscoverTemplates(List<string> templateIds)
        {
            foreach (var id in templateIds)
            {
                if (RunTimeTemplateDataMap.TryGetValue(id, out var tmpl) && tmpl.Status == RunTimeTemplateDataStatus.Hidden)
                {
                    tmpl.Status = RunTimeTemplateDataStatus.Discovered;
                }
            }
        }

        // --- Template Validation ---

        public string ValidateTemplateAnswer(string templateId, List<string> playerAnswers)
        {
            string resultId = ValidateSingleTemplate(templateId, playerAnswers);
            if (resultId != null) return resultId;

            // Smart Matching (兄弟节点)
            if (RunTimeTemplateDataMap.TryGetValue(templateId, out var currentRtTemplate))
            {
                string currentText = currentRtTemplate.r_TemplateData.RawText;
                foreach (var kvp in RunTimeTemplateDataMap)
                {
                    if (kvp.Key == templateId) continue;
                    if (kvp.Value.r_TemplateData.RawText != currentText) continue;

                    string siblingResult = ValidateSingleTemplate(kvp.Key, playerAnswers);
                    if (siblingResult != null) return siblingResult;
                }
            }
            return null;
        }

        private string ValidateSingleTemplate(string templateId, List<string> playerInputs)
        {
            if (!RunTimeTemplateDataMap.TryGetValue(templateId, out var rtTemplate)) return null;
            foreach (var answer in rtTemplate.r_TemplateData.Answers)
            {
                if (CheckAnswerMatch(answer, playerInputs)) return answer.TargetId;
            }
            return null;
        }

        private bool CheckAnswerMatch(AnswerData answer, List<string> playerInputs)
        {
            if (playerInputs.Count != answer.RequiredInputs.Count) return false;
            for (int i = 0; i < answer.RequiredInputs.Count; i++)
            {
                string target = answer.RequiredInputs[i];
                string input = playerInputs[i];
                if (levelGraph.entityListData.Data.TryGetValue(target, out var entity))
                {
                    bool matchName = string.Equals(input, entity.Name, System.StringComparison.OrdinalIgnoreCase);
                    bool matchAlias = entity.Alias != null && entity.Alias.Contains(input);
                    bool matchId = string.Equals(input, target, System.StringComparison.OrdinalIgnoreCase);
                    if (!matchName && !matchAlias && !matchId) return false;
                }
                else
                {
                    if (target != input) return false;
                }
            }
            return true;
        }
    }

    // ==========================================
    // Runtime Classes & Enums (保留在这里供全局引用)
    // ==========================================
    
    public enum RunTimeNodeStatus { Hidden, Discovered, Submitted }
    public enum RuntimePhaseStatus { Locked, Active, Completed, Paused }
    public enum RunTimeEntityItemStatus { Hidden, Discovered }
    public enum RunTimeTemplateDataStatus { Hidden, Discovered, Used }

    public class RuntimeNodeData
    {
        public readonly string Id;
        public readonly NodeData r_NodeData;
        public RunTimeNodeStatus Status;
        public bool IsInvalidated = false;

        public RuntimeNodeData(string id, NodeData nodeData, RunTimeNodeStatus status)
        {
            Id = id;
            r_NodeData = nodeData;
            Status = status;
        }
    }

    public class RuntimeEntityItemData
    {
        public readonly string Id;
        public readonly EntityItem r_EntityItemData;
        public RunTimeEntityItemStatus Status;
        public RuntimeEntityItemData(string id, EntityItem data, RunTimeEntityItemStatus status) { Id = id; r_EntityItemData = data; Status = status; }
    }

    public class RuntimeTemplateData
    {
        /// <summary>
        /// template的id，跟alltemplate的字典id对应。对于节点专属template，使用$"nodeTemplate_{node.Id}"
        /// </summary>
        public readonly string Id;
        public readonly TemplateData r_TemplateData;
        public RunTimeTemplateDataStatus Status;
        public RuntimeTemplateData(string id, TemplateData data, RunTimeTemplateDataStatus status) { Id = id; r_TemplateData = data; Status = status; }
    }
}