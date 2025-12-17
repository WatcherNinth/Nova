using System.Collections.Generic;
using LogicEngine.LevelGraph;
using Interrorgation.MidLayer;
using Newtonsoft.Json.Linq;

namespace LogicEngine.LevelLogic
{
    public class NodeLogicManager
    {
        private PlayerMindMapManager _mindMapManager;
        private GamePhaseManager _phaseManager;

        public NodeLogicManager(PlayerMindMapManager mindMapManager)
        {
            _mindMapManager = mindMapManager;
        }

        // 需要在 PhaseManager 创建后注入
        public void SetPhaseManager(GamePhaseManager phaseManager)
        {
            _phaseManager = phaseManager;
        }

        public bool TryProveNode(string nodeId)
        {
            if (!_mindMapManager.TryGetNode(nodeId, out var runtimeNode)) return false;

            if (runtimeNode.Status == RunTimeNodeStatus.Submitted) return true;
            if (runtimeNode.Status != RunTimeNodeStatus.Discovered || runtimeNode.IsInvalidated) return false;

            // 检查依赖
            if (!CheckDependencies(runtimeNode.r_NodeData.Logic?.DependsOn))
            {
                TriggerDialogue(runtimeNode.r_NodeData.Dialogue?.OnPending);
                return false;
            }

            // 状态变更
            _mindMapManager.SetNodeStatus(nodeId, RunTimeNodeStatus.Submitted);

            // 触发对话
            TriggerDialogue(runtimeNode.r_NodeData.Dialogue?.OnProven);

            // 后续逻辑
            ProcessMutex(runtimeNode.r_NodeData.Logic);
            ProcessAutoVerify();
            
            // 通知阶段管理器检查完成情况
            _phaseManager?.CheckPhaseCompletion();

            return true;
        }

        private void TriggerDialogue(JToken dialogueScript)
        {
            if (dialogueScript == null) return;
            var lines = DialogueRuntimeHelper.GenerateDialogueLines(dialogueScript);
            if (lines != null && lines.Count > 0)
            {
                GameEventDispatcher.DispatchDialogueGenerated(lines);
            }
        }

        public void ProcessMutex(LogicEngine.Nodes.NodeLogicInfo logicInfo)
        {
            if (logicInfo == null) return;
            string mutexGroup = logicInfo.MutexGroup;
            if (string.IsNullOrEmpty(mutexGroup)) return;

            foreach (var kvp in _mindMapManager.RunTimeNodeDataMap)
            {
                var targetNode = kvp.Value;
                if (targetNode.Id == logicInfo.ToString()) continue; // 需修正：这里逻辑上应该是排除触发互斥的源节点

                if (targetNode.r_NodeData.Logic != null && targetNode.r_NodeData.Logic.MutexGroup == mutexGroup)
                {
                    InvalidateNode(targetNode.Id);
                }
            }
        }

        private void InvalidateNode(string nodeId)
        {
            if (_mindMapManager.TryGetNode(nodeId, out var node))
            {
                if (node.Status != RunTimeNodeStatus.Submitted)
                {
                    node.IsInvalidated = true;
                    GameEventDispatcher.DispatchNodeStatusChanged(node);
                }
            }
        }

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
                            ProcessMutex(node.r_NodeData.Logic);
                            hasChanged = true;
                        }
                    }
                }
            }
        }

        // --- Dependency Parsing ---

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