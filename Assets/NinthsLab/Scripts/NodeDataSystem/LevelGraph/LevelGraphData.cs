using System.Collections.Generic;
using System.Linq; // 用于 Count 统计
using LogicEngine.Validation;

namespace LogicEngine.LevelGraph
{
    public class LevelGraphData : IValidatable
    {
        // ==========================================
        // 数据字段
        // ==========================================
        public Dictionary<string, NodeData> universalNodesData = new Dictionary<string, NodeData>();
        public Dictionary<string, PhaseData> phasesData = new Dictionary<string, PhaseData>();
        
        public NodeChoiceGroupData nodeChoiceGroupData = new NodeChoiceGroupData();
        public NodeMutexGroupData nodeMutexGroupData = new NodeMutexGroupData();
        public EntityListData entityListData = new EntityListData();

        /// <summary>
        /// 包含 UniversalNodes, Phases, 以及 Phases 下所有子 Node 的 ID。
        /// </summary>
        public List<string> allIds = new List<string>();

        // ==========================================
        // 数据验证逻辑
        // ==========================================
        #region 数据验证

        public void OnValidate(ValidationContext context)
        {
            context.LogInfo("=== 开始 LevelGraphData 数据验证流程 ===");

            // 1. 执行 ID 收集与查重 (独立函数)
            CheckAndCollectIds(context);

            // 2. 递归验证 Universal Nodes
            if (universalNodesData != null)
            {
                foreach (var kvp in universalNodesData)
                {
                    // 参数：名称，对象，是否允许为空(false)
                    context.ValidateChild($"UniversalNode_{kvp.Key}", kvp.Value, false);
                }
            }

            // 3. 递归验证 Phases
            if (phasesData != null)
            {
                foreach (var kvp in phasesData)
                {
                    // Phase 内部的 Node 验证逻辑由 PhaseData.OnValidate 负责，这里只负责进入 Phase
                    context.ValidateChild($"Phase_{kvp.Key}", kvp.Value, false);
                }
            }

            // 4. 验证其他子结构
            context.ValidateChild("NodeChoiceGroups", nodeChoiceGroupData, false);
            context.ValidateChild("NodeMutexGroups", nodeMutexGroupData, false);
            context.ValidateChild("EntityList", entityListData, false);

            // 5. 统计并输出总结信息
            var result = context.Result;
            int errorCount = result.Entries.Count(e => e.Severity == ValidationSeverity.Error);
            int warningCount = result.Entries.Count(e => e.Severity == ValidationSeverity.Warning);

            context.LogInfo($"=== LevelGraphData 验证结束: 发现 {errorCount} 个错误, {warningCount} 个警告 ===");
        }

        /// <summary>
        /// 收集所有 ID 并检查重复项
        /// </summary>
        private void CheckAndCollectIds(ValidationContext context)
        {
            // 清空列表
            allIds.Clear();

            // 临时字典：<ID, 首次出现的来源描述>
            Dictionary<string, string> idSourceMap = new Dictionary<string, string>();

            // 局部帮助函数
            void TryRegisterId(string id, string currentSource)
            {
                if (string.IsNullOrEmpty(id)) return;

                if (idSourceMap.TryGetValue(id, out string previousSource))
                {
                    context.LogError($"检测到重复 ID: '{id}'。当前来源 [{currentSource}]，但该 ID 已在 [{previousSource}] 中定义。");
                }
                else
                {
                    idSourceMap.Add(id, currentSource);
                    allIds.Add(id);
                }
            }

            // A. 收集 Universal Nodes ID
            if (universalNodesData != null)
            {
                foreach (var id in universalNodesData.Keys)
                {
                    TryRegisterId(id, "Universal Nodes");
                }
            }

            // B. 收集 Phases ID 及其子 Node ID
            if (phasesData != null)
            {
                foreach (var phasePair in phasesData)
                {
                    string phaseId = phasePair.Key;
                    PhaseData phase = phasePair.Value;

                    // 1. Phase 自身的 ID
                    TryRegisterId(phaseId, "Phase Keys");

                    // 2. Phase 内部子节点的 ID (虽然 Phase 内部逻辑不在这里验，但 ID 全局唯一性必须在这里验)
                    if (phase != null && phase.Nodes != null)
                    {
                        foreach (var subNodeId in phase.Nodes.Keys)
                        {
                            TryRegisterId(subNodeId, $"Phase '{phaseId}' Sub-Nodes");
                        }
                    }
                }
            }
        }

        #endregion
    }
}