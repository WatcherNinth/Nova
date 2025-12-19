using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using LogicEngine;
using LogicEngine.LevelGraph;
using LogicEngine.LevelLogic;
using LogicEngine.Parser;
using Interrorgation.MidLayer;
using AIEngine;
using AIEngine.Network;

public class AIRealIntegrationTester : MonoBehaviour
{
    [Header("1. 剧本配置")]
    public string targetFileName = "demo_v2.json";
    
    [Header("2. 玩家交互")]
    [TextArea(3, 10)]
    public string playerInput = "十五楼的血迹是谁的？";

    // --- 状态数据 ---
    [HideInInspector] public bool isWaitingResponse = false;
    [HideInInspector] public string lastAIReasoning = "";
    [HideInInspector] public string statusLog = "";
    
    // Scope 栈缓存
    public List<string> currentScopeStack = new List<string>();
    
    // 待选阶段缓存 (新解锁的)
    public List<(string id, string name)> pendingPhaseChoices = new List<(string id, string name)>();

    // =========================================================
    // 生命周期
    // =========================================================
    private void OnEnable()
    {
        AIEventDispatcher.OnResponseReceived += OnAIResponse;
        GameEventDispatcher.OnDialogueGenerated += OnDialogue;
        GameEventDispatcher.OnPhaseUnlockEvents += OnPhaseUnlock;
        GameEventDispatcher.OnScopeStackChanged += OnScopeChanged;
    }

    private void OnDisable()
    {
        AIEventDispatcher.OnResponseReceived -= OnAIResponse;
        GameEventDispatcher.OnDialogueGenerated -= OnDialogue;
        GameEventDispatcher.OnPhaseUnlockEvents -= OnPhaseUnlock;
        GameEventDispatcher.OnScopeStackChanged -= OnScopeChanged;
    }

    // =========================================================
    // 操作接口
    // =========================================================

    public void InitializeGame()
    {
        EnsureGameInitialized(true);
        currentScopeStack.Clear();
        pendingPhaseChoices.Clear();
        statusLog = "";
    }

    public void SendInputToAI()
    {
        if (!Application.isPlaying) { Log("❌ 请先运行游戏！"); return; }
        if (!EnsureGameInitialized()) return;

        var manager = InterrorgationLevelManager.Instance;
        var graph = GetPrivateField<LevelGraphData>(manager, "currentLevelGraph");
        string phaseId = GetPrivateField<string>(manager, "currentPhaseId");

        if (graph == null) { Log("❌ 关卡未加载"); return; }

        isWaitingResponse = true;
        Log($"🚀 发送: {playerInput} (当前阶段: {phaseId})");
        AIEventDispatcher.DispatchPlayerInputString(graph, phaseId, playerInput);
    }

    public void SubmitNodeOption(string nodeId)
    {
        Log($"👉 [点击提交] 节点: {nodeId}");
        GameEventDispatcher.DispatchNodeOptionSubmitted(nodeId);
    }

    public void SubmitTemplateAnswer(string templateId, string answerString)
    {
        List<string> answers = new List<string>(answerString.Split(new char[] { ',', '，' }, System.StringSplitOptions.RemoveEmptyEntries));
        for(int i=0; i<answers.Count; i++) answers[i] = answers[i].Trim();
        Log($"👉 [提交填空] {templateId}: {string.Join("|", answers)}");
        GameEventDispatcher.DispatchPlayerSubmitTemplateAnswer(templateId, answers);
    }

    // 切换阶段：走事件流程，触发 Pause/Active 逻辑
    public void SwitchPhase(string targetPhaseId)
    {
        Log($"🔄 [请求切换] 目标阶段: {targetPhaseId}");
        GameEventDispatcher.DispatchPlayerRequestPhaseSwitch(targetPhaseId);
        
        // 如果是新解锁的列表里的，切完就移除
        pendingPhaseChoices.RemoveAll(x => x.id == targetPhaseId);
    }

    // =========================================================
    // 数据获取 (反射)
    // =========================================================
    
    public PlayerMindMapManager GetMindMap()
    {
        if (InterrorgationLevelManager.Instance == null) return null;
        return GetPrivateField<PlayerMindMapManager>(InterrorgationLevelManager.Instance, "playerMindMapManager");
    }

    public GamePhaseManager GetPhaseManager()
    {
        if (InterrorgationLevelManager.Instance == null) return null;
        return GetPrivateField<GamePhaseManager>(InterrorgationLevelManager.Instance, "gamePhaseManager");
    }

