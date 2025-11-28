using System.Collections.Generic;
using System.Linq; // 用于 Count 统计
using LogicEngine.Validation;
using UnityEngine;

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

        public Dictionary<string, TemplateData> specialTemplateData = new Dictionary<string, TemplateData>();

        // ========================================================================
        // 运行数据和生成 Region
        // ========================================================================
        #region 运行数据和生成

        /// <summary>
        /// 用于记录节点归属信息的辅助结构
        /// </summary>
        public struct NodeLocationInfo
        {
            /// <summary>
            /// 节点所属的 Phase ID。如果为 null 或 empty，则表示属于 Universal Nodes。
            /// </summary>
            public string OwnerPhaseId;

            /// <summary>
            /// 节点数据的引用
            /// </summary>
            public NodeData Node;

            /// <summary>
            /// 是否是全局节点
            /// </summary>
            public bool IsUniversal => string.IsNullOrEmpty(OwnerPhaseId);
        }

        /// <summary>
        /// 包含 UniversalNodes, Phases, 以及 Phases 下所有子 Node 的 ID。
        /// </summary>
        public List<string> allIds = new List<string>();

        /// <summary>
        /// [生成数据] 节点寻址字典。
        /// Key: NodeId
        /// Value: 包含节点引用和归属 Phase 的信息
        /// </summary>
        public Dictionary<string, NodeLocationInfo> nodeLookup = new Dictionary<string, NodeLocationInfo>();

        /// <summary>
        /// [生成数据] 关卡内使用的所有 Template 列表。
        /// </summary>
        public Dictionary<string, TemplateData> allTemplates = new Dictionary<string, TemplateData>();

        /// <summary>
        /// 刷新节点查找字典 (Task 1)
        /// 遍历 UniversalNodes 和 Phases，建立 ID 到节点位置的映射。
        /// </summary>
        public void RefreshNodeLookup()
        {
            nodeLookup.Clear();

            // 1. 录入 Universal Nodes (OwnerPhaseId 为 null)
            if (universalNodesData != null)
            {
                foreach (var kvp in universalNodesData)
                {
                    if (string.IsNullOrEmpty(kvp.Key)) continue;

                    var info = new NodeLocationInfo
                    {
                        OwnerPhaseId = null, // 表示 Universal
                        Node = kvp.Value
                    };

                    // 因为 OnValidate 已经保证了 ID 唯一性，这里可以直接赋值
                    nodeLookup[kvp.Key] = info;
                }
            }

            // 2. 录入 Phase Nodes
            if (phasesData != null)
            {
                foreach (var phaseKvp in phasesData)
                {
                    string phaseId = phaseKvp.Key;
                    PhaseData phase = phaseKvp.Value;

                    if (phase == null || phase.Nodes == null) continue;

                    foreach (var nodeKvp in phase.Nodes)
                    {
                        if (string.IsNullOrEmpty(nodeKvp.Key)) continue;

                        var info = new NodeLocationInfo
                        {
                            OwnerPhaseId = phaseId,
                            Node = nodeKvp.Value
                        };

                        nodeLookup[nodeKvp.Key] = info;
                    }
                }
            }
        }

        /// <summary>
        /// 刷新模板列表 (Task 2)
        /// 依赖于 nodeLookup，遍历所有节点并提取 TemplateData。
        /// </summary>
        public void RefreshTemplateList()
        {
            allTemplates.Clear();

            // 遍历刚刚生成的查找表，这样就不用再写两遍循环了
            foreach (var info in nodeLookup.Values)
            {
                NodeData node = info.Node;
                if (node == null) continue;

                // 根据要求：通过 Template 类下面的 Template 属性获取
                // 这里使用了空值传播符 (?.) 防止空引用报错
                if (node.Template != null && node.Template.Template != null)
                {
                    allTemplates.Add($"nodeTemplate_{node.Id}", node.Template.Template);
                }
            }
            foreach (var specTemplate in specialTemplateData)
            {
                Debug.Log(specTemplate.Key);
                allTemplates.Add(specTemplate.Key, specTemplate.Value);
            }
        }

        /// <summary>
        /// 供外部调用的初始化方法。
        /// </summary>
        public void InitializeRuntimeData()
        {
            RefreshNodeLookup();
            RefreshTemplateList();
        }

        #endregion
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