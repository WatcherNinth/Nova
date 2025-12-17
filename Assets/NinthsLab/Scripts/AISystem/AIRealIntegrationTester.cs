using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using LogicEngine;
using LogicEngine.LevelGraph;
using LogicEngine.LevelLogic;
using LogicEngine.Parser;
using Interrorgation.MidLayer;
using AIEngine.Network;

public class AIRealIntegrationTester : MonoBehaviour
{
    [Header("1. 剧本配置")]
    public string targetFileName = "demo_v2.json";
    
    [Header("2. 玩家交互")]
    [TextArea(3, 10)]
    public string playerInput = "十五楼的血迹是谁的？";

    [HideInInspector] public string lastAIReasoning = ""; 
    [HideInInspector] public string statusLog = "";
    [HideInInspector] public bool isWaitingResponse = false;

    public List<(string id, string name)> pendingPhaseChoices = new List<(string id, string name)>();
    // --- 事件监听 ---
    private void OnEnable()
    {
        AIEventDispatcher.OnResponseReceived += OnAIResponse;
        GameEventDispatcher.OnDialogueGenerated += OnDialogue;
        GameEventDispatcher.OnPhaseUnlockEvents += OnPhaseUnlock;
    }

    private void OnDisable()
    {
        AIEventDispatcher.OnResponseReceived -= OnAIResponse;
        GameEventDispatcher.OnDialogueGenerated -= OnDialogue;
        GameEventDispatcher.OnPhaseUnlockEvents -= OnPhaseUnlock;
    }

    // =========================================================
    // 操作接口
    // =========================================================

    public void InitializeGame()
    {
        EnsureGameInitialized(true);
    }

    public void SendInputToAI()
    {
        if (!Application.isPlaying) { Log("❌ 必须在 Play 模式下运行！"); return; }
        if (isWaitingResponse) { Log("⚠️ 正在等待上一次请求..."); return; }

        if (!EnsureGameInitialized()) return;

        var manager = InterrorgationLevelManager.Instance;
        var graphData = GetPrivateField<LevelGraphData>(manager, "currentLevelGraph");
        
        // [修改] currentPhaseId 依然在 Manager 中有一份拷贝，可以获取
        string phaseId = GetPrivateField<string>(manager, "currentPhaseId");

        if (graphData == null || string.IsNullOrEmpty(phaseId))
        {
            Log("❌ 数据异常：Graph 或 Phase 为空");
            return;
        }

        isWaitingResponse = true;
        Log($"🚀 发送请求: {playerInput} (Phase: {phaseId})");
        AIEventDispatcher.DispatchPlayerInputString(graphData, phaseId, playerInput);
    }

    public void SubmitNodeOption(string nodeId)
    {
        Log($"👉 [操作] 提交节点选项: {nodeId}");
        GameEventDispatcher.DispatchNodeOptionSubmitted(nodeId);
    }

    public void SubmitTemplateAnswer(string templateId, string answerString)
    {
        List<string> answers = new List<string>(answerString.Split(new char[] { ',', '，' }, System.StringSplitOptions.RemoveEmptyEntries));
        for(int i=0; i<answers.Count; i++) answers[i] = answers[i].Trim();

        Log($"👉 [操作] 提交填空: {templateId} -> [{string.Join("|", answers)}]");
        GameEventDispatcher.DispatchPlayerSubmitTemplateAnswer(templateId, answers);
    }

