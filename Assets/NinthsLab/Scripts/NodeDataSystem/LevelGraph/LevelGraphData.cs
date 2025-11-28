using System.Collections.Generic;
using Newtonsoft.Json;
using LogicEngine.Validation;

namespace LogicEngine.LevelGraph
{
    public class LevelGraphData : IValidatable
    {
        public Dictionary<string, NodeData> universalNodesData;
        public Dictionary<string, PhaseData> phasesData;
        public NodeChoiceGroupData nodeChoiceGroupData;
        public NodeMutexGroupData nodeMutexGroupData;
        public EntityListData entityListData;

        public void OnValidate(ValidationContext context)
        {
            // 校验逻辑
        }
    }
}