using System.Collections.Generic;
using System.Linq;
using DialogueSystem;
using Interrorgation.MidLayer;
using LogicEngine.LevelGraph;
using Newtonsoft.Json.Linq;
using UnityEngine; // 用于 Debug.Log

namespace LogicEngine.LevelLogic
{
    public enum ProofResult
    {
        NotFound,
        Submitted,
        NotDiscovered,
        Invalidated,
        NodeMutex,
        Regular
    }

    public class NodeLogicManager
    {
        private PlayerMindMapManager _mindMapManager;
        private GamePhaseManager _phaseManager;
        private GameScopeManager _scopeManager;

        public NodeLogicManager(PlayerMindMapManager mindMapManager)
        {
            _mindMapManager = mindMapManager;
        }

        public void SetPhaseManager(GamePhaseManager phaseManager)
        {
            _phaseManager = phaseManager;
        }

        public void SetScopeManager(GameScopeManager scopeManager)
        {
            _scopeManager = scopeManager;
        }
        public bool TryProveNode(string nodeId, out ProofResult result)
        {
            if (!_mindMapManager.TryGetNode(nodeId, out var runtimeNode))
            {
                result = ProofResult.NotFound;
                return false;
            }

            if (runtimeNode.Status == RunTimeNodeStatus.Submitted)
            {
                result = ProofResult.Submitted;
                return true;
            }

            if (runtimeNode.Status != RunTimeNodeStatus.Discovered)
            {
                result = ProofResult.NotDiscovered;
                return false;
            }

            if (runtimeNode.IsInvalidated)
            {
                result = ProofResult.Invalidated;
                return false;
            }

            if (CheckNodeMutex(nodeId, _scopeManager.GetCurrentScopeNode()))
            {
                result = ProofResult.NodeMutex;
                return false;
            }

            result = ProofResult.Regular;
            return runtimeNode.r_NodeData.Logic.GetDependOnResult();
        }

        

        public void OnProveFailed(string nodeId, bool isAutoResolve = false)
        {
            if (!_mindMapManager.TryGetNode(nodeId, out var runtimeNode)) return;

            // 只有当状态为已发现（但依赖不满足）时触发这些失败提醒
            if (runtimeNode.Status != RunTimeNodeStatus.Discovered || runtimeNode.IsInvalidated) return;

            if (!isAutoResolve)
            {
                TriggerDialogue(runtimeNode.r_NodeData.Dialogue?.OnPending, nodeId, "on_pending");
                if (runtimeNode.r_NodeData.Dialogue == null || runtimeNode.r_NodeData.Dialogue.OnPending == null)
                {
                    Debug.Log($"[NodeLogic] 依赖未满足，节点 {nodeId} 无法被证明，但没有待定对话。");
                }

                // [修改] 调用 ScopeManager 处理深度逻辑
                _scopeManager?.UpdateScopeOnFail(nodeId);
            }
        }


        /// <summary>
        /// 发起对话。
        /// 注意！这个方法会立刻调用UISequence，注意调用顺序！
        /// </summary>
        /// <param name="dialogueScript"></param>
        /// <param name="nodeId"></param>
        private void TriggerDialogue(JToken dialogueScript, string nodeId, string dialogueKey)
        {
            if (dialogueScript == null) return;
            var lines = DialogueParser.GetRuntimeDialogueList(dialogueScript);
            if (lines != null && lines.Count > 0)
            {
                var source = new DialogueSource(DialogueOwnerType.Node, nodeId, dialogueKey);
                GameEventDispatcher.DispatchDialogueGenerated(lines, source);
            }
        }

        // ========================================================================
        // [核心修改] 使用统一的 CheckNodeMutex 方法进行互斥处理
        // ========================================================================
        public void ProcessMutex(string sourceNodeId, LogicEngine.Nodes.NodeLogicInfo logicInfo)
        {
            Debug.Log($"[NodeLogic] 处理互斥: {sourceNodeId}");

            // 遍历所有运行时节点
            foreach (var kvp in _mindMapManager.RunTimeNodeDataMap)
            {
                string candidateNodeId = kvp.Key;

                // 跳过源节点本身
                if (candidateNodeId == sourceNodeId) continue;

                // 使用统一的方法检查是否互斥
                if (CheckNodeMutex(sourceNodeId, candidateNodeId))
                {
                    InvalidateNode(candidateNodeId);
                }
            }
        }

