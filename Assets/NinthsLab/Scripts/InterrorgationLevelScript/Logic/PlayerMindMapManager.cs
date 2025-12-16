using System.Collections.Generic;
using System.Linq;
using AIEngine.Network;
using LogicEngine.LevelGraph;
using Interrorgation.MidLayer;
using LogicEngine.Templates;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace LogicEngine.LevelLogic
{
    public class PlayerMindMapManager
    {
        readonly LevelGraphData levelGraph;

        // [AITask] 将 List 改为 Dictionary 以保留 ID 索引信息
        // Key: NodeId / KeywordID / TemplateKey
        public Dictionary<string, RuntimeNodeData> RunTimeNodeDataMap = new Dictionary<string, RuntimeNodeData>();

        public Dictionary<string, RuntimeEntityItemData> RunTimeEntityItemDataMap = new Dictionary<string, RuntimeEntityItemData>();

        public Dictionary<string, RuntimeTemplateData> RunTimeTemplateDataMap = new Dictionary<string, RuntimeTemplateData>();

        // [新增] 阶段状态管理
        public Dictionary<string, RuntimePhaseStatus> RunTimePhaseStatusMap = new Dictionary<string, RuntimePhaseStatus>();

        public PlayerMindMapManager(ref LevelGraphData inputLevelGraph)
        {
            levelGraph = inputLevelGraph;
            InitializeRuntimeData();
        }

        void InitializeRuntimeData()
        {
            // 1. 生成 RuntimeNodeDataMap
            // 依赖 LevelGraphData 中的 nodeLookup
            RunTimeNodeDataMap.Clear();
            if (levelGraph.nodeLookup != null)
            {
                foreach (var kvp in levelGraph.nodeLookup)
                {
                    string nodeId = kvp.Key;
                    var nodeInfo = kvp.Value;

                    if (!string.IsNullOrEmpty(nodeId) && nodeInfo.Node != null)
                    {
                        // 将 ID 传入 RuntimeNodeData 保存
                        RunTimeNodeDataMap[nodeId] = new RuntimeNodeData(nodeId, nodeInfo.Node, RunTimeNodeStatus.Hidden);
                    }
                }
            }

            // 2. 生成 RunTimeTemplateDataMap
            // 依赖 LevelGraphData 中的 allTemplates
            RunTimeTemplateDataMap.Clear();
            if (levelGraph.allTemplates != null)
            {
                foreach (var kvp in levelGraph.allTemplates)
                {
                    string templateKey = kvp.Key;
                    var templateData = kvp.Value;

                    if (!string.IsNullOrEmpty(templateKey) && templateData != null)
                    {
                        RunTimeTemplateDataMap[templateKey] = new RuntimeTemplateData(templateKey, templateData, RunTimeTemplateDataStatus.Hidden);
                    }
                }
            }

            // 3. 生成 RunTimeEntityItemDataMap
            // 依赖 LevelGraphData 中的 entityListData.Data (Dictionary<string, EntityItem>)
            RunTimeEntityItemDataMap.Clear();
            if (levelGraph.entityListData != null && levelGraph.entityListData.Data != null)
            {
                foreach (var kvp in levelGraph.entityListData.Data)
                {
                    string keywordId = kvp.Key;
                    var entityItem = kvp.Value;

                    if (!string.IsNullOrEmpty(keywordId) && entityItem != null)
                    {
                        RunTimeEntityItemDataMap[keywordId] = new RuntimeEntityItemData(keywordId, entityItem, RunTimeEntityItemStatus.Hidden);
                    }
                }
            }

            RunTimePhaseStatusMap.Clear();
            if (levelGraph.phasesData != null)
            {
                foreach (var kvp in levelGraph.phasesData)
                {
                    // 默认全部 Locked，初始 active 的设置通常由 Manager 或开场逻辑决定
                    RunTimePhaseStatusMap[kvp.Key] = RuntimePhaseStatus.Locked;
                }
            }
        }

        // --- Search Methods ---
        // 虽然存储变为 Dictionary，但按状态搜索通常是为了遍历处理，因此返回 List 是合理的。
        // ID 信息现在已包含在返回的 Runtime 对象内部。

        public List<RuntimeNodeData> GetRuntimeNodeDataByStatus(RunTimeNodeStatus status)
        {
            return RunTimeNodeDataMap.Values.Where(data => data.Status == status).ToList();
        }

        public List<RuntimeEntityItemData> GetRuntimeEntityItemDataByStatus(RunTimeEntityItemStatus status)
        {
            return RunTimeEntityItemDataMap.Values.Where(data => data.Status == status).ToList();
        }

        public List<RuntimeTemplateData> GetRuntimeTemplateDataByStatus(RunTimeTemplateDataStatus status)
        {
            return RunTimeTemplateDataMap.Values.Where(data => data.Status == status).ToList();
        }

        public void ProcessAIResponse(AIResponseData responseData)
        {
            if (responseData == null || responseData.RefereeResult == null)
            {
                return;
            }

            // 提取 AIRefereeResult
            var result = responseData.RefereeResult;

            // 处理发现的节点
            if (result.PassedNodeIds != null && result.PassedNodeIds.Count > 0)
            {
                discoverNodes(result.PassedNodeIds);
            }

            // 处理发现的实体
            if (result.EntityList != null && result.EntityList.Count > 0)
            {
                discoverEntity(result.EntityList);
            }

            // 处理发现的模板
            if (responseData.DiscoveryResult != null && responseData.DiscoveryResult.DiscoveredNodeIds.Count > 0)
            {
                discoverTemplate(responseData.DiscoveryResult.DiscoveredNodeIds);
            }
        }

        void discoverNodes(List<string> nodeIds)
        {
            // 用于收集本次成功从 Hidden 变为 Discovered 的节点
            List<RuntimeNodeData> newlyDiscoveredNodes = new List<RuntimeNodeData>();

            foreach (var nodeId in nodeIds)
            {
                if (string.IsNullOrEmpty(nodeId)) continue;

                if (RunTimeNodeDataMap.TryGetValue(nodeId, out RuntimeNodeData runtimeNode))
                {
                    // 逻辑：只有当状态为 Hidden 时才更新为 Discovered
                    if (runtimeNode.Status == RunTimeNodeStatus.Hidden)
                    {
                        runtimeNode.Status = RunTimeNodeStatus.Discovered;
                        newlyDiscoveredNodes.Add(runtimeNode);
                    }
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"[PlayerMindMapManager]尝试发现未知节点 ID: {nodeId}");
                }
            }

            // 分发事件
            if (newlyDiscoveredNodes.Count > 0)
            {
                var context = new GameEventDispatcher.NodeDiscoverContext(GameEventDispatcher.NodeDiscoverContext.e_DiscoverNewNodeMethod.PlayerInput);
                GameEventDispatcher.DispatchDiscoveredNewNodes(newlyDiscoveredNodes, context);
            }
        }

        void discoverEntity(List<string> entityIds)
        {
            // 用于收集本次成功从 Hidden 变为 Discovered 的实体
            List<RuntimeEntityItemData> newlyDiscoveredEntities = new List<RuntimeEntityItemData>();

            foreach (var entityId in entityIds)
            {
                if (string.IsNullOrEmpty(entityId)) continue;

                if (RunTimeEntityItemDataMap.TryGetValue(entityId, out RuntimeEntityItemData runtimeEntity))
                {
                    // 逻辑：只有当状态为 Hidden 时才更新为 Discovered
                    if (runtimeEntity.Status == RunTimeEntityItemStatus.Hidden)
                    {
                        runtimeEntity.Status = RunTimeEntityItemStatus.Discovered;
                        newlyDiscoveredEntities.Add(runtimeEntity);
                    }
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"[PlayerMindMapManager]尝试发现未知实体 ID: {entityId}");
                }
            }

            // 分发事件
            if (newlyDiscoveredEntities.Count > 0)
            {
                GameEventDispatcher.DispatchDiscoveredNewEntityItems(newlyDiscoveredEntities);
            }
        }

        void discoverTemplate(List<string> templateIds)
        {
            // 用于收集本次成功从 Hidden 变为 Discovered 的模板
            List<RuntimeTemplateData> newlyDiscoveredTemplates = new List<RuntimeTemplateData>();

            foreach (var templateId in templateIds)
            {
                if (string.IsNullOrEmpty(templateId)) continue;

                if (RunTimeTemplateDataMap.TryGetValue(templateId, out RuntimeTemplateData runtimeTemplate))
                {
                    // 逻辑：只有当状态为 Hidden 时才更新为 Discovered
                    if (runtimeTemplate.Status == RunTimeTemplateDataStatus.Hidden)
                    {
                        runtimeTemplate.Status = RunTimeTemplateDataStatus.Discovered;
                        newlyDiscoveredTemplates.Add(runtimeTemplate);
                    }
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"[PlayerMindMapManager]尝试发现未知模板 ID: {templateId}");
                }
            }

            // 分发事件
            if (newlyDiscoveredTemplates.Count > 0)
            {
                GameEventDispatcher.DispatchDiscoveredNewTemplates(newlyDiscoveredTemplates);
            }
        }

        public string ValidateTemplateAnswer(string templateId, List<string> playerInputs)
        {
            // 1. 验证当前模板
            string resultId = ValidateSingleTemplate(templateId, playerInputs);
            if (resultId != null) return resultId;

            // 2. [补全] 兄弟节点匹配 (Smart Matching)
            // 如果当前模板验证失败，查找其他拥有“完全相同文本”的模板进行验证
            // 这对应 HTML 版中 "verify_partial_completion" 的 sibling 查找逻辑
            
            if (!RunTimeTemplateDataMap.TryGetValue(templateId, out var currentRtTemplate)) return null;
            string currentRawText = currentRtTemplate.r_TemplateData.RawText;

            foreach (var kvp in RunTimeTemplateDataMap)
            {
                var otherId = kvp.Key;
                var otherRtTemplate = kvp.Value;

                // 跳过自己
                if (otherId == templateId) continue;
                // 跳过文本不一样的
                if (otherRtTemplate.r_TemplateData.RawText != currentRawText) continue;

                // 尝试验证这个兄弟模板
                string siblingResult = ValidateSingleTemplate(otherId, playerInputs);
                if (siblingResult != null)
                {
                    UnityEngine.Debug.Log($"[SmartMatch] 玩家在模板 {templateId} 填写的答案匹配到了兄弟节点 {siblingResult}");
                    return siblingResult;
                }
            }

            return null;
        }

        private string ValidateSingleTemplate(string templateId, List<string> playerInputs)
        {
            if (!RunTimeTemplateDataMap.TryGetValue(templateId, out var rtTemplate)) return null;
            var templateData = rtTemplate.r_TemplateData;

            foreach (var answer in templateData.Answers)
            {
                if (CheckAnswerMatch(answer, playerInputs))
                {
                    return answer.TargetId;
                }
            }
            return null;
        }

        private bool CheckAnswerMatch(AnswerData answer, List<string> playerInputs)
        {
            if (playerInputs.Count != answer.RequiredInputs.Count) return false;

            for (int i = 0; i < answer.RequiredInputs.Count; i++)
            {
                string targetIdOrText = answer.RequiredInputs[i]; // 这可能是 EntityID (如 "sabina") 或普通文本
                string userInput = playerInputs[i];

                // A. 尝试作为 Entity ID 匹配 (支持别名)
                if (levelGraph.entityListData.Data.TryGetValue(targetIdOrText, out var entityItem))
                {
                    // 检查用户输入是否匹配 Name 或 Alias 中的任意一个
                    bool matchName = string.Equals(userInput, entityItem.Name, System.StringComparison.OrdinalIgnoreCase);
                    bool matchAlias = entityItem.Alias != null && entityItem.Alias.Contains(userInput);
                    
                    // 还有一个兜底：用户是否直接输入了 ID (虽然不常见)
                    bool matchId = string.Equals(userInput, targetIdOrText, System.StringComparison.OrdinalIgnoreCase);

                    if (!matchName && !matchAlias && !matchId) return false;
                }
                // B. 普通文本匹配 (如 "属于/不属于")
                else
                {
                    if (targetIdOrText != userInput) return false;
                }
            }
            return true;
        }    
        // ==========================================
        // [新增] 逻辑补全 1: 节点证明与互斥接口
        // ==========================================

        /// <summary>
        /// 尝试提交/证明一个节点
        /// </summary>
        public bool TryProveNode(string nodeId)
        {
            if (!RunTimeNodeDataMap.TryGetValue(nodeId, out var runtimeNode)) return false;

            if (runtimeNode.Status == RunTimeNodeStatus.Submitted) return true; // 已经是证明状态
            if (runtimeNode.Status != RunTimeNodeStatus.Discovered || runtimeNode.IsInvalidated) return false;

            // 检查依赖
            if (!CheckDependencies(runtimeNode.r_NodeData.Logic?.DependsOn))
            {
                // [补全] 依赖不满足，触发 OnPending 对话
                TriggerDialogue(runtimeNode.r_NodeData.Dialogue?.OnPending);
                return false;
            }

            // 状态变更
            SetNodeStatus(nodeId, RunTimeNodeStatus.Submitted);

            // [补全] 证明成功，触发 OnProven 对话
            TriggerDialogue(runtimeNode.r_NodeData.Dialogue?.OnProven);

            ProcessMutex(runtimeNode.r_NodeData.Logic);
            ProcessAutoVerify();
            CheckPhaseCompletion();

            return true;
        }


        /// <summary>
        /// 提供给外部或内部使用的互斥处理接口
        /// </summary>
        public void ProcessMutex(LogicEngine.Nodes.NodeLogicInfo logicInfo)
        {
            if (logicInfo == null) return;
            string mutexGroup = logicInfo.MutexGroup;
            if (string.IsNullOrEmpty(mutexGroup)) return;

            foreach (var kvp in RunTimeNodeDataMap)
            {
                var targetNode = kvp.Value;
                // 跳过自己
                if (targetNode.Id == logicInfo.ToString()) continue; // 需确保 logicInfo 能反查 ID 或从上层传 ID，此处简化逻辑

                // 如果属于同一互斥组
                if (targetNode.r_NodeData.Logic != null && targetNode.r_NodeData.Logic.MutexGroup == mutexGroup)
                {
                    // [关键] 不修改 Enum，而是调用作废接口
                    InvalidateNode(targetNode.Id);
                }
            }
        }

        /// <summary>
        /// [要求的互斥逻辑接口] 将节点标记为作废 (Invalidated)
        /// </summary>
        public void InvalidateNode(string nodeId)
        {
            if (RunTimeNodeDataMap.TryGetValue(nodeId, out var node))
            {
                if (node.Status != RunTimeNodeStatus.Submitted) // 已提交的通常不回滚
                {
                    node.IsInvalidated = true; // 标记位变更
                    // 依然分发状态变更事件，UI层需识别 IsInvalidated 字段
                    GameEventDispatcher.DispatchNodeStatusChanged(node);
                }
            }
        }

        private void SetNodeStatus(string nodeId, RunTimeNodeStatus newStatus)
        {
            if (RunTimeNodeDataMap.ContainsKey(nodeId))
            {
                RunTimeNodeDataMap[nodeId].Status = newStatus;
                GameEventDispatcher.DispatchNodeStatusChanged(RunTimeNodeDataMap[nodeId]);
            }
        }

        private void ProcessAutoVerify()
        {
            bool hasChanged = true;
            while (hasChanged)
            {
                hasChanged = false;
                foreach (var kvp in RunTimeNodeDataMap)
                {
                    var node = kvp.Value;
                    // 检查：Discovered + 未作废 + 标记为自动验证
                    if (node.Status == RunTimeNodeStatus.Discovered && !node.IsInvalidated &&
                        node.r_NodeData.Logic != null && node.r_NodeData.Logic.IsAutoVerified)
                    {
                        if (CheckDependencies(node.r_NodeData.Logic.DependsOn))
                        {
                            SetNodeStatus(node.Id, RunTimeNodeStatus.Submitted);
                            ProcessMutex(node.r_NodeData.Logic);
                            hasChanged = true;
                        }
                    }
                }
            }
        }

        // ==========================================
        // [新增] 逻辑补全 2: 阶段流转与完成判定
        // ==========================================

        public void SetPhaseStatus(string phaseId, RuntimePhaseStatus status)
        {
            if (RunTimePhaseStatusMap.ContainsKey(phaseId))
            {
                RunTimePhaseStatusMap[phaseId] = status;
                GameEventDispatcher.DispatchPhaseStatusChanged(phaseId, status);
            }
        }

        private void CheckPhaseCompletion()
        {
            var activePhases = RunTimePhaseStatusMap.Where(x => x.Value == RuntimePhaseStatus.Active).Select(x => x.Key).ToList();

            foreach (var phaseId in activePhases)
            {
                if (!levelGraph.phasesData.TryGetValue(phaseId, out var phaseData)) continue;
                if (phaseData.CompletionNodes == null || phaseData.CompletionNodes.Count == 0) continue;

                // Any 逻辑：任一完成即完成
                bool isComplete = false;
                foreach (var targetId in phaseData.CompletionNodes)
                {
                    if (IsNodeProven(targetId))
                    {
                        isComplete = true;
                        break;
                    }
                }

                if (isComplete)
                {
                    SetPhaseStatus(phaseId, RuntimePhaseStatus.Completed);
                    
                    // [补全] 触发阶段完成对话
                    if (phaseData.Dialogue?.OnPhaseComplete != null)
                    {
                        TriggerDialogue(phaseData.Dialogue.OnPhaseComplete);
                    }

                    // [补全] 计算下一阶段 (Find Unlockable Phases)
                    var nextPhases = FindUnlockablePhases();
                    if (nextPhases.Count > 0)
                    {
                        // 通知 UI 显示阶段选择弹窗
                        GameEventDispatcher.DispatchPhaseUnlockEvents(phaseData.Name, nextPhases);
                    }
                }
            }
        }

        private List<(string id, string name)> FindUnlockablePhases()
        {
            var result = new List<(string id, string name)>();
            
            // 获取当前所有已完成的阶段
            var completedPhaseIds = RunTimePhaseStatusMap
                .Where(x => x.Value == RuntimePhaseStatus.Completed)
                .Select(x => x.Key).ToList();

            // 构造一个简单的 Context 供依赖检查使用 (True = Completed)
            // 这里我们临时欺骗 CheckDependencies，让它把 Completed 的阶段当做 Proven 节点处理
            // (前提是 depends_on 的 JSON 结构是通用的)
            
            // 为了复用 CheckDependencies，我们需要一个能判断 "Phase是否完成" 的逻辑
            // 但 CheckDependencies 内部是调用的 IsNodeProven。
            // 简单的做法：重写一个针对 Phase 的依赖检查，或者扩充 CheckDependencies。
            // 鉴于不修改接口的原则，我们在这里手动实现简单的 Phase 依赖检查。
            
            foreach (var kvp in levelGraph.phasesData)
            {
                string pid = kvp.Key;
                var pData = kvp.Value;
                var currentStatus = RunTimePhaseStatusMap.ContainsKey(pid) ? RunTimePhaseStatusMap[pid] : RuntimePhaseStatus.Locked;

                // 只检查 Locked 状态的
                if (currentStatus != RuntimePhaseStatus.Locked) continue;

                // 检查依赖
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

            // 情况 1: 字符串形式 "phase1" -> 默认为 true
            if (dependsOn.Type == JTokenType.String)
            {
                return completedPhases.Contains(dependsOn.ToString());
            }
            // 情况 2: 对象形式 { ... }
            else if (dependsOn.Type == JTokenType.Object)
            {
                var obj = dependsOn as JObject;
                foreach (var prop in obj.Properties())
                {
                    string key = prop.Name.ToLower();
                    JToken value = prop.Value;

                    if (key == "or")
                    {
                        // OR 逻辑
                        bool anyMet = false;
                        if (value is JObject orObj)
                        {
                            foreach (var p in orObj.Properties())
                            {
                                if (CheckSinglePhaseCondition(p.Name, p.Value, completedPhases)) 
                                { 
                                    anyMet = true; 
                                    break; 
                                }
                            }
                        }
                        if (!anyMet) return false;
                    }
                    else if (key == "and")
                    {
                        // AND 逻辑
                        if (value is JObject andObj)
                        {
                            foreach (var p in andObj.Properties())
                            {
                                if (!CheckSinglePhaseCondition(p.Name, p.Value, completedPhases)) return false;
                            }
                        }
                    }
                    else
                    {
                        // 直接键值对: "phase1": true
                        if (!CheckSinglePhaseCondition(key, value, completedPhases)) return false;
                    }
                }
                return true;
            }
            
            return true; 
        }

        // [新增] 辅助方法：检查单个阶段是否满足条件
        private bool CheckSinglePhaseCondition(string targetPhaseId, JToken expectedValueToken, List<string> completedPhases)
        {
            bool isCompleted = completedPhases.Contains(targetPhaseId);
            
            // 尝试获取期望值 (true/false)
            bool expected = true;
            try 
            {
                expected = expectedValueToken.ToObject<bool>();
            }
            catch 
            {
                // 如果配置错误（比如把对象当bool），默认视为 true 并打印警告，防止崩溃
                UnityEngine.Debug.LogWarning($"[PhaseCheck] 阶段依赖格式错误: Key={targetPhaseId} 的值不是布尔类型。");
                return false;
            }

            return isCompleted == expected;
        }
        // ==========================================
        // [新增] 依赖检查辅助方法
        // ==========================================
        private bool CheckDependencies(JToken dependsOn)
        {
            if (dependsOn == null || !dependsOn.HasValues) return true;

            if (dependsOn.Type == JTokenType.String)
            {
                return IsNodeProven(dependsOn.ToString());
            }
            else if (dependsOn.Type == JTokenType.Array) // AND
            {
                foreach (var child in dependsOn)
                {
                    if (!CheckDependencies(child)) return false;
                }
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
                            {
                                if (CheckSingleCondition(p.Name, p.Value)) { anyMet = true; break; }
                            }
                        }
                        if (!anyMet) return false;
                    }
                    else if (key == "and")
                    {
                        if (prop.Value is JObject andObj)
                        {
                            foreach (var p in andObj.Properties())
                            {
                                if (!CheckSingleCondition(p.Name, p.Value)) return false;
                            }
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
            return RunTimeNodeDataMap.TryGetValue(nodeId, out var node) && node.Status == RunTimeNodeStatus.Submitted;
        }

        private void TriggerDialogue(JToken dialogueScript)
        {
            if (dialogueScript == null) return;
            
            // 调用 Helper，它内部会调用 DialogueParser.ParseDialogue
            var lines = DialogueRuntimeHelper.GenerateDialogueLines(dialogueScript);
            
            if (lines != null && lines.Count > 0)
            {
                GameEventDispatcher.DispatchDialogueGenerated(lines);
            }
        }
    }

    // ==========================================
    // Runtime Data Classes
    // ==========================================

    public enum RunTimeNodeStatus
    {
        Hidden,
        Discovered,
        Submitted,
    }

    public class RuntimeNodeData
    {
        public readonly string Id; // 新增 ID 字段
        public readonly NodeData r_NodeData;
        public RunTimeNodeStatus Status;

        public bool IsInvalidated = false; 
        public RuntimeNodeData(string id, NodeData nodeData, RunTimeNodeStatus status)
        {
            this.Id = id;
            this.r_NodeData = nodeData;
            this.Status = status;
        }
    }
    public enum RuntimePhaseStatus
    {
        Locked,
        Active,
        Completed,
        Paused
    }
    public enum RunTimeEntityItemStatus
    {
        Hidden,
        Discovered,
    }

    public class RuntimeEntityItemData
    {
        public readonly string Id; // 新增 ID 字段 (对应 KeywordID)
        public readonly EntityItem r_EntityItemData;
        public RunTimeEntityItemStatus Status;

        public RuntimeEntityItemData(string id, EntityItem data, RunTimeEntityItemStatus status)
        {
            this.Id = id;
            this.r_EntityItemData = data;
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
        public readonly string Id; // 新增 ID 字段 (对应 TemplateKey)
        public readonly TemplateData r_TemplateData;
        public RunTimeTemplateDataStatus Status;

        public RuntimeTemplateData(string id, TemplateData data, RunTimeTemplateDataStatus status)
        {
            this.Id = id;
            this.r_TemplateData = data;
            this.Status = status;
        }
    }
}