    public void SwitchPhase(string targetPhaseId)
    {
        var manager = InterrorgationLevelManager.Instance;
        var phaseMgr = GetPhaseManager(manager);
        
        if (phaseMgr != null)
        {
            // 1. 获取当前 Phase
            string currentPhaseId = GetPrivateField<string>(manager, "currentPhaseId");

            // 2. 暂停当前
            if (phaseMgr.RunTimePhaseStatusMap.TryGetValue(currentPhaseId, out var status))
            {
                if (status == RuntimePhaseStatus.Active)
                {
                    phaseMgr.SetPhaseStatus(currentPhaseId, RuntimePhaseStatus.Paused);
                    Log($"⏸️ 暂停阶段: {currentPhaseId}");
                }
            }

            // 3. 激活新阶段
            phaseMgr.SetPhaseStatus(targetPhaseId, RuntimePhaseStatus.Active);
            
            // 4. 更新 Manager 记录
            var field = typeof(InterrorgationLevelManager).GetField("currentPhaseId", BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(manager, targetPhaseId);
            
            Log($"▶️ 激活阶段: {targetPhaseId}");

            // [新增] 切换成功后，清空待选列表（让黄色的警告框消失）
            pendingPhaseChoices.Clear();
        }
    }
    // =========================================================
    // 数据获取接口 (供 Editor 使用)
    // =========================================================
    
    public PlayerMindMapManager GetMindMapData()
    {
        if (InterrorgationLevelManager.Instance == null) return null;
        return GetPrivateField<PlayerMindMapManager>(InterrorgationLevelManager.Instance, "playerMindMapManager");
    }

    // [新增] 获取 PhaseManager
    public GamePhaseManager GetPhaseData()
    {
        if (InterrorgationLevelManager.Instance == null) return null;
        return GetPrivateField<GamePhaseManager>(InterrorgationLevelManager.Instance, "gamePhaseManager");
    }

    // =========================================================
    // 内部逻辑
    // =========================================================

    private void OnAIResponse(AIResponseData data)
    {
        isWaitingResponse = false;
        if (data.HasError)
        {
            Log($"❌ AI 错误: {data.ErrorMessage}");
        }
        else
        {
            lastAIReasoning = "（思考过程已在 Console 日志中打印）";
            Log("✅ AI 响应接收成功");
            
            if (data.RefereeResult != null && data.RefereeResult.PassedNodeIds.Count > 0)
                Log($"   🎯 通过节点: {string.Join(", ", data.RefereeResult.PassedNodeIds)}");
            
            if (data.DiscoveryResult != null && data.DiscoveryResult.DiscoveredNodeIds.Count > 0)
                Log($"   💡 发现线索: {string.Join(", ", data.DiscoveryResult.DiscoveredNodeIds)}");
        }
    }

    private void OnDialogue(List<string> lines)
    {
        foreach (var line in lines) Log($"🗣️ [剧情]: {line}");
    }
    
    private void OnPhaseUnlock(string completedName, List<(string id, string name)> nextPhases)
    {
        Log($"🎉 阶段 [{completedName}] 完成！解锁了 {nextPhases.Count} 个新方向。");
        
        // [新增] 更新列表供 Editor 显示
        pendingPhaseChoices.Clear();
        if (nextPhases != null)
        {
            pendingPhaseChoices.AddRange(nextPhases);
        }
    }

    private void Log(string msg)
    {
        statusLog = $"[{System.DateTime.Now:HH:mm:ss}] {msg}\n" + statusLog;
        if (statusLog.Length > 2000) statusLog = statusLog.Substring(0, 2000);
        Debug.Log(msg);
    }

    // [核心修改] 适配新的三 Manager 架构
    private bool EnsureGameInitialized(bool forceReload = false)
    {
        var manager = InterrorgationLevelManager.Instance;
        if (manager == null) return false;

        var map = GetPrivateField<PlayerMindMapManager>(manager, "playerMindMapManager");
        if (map != null && !forceReload) return true;

        // 加载逻辑
        string relativePath = LogicEngine.Tests.LevelTestManager.Instance.relativePath;
        string path = Path.Combine(Application.dataPath, relativePath, targetFileName);
        
        if (!File.Exists(path)) { Log($"❌ 文件未找到: {path}"); return false; }

        string json = File.ReadAllText(path);
        var graph = LevelGraphParser.Parse(json);
        graph.InitializeRuntimeData();

        // [修改] 手动组装三个 Manager
        var playerMap = new PlayerMindMapManager(graph);
        var phaseMgr = new GamePhaseManager(playerMap);
        var logicMgr = new NodeLogicManager(playerMap);
        logicMgr.SetPhaseManager(phaseMgr);

        // [修改] 反射注入所有字段
        var t = typeof(InterrorgationLevelManager);
        t.GetField("currentLevelGraph", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(manager, graph);
        t.GetField("playerMindMapManager", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(manager, playerMap);
        t.GetField("gamePhaseManager", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(manager, phaseMgr); // 注入 Phase
        t.GetField("nodeLogicManager", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(manager, logicMgr); // 注入 Logic
        
        // 启动
        manager.StartGameLogic(); 
        Log("✅ 游戏初始化完成 (架构升级版)");
        return true;
    }

    private T GetPrivateField<T>(object instance, string fieldName)
    {
        var f = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (f == null) return default(T);
        return (T)f.GetValue(instance);
    }
    
    // [新增] 用于存储从反射获取 PhaseManager 的辅助方法
    private GamePhaseManager GetPhaseManager(InterrorgationLevelManager manager)
    {
        return GetPrivateField<GamePhaseManager>(manager, "gamePhaseManager");
    }
}