        private void InvalidateNode(string nodeId)
        {
            // 尝试获取目标节点 (必须是已加载到 MindMap 的节点)
            if (_mindMapManager.TryGetNode(nodeId, out var node))
            {
                // 只有非 Submitted 的节点才会被作废
                // 已提交的节点通常是"赢家"，不能被作废
                if (node.Status != RunTimeNodeStatus.Submitted && !node.IsInvalidated)
                {
                    node.IsInvalidated = true;
                    Debug.Log($"[NodeLogic] 互斥生效：节点 {nodeId} 已被作废。");
                    GameEventDispatcher.DispatchNodeStatusChanged(node);
                }
            }
        }

        /// <summary>
        /// 检查两个节点是否互斥
        /// </summary>
        private bool CheckNodeMutex(string nodeIdA, string nodeIdB)
        {
            // 边界情况处理
            if (string.IsNullOrEmpty(nodeIdA) || string.IsNullOrEmpty(nodeIdB)) return false;
            if (nodeIdA == nodeIdB) return false;
            if (!_mindMapManager.TryGetNode(nodeIdA, out var nodeA)) return false;
            if (!_mindMapManager.TryGetNode(nodeIdB, out var nodeB)) return false;

            // 检查 Mutex Groups
            var mutexData = _mindMapManager.levelGraph.nodeMutexGroupData;
            if (mutexData != null && mutexData.Data != null)
            {
                foreach (var kvp in mutexData.Data)
                {
                    var mutexItems = kvp.Value;
                    if (mutexItems == null) continue;

                    NodeMutexItem itemA = null;
                    NodeMutexItem itemB = null;

                    // 找到两个节点所在的条目
                    foreach (var item in mutexItems)
                    {
                        if (!item.IsGroupList && item.SingleNodeId == nodeIdA)
                        {
                            itemA = item;
                        }
                        else if (item.IsGroupList && item.GroupNodeIds != null && item.GroupNodeIds.Contains(nodeIdA))
                        {
                            itemA = item;
                        }

                        if (!item.IsGroupList && item.SingleNodeId == nodeIdB)
                        {
                            itemB = item;
                        }
                        else if (item.IsGroupList && item.GroupNodeIds != null && item.GroupNodeIds.Contains(nodeIdB))
                        {
                            itemB = item;
                        }
                    }

                    // 如果两个节点都在同一个互斥组的不同条目中，则互斥
                    if (itemA != null && itemB != null && itemA != itemB)
                    {
                        return true;
                    }
                }
            }

            // 检查 ExtraMutexList (双向检查)
            if (nodeA.r_NodeData.Logic != null && nodeA.r_NodeData.Logic.ExtraMutexList != null)
            {
                if (nodeA.r_NodeData.Logic.ExtraMutexList.Contains(nodeIdB)) return true;
            }
            if (nodeB.r_NodeData.Logic != null && nodeB.r_NodeData.Logic.ExtraMutexList != null)
            {
                if (nodeB.r_NodeData.Logic.ExtraMutexList.Contains(nodeIdA)) return true;
            }

            // 检查 GeneratedMutexNodes (双向检查)
            if (nodeA.r_NodeData.Logic != null && nodeA.r_NodeData.Logic.GeneratedMutexNodes != null)
            {
                if (nodeA.r_NodeData.Logic.GeneratedMutexNodes.Contains(nodeIdB)) return true;
            }
            if (nodeB.r_NodeData.Logic != null && nodeB.r_NodeData.Logic.GeneratedMutexNodes != null)
            {
                if (nodeB.r_NodeData.Logic.GeneratedMutexNodes.Contains(nodeIdA)) return true;
            }

            return false;
        }

        /// <summary>
        /// 检查两个节点是否互斥 (公共接口，供 GameEvent 调用)
        /// </summary>
        public bool CheckNodeMutexPublic(string nodeIdA, string nodeIdB)
        {
            return CheckNodeMutex(nodeIdA, nodeIdB);
        }

        

