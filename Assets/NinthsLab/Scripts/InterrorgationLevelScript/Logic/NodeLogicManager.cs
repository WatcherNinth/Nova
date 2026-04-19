using System.Collections.Generic;
using Interrorgation.MidLayer;
using LogicEngine.LevelGraph;
using Newtonsoft.Json.Linq;
using UnityEngine; // 用于 Debug.Log

namespace LogicEngine.LevelLogic
{
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
        public bool TryProveNode(string nodeId)
        {
            if (!_mindMapManager.TryGetNode(nodeId, out var runtimeNode)) return false;

            if (runtimeNode.Status == RunTimeNodeStatus.Submitted) return true;
            if (runtimeNode.Status != RunTimeNodeStatus.Discovered || runtimeNode.IsInvalidated) return false;

            // 检查依赖
            return runtimeNode.r_NodeData.Logic.GetDependOnResult();
        }

        

        public void OnProveFailed(string nodeId, bool isAutoResolve = false)
        {
            if (!_mindMapManager.TryGetNode(nodeId, out var runtimeNode)) return;

            // 只有当状态为已发现（但依赖不满足）时触发这些失败提醒
            if (runtimeNode.Status != RunTimeNodeStatus.Discovered || runtimeNode.IsInvalidated) return;

            if (!isAutoResolve)
            {
                TriggerDialogue(runtimeNode.r_NodeData.Dialogue?.OnPending);
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
        private void TriggerDialogue(JToken dialogueScript)
        {
            if (dialogueScript == null) return;
            var lines = DialogueParser.GetRuntimeDialogueList(dialogueScript);
            if (lines != null && lines.Count > 0)
            {
                GameEventDispatcher.DispatchDialogueGenerated(lines);
            }
        }

        // ========================================================================
        // [核心修改] 使用 NodeMutexGroupData 进行查表式互斥处理
        // ========================================================================
        public void ProcessMutex(string sourceNodeId, LogicEngine.Nodes.NodeLogicInfo logicInfo)
        {
            Debug.Log($"[NodeLogic] 处理互斥: {sourceNodeId}");

            var mutexData = _mindMapManager.levelGraph.nodeMutexGroupData;
            if (mutexData == null || mutexData.Data == null) return;

            // 遍历配置中所有的互斥组
            foreach (var kvp in mutexData.Data)
            {
                var mutexItems = kvp.Value;
                if (mutexItems == null) continue;

                // 查找当前 sourceNodeId 是否属于该互斥组的某一个条目
                NodeMutexItem ownerItem = null;
                foreach (var item in mutexItems)
                {
                    if (!item.IsGroupList && item.SingleNodeId == sourceNodeId)
                    {
                        ownerItem = item;
                        break;
                    }
                    else if (item.IsGroupList && item.GroupNodeIds != null && item.GroupNodeIds.Contains(sourceNodeId))
                    {
                        ownerItem = item;
                        break;
                    }
                }

                // 如果当前节点在该互斥组中，则把该组内**其他**条目的所有节点全部作废
                if (ownerItem != null)
                {
                    foreach (var item in mutexItems)
                    {
                        // 同一条目内部的节点不互斥（即当前节点组里的节点不互相互斥）
                        if (item == ownerItem) continue;

                        // 情况 A: 单个节点互斥
                        if (!item.IsGroupList && !string.IsNullOrEmpty(item.SingleNodeId))
                        {
                            InvalidateNode(item.SingleNodeId);
                        }
                        // 情况 B: 节点组互斥 (列表)
                        else if (item.IsGroupList && item.GroupNodeIds != null)
                        {
                            foreach (var targetId in item.GroupNodeIds)
                            {
                                InvalidateNode(targetId);
                            }
                        }
                    }
                }
            }
        }

        private void InvalidateNode(string nodeId)
        {
            // 尝试获取目标节点 (必须是已加载到 MindMap 的节点)
            if (_mindMapManager.TryGetNode(nodeId, out var node))
            {
                // 只有非 Submitted 的节点才会被作废
                // 已提交的节点通常是“赢家”，不能被作废
                if (node.Status != RunTimeNodeStatus.Submitted && !node.IsInvalidated)
                {
                    node.IsInvalidated = true;
                    Debug.Log($"[NodeLogic] 互斥生效：节点 {nodeId} 已被作废。");
                    GameEventDispatcher.DispatchNodeStatusChanged(node);
                }
            }
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
            TriggerDialogue(runtimeNode.r_NodeData.Dialogue?.OnProven);

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
            TriggerDialogue(runtimeNode.r_NodeData.Dialogue?.OnProven);

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
            TriggerDialogue(runtimeNode.r_NodeData.Dialogue?.OnProven);

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
            TriggerDialogue(runtimeNode.r_NodeData.Dialogue?.OnProven);

            // 互斥处理
            ProcessMutex(nodeId, runtimeNode.r_NodeData.Logic);

            return true;
        }
    }
}