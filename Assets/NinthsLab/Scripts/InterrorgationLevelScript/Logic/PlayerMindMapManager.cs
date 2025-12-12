using System.Collections.Generic;
using System.Linq;
using AIEngine.Network;
using LogicEngine.LevelGraph;

namespace LogicEngine.LevelLogic
{
    public class PlayerMindMapManager
    {
        readonly LevelGraphData levelGraph;

        // [AITask] 将 List 改为 Dictionary 以保留 ID 索引信息
        // Key: NodeId / KeywordID / TemplateKey
        public Dictionary<string, RuntimeNodeData> RunTimeNodeDataMap = new Dictionary<string, RuntimeNodeData>();

        public Dictionary<string, RuntimeEntityItemData> RunTimeEntityItemDataMap = new Dictionary<string, RuntimeEntityItemData>();

        public Dictionary<string, RuntimeTemplateData> RunTimeTemplateDataMap = new Dictionary<string, RuntimeTemplateData>();

        public PlayerMindMapManager(ref LevelGraphData inputLevelGraph)
        {
            levelGraph = inputLevelGraph;
            InitializeRuntimeData();
        }

        void InitializeRuntimeData()
        {
            // 1. 生成 RuntimeNodeDataMap
            // 依赖 LevelGraphData 中的 nodeLookup
            RunTimeNodeDataMap.Clear();
            if (levelGraph.nodeLookup != null)
            {
                foreach (var kvp in levelGraph.nodeLookup)
                {
                    string nodeId = kvp.Key;
                    var nodeInfo = kvp.Value;
                    
                    if (!string.IsNullOrEmpty(nodeId) && nodeInfo.Node != null)
                    {
                        // 将 ID 传入 RuntimeNodeData 保存
                        RunTimeNodeDataMap[nodeId] = new RuntimeNodeData(nodeId, nodeInfo.Node, RunTimeNodeStatus.Hidden);
                    }
                }
            }

            // 2. 生成 RunTimeTemplateDataMap
            // 依赖 LevelGraphData 中的 allTemplates
            RunTimeTemplateDataMap.Clear();
            if (levelGraph.allTemplates != null)
            {
                foreach (var kvp in levelGraph.allTemplates)
                {
                    string templateKey = kvp.Key;
                    var templateData = kvp.Value;

                    if (!string.IsNullOrEmpty(templateKey) && templateData != null)
                    {
                        RunTimeTemplateDataMap[templateKey] = new RuntimeTemplateData(templateKey, templateData, RunTimeTemplateDataStatus.Hidden);
                    }
                }
            }

            // 3. 生成 RunTimeEntityItemDataMap
            // 依赖 LevelGraphData 中的 entityListData.Data (Dictionary<string, EntityItem>)
            RunTimeEntityItemDataMap.Clear();
            if (levelGraph.entityListData != null && levelGraph.entityListData.Data != null)
            {
                foreach (var kvp in levelGraph.entityListData.Data)
                {
                    string keywordId = kvp.Key;
                    var entityItem = kvp.Value;

                    if (!string.IsNullOrEmpty(keywordId) && entityItem != null)
                    {
                        RunTimeEntityItemDataMap[keywordId] = new RuntimeEntityItemData(keywordId, entityItem, RunTimeEntityItemStatus.Hidden);
                    }
                }
            }
        }

        // --- Search Methods ---
        // 虽然存储变为 Dictionary，但按状态搜索通常是为了遍历处理，因此返回 List 是合理的。
        // ID 信息现在已包含在返回的 Runtime 对象内部。

        public List<RuntimeNodeData> GetRuntimeNodeDataByStatus(RunTimeNodeStatus status)
        {
            return RunTimeNodeDataMap.Values.Where(data => data.Status == status).ToList();
        }

        public List<RuntimeEntityItemData> GetRuntimeEntityItemDataByStatus(RunTimeEntityItemStatus status)
        {
            return RunTimeEntityItemDataMap.Values.Where(data => data.Status == status).ToList();
        }

        public List<RuntimeTemplateData> GetRuntimeTemplateDataByStatus(RunTimeTemplateDataStatus status)
        {
            return RunTimeTemplateDataMap.Values.Where(data => data.Status == status).ToList();
        }
        
        public void ProcessAIResponse(AIResponseData responseData)
        {
            // 处理AI响应数据
        }
    }

    // ==========================================
    // Runtime Data Classes
    // ==========================================

    public enum RunTimeNodeStatus
    {
        Hidden,
        Discovered,
        Submitted,
    }

    public class RuntimeNodeData
    {
        public readonly string Id; // 新增 ID 字段
        public readonly NodeData r_NodeData;
        public RunTimeNodeStatus Status;

        public RuntimeNodeData(string id, NodeData nodeData, RunTimeNodeStatus status)
        {
            this.Id = id;
            this.r_NodeData = nodeData;
            this.Status = status;
        }
    }

    public enum RunTimeEntityItemStatus
    {
        Hidden,
        Discovered,
    }

    public class RuntimeEntityItemData
    {
        public readonly string Id; // 新增 ID 字段 (对应 KeywordID)
        public readonly EntityItem r_EntityItemData;
        public RunTimeEntityItemStatus Status;

        public RuntimeEntityItemData(string id, EntityItem data, RunTimeEntityItemStatus status)
        {
            this.Id = id;
            this.r_EntityItemData = data;
            this.Status = status;
        }
    }

    public enum RunTimeTemplateDataStatus
    {
        Hidden,
        Discovered,
        Used,
    }

    public class RuntimeTemplateData
    {
        public readonly string Id; // 新增 ID 字段 (对应 TemplateKey)
        public readonly TemplateData r_TemplateData;
        public RunTimeTemplateDataStatus Status;

        public RuntimeTemplateData(string id, TemplateData data, RunTimeTemplateDataStatus status)
        {
            this.Id = id;
            this.r_TemplateData = data;
            this.Status = status;
        }
    }
}