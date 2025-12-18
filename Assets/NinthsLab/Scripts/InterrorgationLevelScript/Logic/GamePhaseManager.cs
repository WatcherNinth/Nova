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
                // 每次状态变动都刷新可用列表 (UI 侧边栏按钮用)
                BroadcastAvailablePhases(); 
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

        // [新增] 核心切换逻辑
        public bool SwitchToPhase(string targetPhaseId)
        {
            // 1. 校验目标是否存在
            if (!_mindMapManager.levelGraph.phasesData.ContainsKey(targetPhaseId))
            {
                UnityEngine.Debug.LogError($"[PhaseManager] 试图切换到不存在的阶段: {targetPhaseId}");
                return false;
            }

            // 2. 获取当前状态
            var currentStatus = RunTimePhaseStatusMap.ContainsKey(targetPhaseId) 
                ? RunTimePhaseStatusMap[targetPhaseId] 
                : RuntimePhaseStatus.Locked;

            // 如果已经是 Active，无需切换
            if (currentStatus == RuntimePhaseStatus.Active) return false;

            // 3. 校验权限 (必须是 Paused 或 已解锁)
            // 简单的校验：如果是 Locked，必须检查依赖是否满足
            if (currentStatus == RuntimePhaseStatus.Locked)
            {
                var unlockables = FindUnlockablePhases();
                if (!unlockables.Any(p => p.id == targetPhaseId))
                {
                    UnityEngine.Debug.LogWarning($"[PhaseManager] 阶段 {targetPhaseId} 尚未解锁，无法切换。");
                    return false;
                }
            }

            // 4. [执行切换]
            
            // A. 暂停当前所有 Active 的阶段
            var activePhases = RunTimePhaseStatusMap.Where(x => x.Value == RuntimePhaseStatus.Active).ToList();
            foreach (var kvp in activePhases)
            {
                SetPhaseStatus(kvp.Key, RuntimePhaseStatus.Paused);
            }

            // B. 激活新阶段
            SetPhaseStatus(targetPhaseId, RuntimePhaseStatus.Active);

            // 5. [触发对话]
            // 如果是从 Locked -> Active (首次进入)，播放开场白
            if (currentStatus == RuntimePhaseStatus.Locked)
            {
                var phaseData = _mindMapManager.levelGraph.phasesData[targetPhaseId];
                if (phaseData.Dialogue?.OnPhaseStart != null)
                {
                    var lines = DialogueRuntimeHelper.GenerateDialogueLines(phaseData.Dialogue.OnPhaseStart);
                    GameEventDispatcher.DispatchDialogueGenerated(lines);
                }
            }
            else
            {
                // 如果是从 Paused -> Active (切回)，可以给个简单提示
                string phaseName = _mindMapManager.levelGraph.phasesData[targetPhaseId].Name;
                GameEventDispatcher.DispatchDialogueGenerated(new List<string> { $"[系统] 你回到了对“{phaseName}”的调查。" });
            }
            
            // 6. 切换后，广播最新的可用列表 (因为状态变了)
            BroadcastAvailablePhases();

            return true;
        }

        // [新增] 获取所有可供玩家切换的目标
        public List<(string id, string name, string status)> GetAvailableSwitchTargets()
        {
            var list = new List<(string id, string name, string status)>();

            // A. 获取已解锁但未开始的 (Unlockable)
            var unlockables = FindUnlockablePhases();
            foreach (var p in unlockables)
            {
                list.Add((p.id, p.name, "New"));
            }

            // B. 获取暂停中的 (Paused)
            foreach (var kvp in RunTimePhaseStatusMap)
            {
                if (kvp.Value == RuntimePhaseStatus.Paused)
                {
                    string name = _mindMapManager.levelGraph.phasesData[kvp.Key].Name;
                    list.Add((kvp.Key, name, "Paused"));
                }
            }
            
            // 排序 (可选)
            return list.OrderBy(x => x.id).ToList();
        }

        public void BroadcastAvailablePhases()
        {
            var list = GetAvailableSwitchTargets();
            GameEventDispatcher.DispatchAvailablePhasesChanged(list);
        }
    }
}