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

        // --- Discovery Logic (AI 响应处理) ---

        public void ProcessAIResponse(AIResponseData responseData)
        {
            if (responseData == null) return;

            var result = responseData.RefereeResult;
            if (result != null)
            {
                if (result.PassedNodeIds != null && result.PassedNodeIds.Count > 0) discoverNodes(result.PassedNodeIds);
                if (result.EntityList != null && result.EntityList.Count > 0) discoverEntity(result.EntityList);
            }

            if (responseData.DiscoveryResult != null && responseData.DiscoveryResult.DiscoveredNodeIds.Count > 0)
            {
                var ids = responseData.DiscoveryResult.DiscoveredNodeIds;

                // 尝试作为模板发现
                List<string> templatesToUnlock = new List<string>();
                foreach (var nodeId in ids)
                {
                    if (levelGraph.nodeLookup.TryGetValue(nodeId, out var nodeInfo) && nodeInfo.Node != null)
                    {
                        string specialTmplId = nodeInfo.Node.Template?.SpecialTemplateId;
                        if (!string.IsNullOrEmpty(specialTmplId)) templatesToUnlock.Add(specialTmplId);
                        else if (nodeInfo.Node.Template?.Template != null) templatesToUnlock.Add($"nodeTemplate_{nodeId}");
                    }
                }
                if (templatesToUnlock.Count > 0) discoverTemplate(templatesToUnlock);
            }
        }

        void discoverNodes(List<string> nodeIds)
        {
            List<RuntimeNodeData> newlyDiscovered = new List<RuntimeNodeData>();
            foreach (var id in nodeIds)
            {
                if (RunTimeNodeDataMap.TryGetValue(id, out var node) && node.Status == RunTimeNodeStatus.Hidden)
                {
                    node.Status = RunTimeNodeStatus.Discovered;
                    newlyDiscovered.Add(node);
                }
            }
            if (newlyDiscovered.Count > 0)
            {
                var context = new GameEventDispatcher.NodeDiscoverContext(GameEventDispatcher.NodeDiscoverContext.e_DiscoverNewNodeMethod.PlayerInput);
                GameEventDispatcher.DispatchDiscoveredNewNodes(newlyDiscovered, context);
            }
        }

        void discoverEntity(List<string> entityIds)
        {
            List<RuntimeEntityItemData> newlyDiscovered = new List<RuntimeEntityItemData>();
            foreach (var id in entityIds)
            {
                if (RunTimeEntityItemDataMap.TryGetValue(id, out var entity) && entity.Status == RunTimeEntityItemStatus.Hidden)
                {
                    entity.Status = RunTimeEntityItemStatus.Discovered;
                    newlyDiscovered.Add(entity);
                }
            }
            if (newlyDiscovered.Count > 0) GameEventDispatcher.DispatchDiscoveredNewEntityItems(newlyDiscovered);
        }

        void discoverTemplate(List<string> templateIds)
        {
            List<RuntimeTemplateData> newlyDiscovered = new List<RuntimeTemplateData>();
            foreach (var id in templateIds)
            {
                if (RunTimeTemplateDataMap.TryGetValue(id, out var tmpl) && tmpl.Status == RunTimeTemplateDataStatus.Hidden)
                {
                    tmpl.Status = RunTimeTemplateDataStatus.Discovered;
                    newlyDiscovered.Add(tmpl);
                }
            }
            if (newlyDiscovered.Count > 0) GameEventDispatcher.DispatchDiscoveredNewTemplates(newlyDiscovered);
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
    
    public enum RunTimeNodeStatus { Hidden, Discovered, Submitted, Invalidated }
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