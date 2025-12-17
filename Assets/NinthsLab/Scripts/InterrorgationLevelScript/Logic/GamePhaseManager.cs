using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using LogicEngine.LevelGraph;
using Interrorgation.MidLayer;

namespace LogicEngine.LevelLogic
{
    public class GamePhaseManager
    {
        private PlayerMindMapManager _mindMapManager;
        public Dictionary<string, RuntimePhaseStatus> RunTimePhaseStatusMap = new Dictionary<string, RuntimePhaseStatus>();

        public GamePhaseManager(PlayerMindMapManager mindMapManager)
        {
            _mindMapManager = mindMapManager;
            InitializePhases();
        }

        void InitializePhases()
        {
            RunTimePhaseStatusMap.Clear();
            if (_mindMapManager.levelGraph.phasesData != null)
            {
                foreach (var kvp in _mindMapManager.levelGraph.phasesData)
                {
                    RunTimePhaseStatusMap[kvp.Key] = RuntimePhaseStatus.Locked;
                }
            }
        }

        public void SetPhaseStatus(string phaseId, RuntimePhaseStatus status)
        {
            if (RunTimePhaseStatusMap.ContainsKey(phaseId))
            {
                RunTimePhaseStatusMap[phaseId] = status;
                GameEventDispatcher.DispatchPhaseStatusChanged(phaseId, status);
            }
        }

        public void CheckPhaseCompletion()
        {
            var activePhases = RunTimePhaseStatusMap.Where(x => x.Value == RuntimePhaseStatus.Active).Select(x => x.Key).ToList();

            foreach (var phaseId in activePhases)
            {
                if (!_mindMapManager.levelGraph.phasesData.TryGetValue(phaseId, out var phaseData)) continue;
                if (phaseData.CompletionNodes == null || phaseData.CompletionNodes.Count == 0) continue;

                bool isComplete = false;
                foreach (var targetId in phaseData.CompletionNodes)
                {
                    // 需要访问 MindMap 确认节点状态
                    if (_mindMapManager.TryGetNode(targetId, out var node) && node.Status == RunTimeNodeStatus.Submitted)
                    {
                        isComplete = true;
                        break;
                    }
                }

                if (isComplete)
                {
                    SetPhaseStatus(phaseId, RuntimePhaseStatus.Completed);

                    // 触发对话
                    if (phaseData.Dialogue?.OnPhaseComplete != null)
                    {
                        var lines = DialogueRuntimeHelper.GenerateDialogueLines(phaseData.Dialogue.OnPhaseComplete);
                        GameEventDispatcher.DispatchDialogueGenerated(lines);
                    }

                    // 计算解锁
                    var nextPhases = FindUnlockablePhases();
                    if (nextPhases.Count > 0)
                    {
                        GameEventDispatcher.DispatchPhaseUnlockEvents(phaseData.Name, nextPhases);
                    }
                }
            }
        }

        public List<(string id, string name)> FindUnlockablePhases()
        {
            var result = new List<(string id, string name)>();
            var completedPhaseIds = RunTimePhaseStatusMap
                .Where(x => x.Value == RuntimePhaseStatus.Completed)
                .Select(x => x.Key).ToList();

            foreach (var kvp in _mindMapManager.levelGraph.phasesData)
            {
                string pid = kvp.Key;
                var pData = kvp.Value;
                var currentStatus = RunTimePhaseStatusMap.ContainsKey(pid) ? RunTimePhaseStatusMap[pid] : RuntimePhaseStatus.Locked;

                if (currentStatus != RuntimePhaseStatus.Locked) continue;

                if (CheckPhaseDependencies(pData.DependsOn, completedPhaseIds))
                {
                    result.Add((pid, pData.Name));
                }
            }
            return result;
        }

        private bool CheckPhaseDependencies(JToken dependsOn, List<string> completedPhases)
        {
            if (dependsOn == null || !dependsOn.HasValues) return true;

            if (dependsOn.Type == JTokenType.String)
            {
                return completedPhases.Contains(dependsOn.ToString());
            }
            else if (dependsOn.Type == JTokenType.Object)
            {
                var obj = dependsOn as JObject;
                foreach (var prop in obj.Properties())
                {
                    string key = prop.Name.ToLower();
                    JToken value = prop.Value;

                    if (key == "or")
                    {
                        bool anyMet = false;
                        if (value is JObject orObj)
                        {
                            foreach (var p in orObj.Properties())
                                if (CheckSinglePhaseCondition(p.Name, p.Value, completedPhases)) { anyMet = true; break; }
                        }
                        if (!anyMet) return false;
                    }
                    else if (key == "and")
                    {
                        if (value is JObject andObj)
                        {
                            foreach (var p in andObj.Properties())
                                if (!CheckSinglePhaseCondition(p.Name, p.Value, completedPhases)) return false;
                        }
                    }
                    else
                    {
                        if (!CheckSinglePhaseCondition(key, value, completedPhases)) return false;
                    }
                }
                return true;
            }
            return true;
        }

        private bool CheckSinglePhaseCondition(string targetPhaseId, JToken expectedValueToken, List<string> completedPhases)
        {
            bool isCompleted = completedPhases.Contains(targetPhaseId);
            bool expected = true;
            try { expected = expectedValueToken.ToObject<bool>(); } catch { return false; }
            return isCompleted == expected;
        }
    }
}