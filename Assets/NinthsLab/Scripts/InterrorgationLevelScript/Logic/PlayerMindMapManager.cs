using System.Collections.Generic;
using AIEngine.Network;
using LogicEngine.LevelGraph;


namespace LogicEngine.LevelLogic
{
    public class PlayerMindMapManager
    {
        readonly LevelGraphData levelGraph;

        public List<RuntimeNodeData> RunTimeNodeDataList = new List<RuntimeNodeData>();

        public List<RuntimeEntityItemData> RunTimeEntityItemDataList = new List<RuntimeEntityItemData>();

        public List<RuntimeTemplateData> RunTimeTemplateDataList = new List<RuntimeTemplateData>();


        public PlayerMindMapManager(ref LevelGraphData inputLevelGraph)
        {
            levelGraph = inputLevelGraph;
        }

        void InitializeRuntimeData()
        {
            //[AITask]根据LevelGraphData生成RuntimeNodeDataList, RunTimeEntityItemDataList, RunTimeTemplateDataList，默认状态为Hidden
        }

        //[AITask]参考下面的GetRuntimeNodeDataByStatus结构，完成三个数据结构按status搜索的方法
        public List<RuntimeNodeData> GetRuntimeNodeDataByStatus(RunTimeNodeStatus runTimeNodeStatus)
        {
            return null;
        }
        

        public void ProcessAIResponse(AIResponseData responseData)
        {
            // 处理AI响应数据
        }


    }

    public enum RunTimeNodeStatus
    {
        Hidden,
        Discovered,
        Submitted,
    }
    public class RuntimeNodeData
    {
        public readonly NodeData r_NodeData;
        public RunTimeNodeStatus Status;
        public RuntimeNodeData(NodeData nodeData, RunTimeNodeStatus status)
        {
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
        public readonly EntityItem r_EntityItemData;
        public RunTimeEntityItemStatus Status;
        public RuntimeEntityItemData(EntityItem Data, RunTimeEntityItemStatus status)
        {
            this.r_EntityItemData = Data;
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
        public readonly TemplateData r_TemplateData;
        public RunTimeTemplateDataStatus Status;
        public RuntimeTemplateData(TemplateData Data, RunTimeTemplateDataStatus status)
        {
            this.r_TemplateData = Data;
            this.Status = status;
        }
    }
}