        // --- Dependency Parsing (保持不变) ---
/*
        private bool CheckDependencies(JToken dependsOn)
        {
            if (dependsOn == null || !dependsOn.HasValues) return true;

            if (dependsOn.Type == JTokenType.String)
            {
                return IsNodeProven(dependsOn.ToString());
            }
            else if (dependsOn.Type == JTokenType.Array)
            {
                foreach (var child in dependsOn) if (!CheckDependencies(child)) return false;
                return true;
            }
            else if (dependsOn.Type == JTokenType.Object)
            {
                var obj = dependsOn as JObject;
                foreach (var prop in obj.Properties())
                {
                    string key = prop.Name.ToLower();
                    if (key == "or")
                    {
                        bool anyMet = false;
                        if (prop.Value is JObject orObj)
                        {
                            foreach (var p in orObj.Properties())
                                if (CheckSingleCondition(p.Name, p.Value)) { anyMet = true; break; }
                        }
                        if (!anyMet) return false;
                    }
                    else if (key == "and")
                    {
                        if (prop.Value is JObject andObj)
                        {
                            foreach (var p in andObj.Properties())
                                if (!CheckSingleCondition(p.Name, p.Value)) return false;
                        }
                    }
                    else
                    {
                        if (!CheckSingleCondition(key, prop.Value)) return false;
                    }
                }
                return true;
            }
            return true;
        }

        private bool CheckSingleCondition(string nodeId, JToken expectedValue)
        {
            bool isProven = IsNodeProven(nodeId);
            bool expected = expectedValue.ToObject<bool>();
            return isProven == expected;
        }

        private bool IsNodeProven(string nodeId)
        {
            if (_mindMapManager.TryGetNode(nodeId, out var node))
            {
                return node.Status == RunTimeNodeStatus.Submitted;
            }
            return false;
        }
        */

        /// <summary>
        /// 执行完整的节点证明流程（主证明 + 所有衍生证明）
        /// </summary>
        public void ExecuteFullProofFlow(string nodeId, bool isDerived)
        {
            // 步骤 1: 软依赖证明（仅在主证明时执行）
            if (!isDerived)
            {
                ProcessSoftDependencyProofs(nodeId);
            }

            // 步骤 2: 主证明流程
            ProcessMainProof(nodeId, isDerived);

            // 步骤 3: Scope 证明（仅在主证明时执行）
            if (!isDerived)
            {
                ProcessScopeProof(nodeId);
            }

            // 步骤 4: AutoVerified 证明（仅在主证明时执行）
            if (!isDerived)
            {
                ProcessAutoVerifiedProof();
            }
        }

        /// <summary>
        /// 执行单个节点的主证明逻辑
        /// </summary>
        private void ProcessMainProof(string nodeId, bool isDerived)
        {
            if (!_mindMapManager.TryGetNode(nodeId, out var runtimeNode)) return;
            if (runtimeNode.Status == RunTimeNodeStatus.Submitted) return;

            // 发送主证明开始事件
            GameEventDispatcher.DispatchMainProofStarted(nodeId);

            // 设置状态
            _mindMapManager.SetNodeStatus(nodeId, RunTimeNodeStatus.Submitted);

            // 触发 OnProven 对话
            TriggerDialogue(runtimeNode.r_NodeData.Dialogue?.OnProven, nodeId, "on_proven");

            // 互斥处理（所有证明都需要）
            ProcessMutex(nodeId, runtimeNode.r_NodeData.Logic);

            // 阶段检查
            _phaseManager?.CheckPhaseCompletion();
        }

        /// <summary>
        /// 处理软依赖证明（发生在主证明之前）
        /// 使用深度优先搜索逐个证明
        /// </summary>
        private void ProcessSoftDependencyProofs(string nodeId)
        {
            if (!_mindMapManager.TryGetNode(nodeId, out var runtimeNode)) return;

            var softDependencies = runtimeNode.r_NodeData.Logic.SoftDependOn;
            if (softDependencies == null || softDependencies.Count == 0) return;

            // 深度优先递归证明软依赖
            foreach (var softNodeId in softDependencies)
            {
                ProveNodeWithSoftDependencies(softNodeId, visited: new HashSet<string>());
            }
        }

