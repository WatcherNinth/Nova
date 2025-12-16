using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection; // [必需] 用于反射注入
using LogicEngine;
using LogicEngine.LevelGraph;
using LogicEngine.LevelLogic; // 引用 PlayerMindMapManager
using LogicEngine.Parser;
using LogicEngine.Tests;
using AIEngine;
using AIEngine.Network;
using Interrorgation.MidLayer;

public class AIRealIntegrationTester : MonoBehaviour
{
    [Header("1. 自动加载配置")]
    [Tooltip("文件名 (必须位于 LevelTestManager 配置的路径下)")]
    public string targetFileName = "demo_v2.json";

    [Header("2. 测试环境")]
    public string phaseId = "phase1";
    
    [TextArea(3, 5)]
    public string playerInput = "十五楼的血迹是谁的？";

    [Header("3. 状态监控")]
    [SerializeField] private bool isWaitingResponse = false;

    private void OnEnable()
    {
        AIEventDispatcher.OnResponseReceived += OnFinalResultReceived;
    }

    private void OnDisable()
    {
        AIEventDispatcher.OnResponseReceived -= OnFinalResultReceived;
    }

    // =========================================================
    // 测试入口
    // =========================================================
    [ContextMenu("🚀 发送真实请求 (Real Request)")]
    public void SendRealRequest()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("❌ [Test] 请先点击 Play 运行游戏！");
            return;
        }

        if (isWaitingResponse)
        {
            Debug.LogWarning("⚠️ [Test] 请等待上一个请求完成...");
            return;
        }

        // --- 核心修改：确保游戏管理器已初始化 ---
        if (!EnsureGameInitialized())
        {
            return; // 初始化失败，中止
        }

        // 获取刚刚注入的数据
        LevelGraphData graphData = LevelGraphContext.CurrentGraph;

        // 3. 触发事件
        Debug.Log($"<color=cyan>====== 🚀 [测试开始] 发送真实 AI 请求 ======</color>\n" +
                  $"输入内容: {playerInput}");

        isWaitingResponse = true;
        
        // 这将触发 AIManager -> HTTP -> ... -> InterrorgationLevelManager
        AIEventDispatcher.DispatchPlayerInputString(graphData, phaseId, playerInput);
    }

    // =========================================================
    // 初始化逻辑 (模拟 LoadLevel 的行为)
    // =========================================================
    private bool EnsureGameInitialized()
    {
        var manager = InterrorgationLevelManager.Instance;
        if (manager == null)
        {
            Debug.LogError("❌ [Test] 场景中找不到 InterrorgationLevelManager！");
            return false;
        }

        // 1. 检查是否已经初始化过 (通过反射检查私有字段)
        var type = typeof(InterrorgationLevelManager);
        var mapField = type.GetField("playerMindMapManager", BindingFlags.NonPublic | BindingFlags.Instance);
        var currentMap = mapField.GetValue(manager);

        if (currentMap != null)
        {
            // 已经初始化过了，直接返回成功
            return true;
        }

        Debug.LogWarning("⚠️ [Test] 检测到管理器未初始化，正在执行手动注入 (Bypass LoadLevel)...");

        // 2. 加载数据 (这一步是为了获取 GraphData)
        // 我们借用 LevelTestManager 的路径配置
        var testManager = LevelTestManager.Instance;
        string folderPath = Path.Combine(Application.dataPath, testManager.relativePath);
        string fullPath = Path.Combine(folderPath, targetFileName);

        if (!File.Exists(fullPath))
        {
            Debug.LogError($"❌ [Test] 找不到文件: {fullPath}");
            return false;
        }

        try
        {
            string jsonText = File.ReadAllText(fullPath);
            LevelGraphData graphData = LevelGraphParser.Parse(jsonText);
            graphData.InitializeRuntimeData();

            // 3. 创建 PlayerMindMapManager 实例
            PlayerMindMapManager mindMap = new PlayerMindMapManager(ref graphData);

            // 4. 【反射注入】将数据强行塞给 Manager
            // 注入 currentLevelGraph
            var graphField = type.GetField("currentLevelGraph", BindingFlags.NonPublic | BindingFlags.Instance);
            graphField.SetValue(manager, graphData);

            // 注入 playerMindMapManager
            mapField.SetValue(manager, mindMap);

            // 注入 currentPhaseId (设置为 Inspector 里填的值)
            var phaseField = type.GetField("currentPhaseId", BindingFlags.NonPublic | BindingFlags.Instance);
            phaseField.SetValue(manager, phaseId);

            // 5. 启动初始逻辑 (激活 Phase)
            manager.StartGameLogic();

            Debug.Log($"✅ [Test] 初始化成功！已注入数据并激活 {phaseId}");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ [Test] 初始化异常: {ex}");
            return false;
        }
    }

    // =========================================================
    // 回调处理
    // =========================================================
    private void OnFinalResultReceived(AIResponseData response)
    {
        isWaitingResponse = false;
        Debug.Log("<color=green>====== ✅ [测试结束] 收到 AI 响应 ======</color>");

        if (response.HasError)
        {
            Debug.LogError($"❌ [AI 报错]: {response.ErrorMessage}");
            return;
        }

        // 打印结果... (保持原样)
        if (response.RefereeResult != null)
        {
            var r = response.RefereeResult;
            string nodes = (r.PassedNodeIds != null && r.PassedNodeIds.Count > 0) ? string.Join(", ", r.PassedNodeIds) : "无";
            Debug.Log($"🎯 [Referee] 判定节点: {nodes}");
        }

        if (response.DiscoveryResult != null)
        {
            var d = response.DiscoveryResult;
            string disc = (d.DiscoveredNodeIds != null && d.DiscoveredNodeIds.Count > 0) ? string.Join(", ", d.DiscoveredNodeIds) : "无";
            Debug.Log($"💡 [Discovery] 发现线索: {disc}");
        }
    }
}