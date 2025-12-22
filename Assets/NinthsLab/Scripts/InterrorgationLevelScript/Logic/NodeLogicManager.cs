using System.Collections.Generic;
using LogicEngine.LevelGraph;
using Interrorgation.MidLayer;
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
        public bool TryProveNode(string nodeId, bool isAutoResolve = false)
        {
            if (!_mindMapManager.TryGetNode(nodeId, out var runtimeNode)) return false;

            if (runtimeNode.Status == RunTimeNodeStatus.Submitted) return true;
            if (runtimeNode.Status != RunTimeNodeStatus.Discovered || runtimeNode.IsInvalidated) return false;

            // 检查依赖
            if (!CheckDependencies(runtimeNode.r_NodeData.Logic?.DependsOn))
            {
                // 只有玩家主动点击时，才触发“等待对话”和“Scope更新”
                // 如果是自动结算过程中发现不满足，就静默失败
                if (!isAutoResolve)
                {
                    TriggerDialogue(runtimeNode.r_NodeData.Dialogue?.OnPending);
                    
                    // [修改] 调用 ScopeManager 处理深度逻辑
                    _scopeManager?.UpdateScopeOnFail(nodeId);
                }
                return false;
            }

            // 成功逻辑
            _mindMapManager.SetNodeStatus(nodeId, RunTimeNodeStatus.Submitted);
            
            TriggerDialogue(runtimeNode.r_NodeData.Dialogue?.OnProven);
            ProcessMutex(nodeId, runtimeNode.r_NodeData.Logic);
            ProcessAutoVerify();
            _phaseManager?.CheckPhaseCompletion();

            // [新增] 成功后，通知 ScopeManager 尝试结算整个链条
            // 必须放在最后，否则递归可能出问题
            if (!isAutoResolve) 
            {
                _scopeManager?.ResolveScopeChain(nodeId);
            }

            return true;
        }
        
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
            if (logicInfo == null) return;
            
            string groupId = logicInfo.MutexGroup;
            if (string.IsNullOrEmpty(groupId)) return;

            // 1. 从 LevelGraphData 中直接获取互斥配置表
            // 这里不再遍历所有节点，而是直接查表，效率极高
            var mutexData = _mindMapManager.levelGraph.nodeMutexGroupData;
            var mutexItems = mutexData.GetMutexItems(groupId);

            if (mutexItems == null)
            {
                // 如果填了 MutexGroup 但没在 nodes_mutex_group 里定义，可能是配置错误
                // 但也可能是简单的单向互斥，这里可以选择 LogWarning 或忽略
                return; 
            }

            // 2. 遍历该组定义的所有条目
            foreach (var item in mutexItems)
            {
                // 情况 A: 单个节点互斥
                if (!string.IsNullOrEmpty(item.SingleNodeId))
                {
                    // 只要不是自己，就作废
                    if (item.SingleNodeId != sourceNodeId)
                    {
                        InvalidateNode(item.SingleNodeId);
                    }
                }

                // 情况 B: 节点组互斥 (列表)
                if (item.GroupNodeIds != null)
                {
                    foreach (var targetId in item.GroupNodeIds)
                    {
                        // 只要不是自己，就作废
                        if (targetId != sourceNodeId)
                        {
                            InvalidateNode(targetId);
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

        // ========================================================================

        private void ProcessAutoVerify()
        {
            bool hasChanged = true;
            while (hasChanged)
            {
                hasChanged = false;
                foreach (var kvp in _mindMapManager.RunTimeNodeDataMap)
                {
                    var node = kvp.Value;
                    if (node.Status == RunTimeNodeStatus.Discovered && !node.IsInvalidated &&
                        node.r_NodeData.Logic != null && node.r_NodeData.Logic.IsAutoVerified)
                    {
                        if (CheckDependencies(node.r_NodeData.Logic.DependsOn))
                        {
                            _mindMapManager.SetNodeStatus(node.Id, RunTimeNodeStatus.Submitted);
                            
                            // [修改] 传入 ID
                            ProcessMutex(node.Id, node.r_NodeData.Logic);
                            
                            hasChanged = true;
                        }
                    }
                }
            }
        }

        // --- Dependency Parsing (保持不变) ---

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
    }
}