        /// <summary>
        /// 证明节点及其软依赖（深度优先）
        /// </summary>
        private void ProveNodeWithSoftDependencies(string nodeId, HashSet<string> visited)
        {
            // 防止循环依赖
            if (visited.Contains(nodeId)) return;
            visited.Add(nodeId);

            if (!_mindMapManager.TryGetNode(nodeId, out var runtimeNode)) return;

            // 检查是否已经提交
            if (runtimeNode.Status == RunTimeNodeStatus.Submitted) return;

            // 深度优先：先证明软依赖
            var softDependencies = runtimeNode.r_NodeData.Logic.SoftDependOn;
            if (softDependencies != null && softDependencies.Count > 0)
            {
                foreach (var softNodeId in softDependencies)
                {
                    ProveNodeWithSoftDependencies(softNodeId, visited);
                }
            }

            // 检查依赖是否满足
            if (!runtimeNode.r_NodeData.Logic.GetDependOnResult()) return;

            // 发送衍生证明开始事件（仅在成功时）
            GameEventDispatcher.DispatchDeriveProofStarted(
                GameEventDispatcher.DeriveProofType.SoftDependency, nodeId
            );

            // 设置状态
            _mindMapManager.SetNodeStatus(nodeId, RunTimeNodeStatus.Submitted);

            // 触发 OnProven 对话
            TriggerDialogue(runtimeNode.r_NodeData.Dialogue?.OnProven, nodeId, "on_proven");

            // 互斥处理
            ProcessMutex(nodeId, runtimeNode.r_NodeData.Logic);
        }

        /// <summary>
        /// 处理 Scope 证明（发生在主证明之后）
        /// </summary>
        private void ProcessScopeProof(string nodeId)
        {
            if (_scopeManager == null) return;

            // 使用新的证明方法执行 Scope 结算
            _scopeManager.ResolveScopeChainWithProof(nodeId, ProveNodeForScope);
        }

        /// <summary>
        /// 为 Scope 证明节点（用于衍生证明）
        /// </summary>
        private bool ProveNodeForScope(string nodeId)
        {
            if (!_mindMapManager.TryGetNode(nodeId, out var runtimeNode)) return false;

            // 检查依赖
            if (!runtimeNode.r_NodeData.Logic.GetDependOnResult()) return false;

            // 检查状态
            if (runtimeNode.Status != RunTimeNodeStatus.Discovered || runtimeNode.IsInvalidated) return false;

            // 发送衍生证明开始事件（仅在成功时）
            GameEventDispatcher.DispatchDeriveProofStarted(
                GameEventDispatcher.DeriveProofType.Scope, nodeId
            );

            // 设置状态
            _mindMapManager.SetNodeStatus(nodeId, RunTimeNodeStatus.Submitted);

            // 触发 OnProven 对话
            TriggerDialogue(runtimeNode.r_NodeData.Dialogue?.OnProven, nodeId, "on_proven");

            // 互斥处理
            ProcessMutex(nodeId, runtimeNode.r_NodeData.Logic);

            return true;
        }

        /// <summary>
        /// 处理 AutoVerified 证明（发生在 Scope 证明之后）
        /// </summary>
        private void ProcessAutoVerifiedProof()
        {
            bool hasChanged = true;
            while (hasChanged)
            {
                hasChanged = false;
                foreach (var kvp in _mindMapManager.RunTimeNodeDataMap)
                {
                    var node = kvp.Value;
                    if (node.Status == RunTimeNodeStatus.Discovered &&
                        !node.IsInvalidated &&
                        node.r_NodeData.Logic != null &&
                        node.r_NodeData.Logic.IsAutoVerified)
                    {

                        if (ProveNodeForAutoVerified(node.Id))
                        {
                            hasChanged = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 为 AutoVerified 证明节点
        /// </summary>
        private bool ProveNodeForAutoVerified(string nodeId)
        {
            if (!_mindMapManager.TryGetNode(nodeId, out var runtimeNode)) return false;

            // 检查依赖
            if (!runtimeNode.r_NodeData.Logic.GetDependOnResult()) return false;

            // 检查状态
            if (runtimeNode.Status != RunTimeNodeStatus.Discovered || runtimeNode.IsInvalidated) return false;

            // 发送衍生证明开始事件（仅在成功时）
            GameEventDispatcher.DispatchDeriveProofStarted(
                GameEventDispatcher.DeriveProofType.AutoVerified, nodeId
            );

            // 设置状态
            _mindMapManager.SetNodeStatus(nodeId, RunTimeNodeStatus.Submitted);

            // 触发 OnProven 对话
            TriggerDialogue(runtimeNode.r_NodeData.Dialogue?.OnProven, nodeId, "on_proven");

            // 互斥处理
            ProcessMutex(nodeId, runtimeNode.r_NodeData.Logic);

            return true;
        }
    }
}