    public GameScopeManager GetScopeManager()
    {
        if (InterrorgationLevelManager.Instance == null) return null;
        return GetPrivateField<GameScopeManager>(InterrorgationLevelManager.Instance, "gameScopeManager");
    }

    // =========================================================
    // 回调处理
    // =========================================================

    private void OnScopeChanged(List<string> stack)
    {
        currentScopeStack = stack ?? new List<string>();
        string path = currentScopeStack.Count > 0 ? string.Join(" > ", currentScopeStack) : "全局";
        Log($"🔍 [Scope 更新] 当前关注路径: {path}");
    }

    private void OnAIResponse(AIResponseData data)
    {
        isWaitingResponse = false;
        if (data.HasError) Log($"❌ AI Error: {data.ErrorMessage}");
        else
        {
            Log("✅ AI 响应接收成功");
            if (data.RefereeResult?.PassedNodeIds?.Count > 0)
                Log($"   🎯 通过节点: {string.Join(", ", data.RefereeResult.PassedNodeIds)}");
            if (data.DiscoveryResult?.DiscoveredNodeIds?.Count > 0)
                Log($"   💡 发现线索: {string.Join(", ", data.DiscoveryResult.DiscoveredNodeIds)}");
        }
    }

    private void OnDialogue(List<string> lines)
    {
        foreach (var line in lines) Log($"🗣️ {line}");
    }

    private void OnPhaseUnlock(string name, List<(string id, string name)> nexts)
    {
        Log($"🎉 阶段 [{name}] 完成！解锁新路径。");
        pendingPhaseChoices.Clear();
        pendingPhaseChoices.AddRange(nexts);
    }

    private void Log(string msg)
    {
        statusLog = $"[{System.DateTime.Now:mm:ss}] {msg}\n" + statusLog;
        if (statusLog.Length > 3000) statusLog = statusLog.Substring(0, 3000);
        Debug.Log(msg);
    }

    // =========================================================
    // 初始化逻辑 (手动组装四层架构)
    // =========================================================
    private bool EnsureGameInitialized(bool force = false)
    {
        var manager = InterrorgationLevelManager.Instance;
        if (manager == null) return false;
        
        var map = GetPrivateField<PlayerMindMapManager>(manager, "playerMindMapManager");
        if (map != null && !force) return true;

        // 1. 加载
        string relativePath = LogicEngine.Tests.LevelTestManager.Instance.relativePath;
        string path = Path.Combine(Application.dataPath, relativePath, targetFileName);
        if (!File.Exists(path)) { Log($"❌ 文件丢失: {path}"); return false; }

        string json = File.ReadAllText(path);
        var graph = LevelGraphParser.Parse(json);
        graph.InitializeRuntimeData();

        // 2. 组装 Manager (这里补回了 ScopeManager)
        var playerMap = new PlayerMindMapManager(graph);
        var phaseMgr = new GamePhaseManager(playerMap);
        var logicMgr = new NodeLogicManager(playerMap);
        var scopeMgr = new GameScopeManager(playerMap); // [修复] 创建

        // 3. 连接依赖
        logicMgr.SetPhaseManager(phaseMgr);
        logicMgr.SetScopeManager(scopeMgr); // [修复] 注入
        scopeMgr.SetLogicManager(logicMgr); // [修复] 注入

        // 4. 注入到 InterrorgationLevelManager
        var t = typeof(InterrorgationLevelManager);
        Inject(t, manager, "currentLevelGraph", graph);
        Inject(t, manager, "playerMindMapManager", playerMap);
        Inject(t, manager, "gamePhaseManager", phaseMgr);
        Inject(t, manager, "nodeLogicManager", logicMgr);
        Inject(t, manager, "gameScopeManager", scopeMgr); // [修复] 注入字段
        
        manager.StartGameLogic();
        Log("✅ 游戏初始化完成 (Scope/Phase/Logic/Map)");
        return true;
    }

    private void Inject(System.Type t, object obj, string name, object val)
    {
        var f = t.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        if (f != null) f.SetValue(obj, val);
        else Debug.LogError($"无法注入字段 {name} (请检查 InterrorgationLevelManager 是否包含此字段)");
    }

    private T GetPrivateField<T>(object obj, string name)
    {
        var f = obj.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        return f != null ? (T)f.GetValue(obj) : default(T);
    }
}