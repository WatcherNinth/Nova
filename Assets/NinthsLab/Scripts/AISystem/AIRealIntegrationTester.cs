using UnityEngine;
using System.Collections.Generic;
using System.IO;
using LogicEngine;
using LogicEngine.LevelGraph;
using LogicEngine.Parser;
using LogicEngine.Tests; // 引用 LevelTestManager
using AIEngine;          // 引用 Dispatcher
using AIEngine.Network;  // 引用 AIResponseData
using Interrorgation.MidLayer;

public class AIRealIntegrationTester : MonoBehaviour
{
    [Header("1. 自动加载配置")]
    [Tooltip("如果当前没有加载剧本，脚本会自动加载这个文件 (必须位于 LevelTestManager 配置的路径下)")]
    public string targetFileName = "demo_v2.json";

    [Header("2. 测试环境")]
    [Tooltip("模拟当前阶段 ID (必须存在于剧本中)")]
    public string phaseId = "phase1";
    
    [Tooltip("模拟玩家输入")]
    [TextArea(3, 5)]
    public string playerInput = "十五楼的血迹是谁的？";

    [Header("3. 状态监控")]
    [SerializeField] private bool isWaitingResponse = false;

    // =========================================================
    // 生命周期
    // =========================================================
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
        // 1. 检查运行状态
        if (!Application.isPlaying)
        {
            Debug.LogError("❌ [Test] 请先点击 Unity 的 Play 按钮运行游戏！网络请求依赖协程。");
            return;
        }

        if (isWaitingResponse)
        {
            Debug.LogWarning("⚠️ [Test] 上一个请求还在处理中，请稍候...");
            return;
        }

        // 2. 获取或加载数据
        LevelGraphData graphData = EnsureDataLoaded();
        
        if (graphData == null)
        {
            // 错误信息在 EnsureDataLoaded 里打印了
            return;
        }

        // 3. 触发事件
        Debug.Log($"<color=cyan>====== 🚀 [测试开始] 发送真实 AI 请求 ======</color>\n" +
                  $"目标文件: {targetFileName}\n" +
                  $"输入内容: {playerInput}\n" +
                  $"当前阶段: {phaseId}\n" +
                  $"剧本节点数: {graphData.nodeLookup.Count}");

        isWaitingResponse = true;
        
        // 这将触发 AIManager -> AIRefereeModel -> AIClient -> HTTP Request
        AIEventDispatcher.DispatchPlayerInputString(graphData, phaseId, playerInput);
    }

    // =========================================================
    // 自动加载逻辑 (复用之前的逻辑)
    // =========================================================
    private LevelGraphData EnsureDataLoaded()
    {
        // A. 先尝试直接从 Context 获取
        var current = LevelGraphContext.CurrentGraph;
        if (current != null && current.nodeLookup != null && current.nodeLookup.Count > 0)
        {
            return current;
        }

        Debug.LogWarning("⚠️ [Test] 当前没有加载剧本数据，正在尝试自动加载...");

        // B. 尝试通过 LevelTestManager 加载
        var manager = LevelTestManager.Instance;
        if (manager == null)
        {
            Debug.LogError("❌ [Test] 场景中找不到 LevelTestManager！无法获取路径配置。");
            return null;
        }

        // 拼接路径
        string folderPath = Path.Combine(Application.dataPath, manager.relativePath);
        string fullPath = Path.Combine(folderPath, targetFileName);

        if (!File.Exists(fullPath))
        {
            Debug.LogError($"❌ [Test] 找不到文件: {fullPath}");
            return null;
        }

        try
        {
            // 读取与解析
            string jsonText = File.ReadAllText(fullPath);
            LevelGraphData graphData = LevelGraphParser.Parse(jsonText);

            if (graphData == null)
            {
                Debug.LogError("❌ [Test] JSON 解析失败。");
                return null;
            }

            // 初始化运行时
            graphData.InitializeRuntimeData();

            // 【关键】注入回 Manager，这样后续逻辑就能通过 Context 访问到了
            manager.CurrentLevelGraph = graphData;

            Debug.Log($"✅ [Test] 自动加载并注入成功: {targetFileName}");
            return graphData;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ [Test] 加载异常: {ex.Message}");
            return null;
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

        if (response.RefereeResult != null)
        {
            var result = response.RefereeResult;

            if (result.PassedNodeIds != null && result.PassedNodeIds.Count > 0)
            {
                string nodesStr = string.Join(", ", result.PassedNodeIds);
                Debug.Log($"🎯 <b>[判定通过的节点]</b>: <color=yellow>{nodesStr}</color>");
            }
            else
            {
                Debug.Log("⚪ [节点] 无节点通过判定。");
            }

            if (result.EntityList != null && result.EntityList.Count > 0)
            {
                string entityStr = string.Join(", ", result.EntityList);
                Debug.Log($"🗝️ <b>[提取到的实体 ID]</b>: <color=cyan>{entityStr}</color>");
            }
            else
            {
                Debug.Log("⚪ [实体] 无关键词提取。");
            }
        }
    }
}