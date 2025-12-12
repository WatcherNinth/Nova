using UnityEngine;
using LogicEngine;
using LogicEngine.LevelGraph;
using LogicEngine.Tests; // 引用 LevelTestManager
using AIEngine;          // 引用 Dispatcher
using AIEngine.Logic;    // 引用 AIRefereeModel
using AIEngine.Network;  // 引用 AIResponseData
using Interrorgation.MidLayer;

public class AIFullFlowDebug : MonoBehaviour
{
    [Header("1. 测试环境")]
    [Tooltip("模拟当前阶段 ID")]
    public string phaseId = "phase1";
    
    [Tooltip("模拟玩家输入")]
    [TextArea(3, 5)]
    public string playerInput = "十五楼的血迹是谁的？";

    [Header("2. 调试开关")]
    public bool printFullJson = true;

    // =========================================================
    // 生命周期：订阅与取消订阅最终结果事件
    // =========================================================
    private void OnEnable()
    {
        Debug.Log("<color=cyan>[Test] 已开始监听 OnResponseReceived 事件...</color>");
        AIEventDispatcher.OnResponseReceived += OnFinalResultReceived;
    }

    private void OnDisable()
    {
        AIEventDispatcher.OnResponseReceived -= OnFinalResultReceived;
    }

    // =========================================================
    // 核心测试逻辑
    // =========================================================
    [ContextMenu("🔥 运行全链路测试 (Run Full Flow)")]
    public void RunTest()
    {
        Debug.ClearDeveloperConsole(); // 清空控制台，方便查看
        Debug.Log("<color=yellow>=== 🎬 开始 AI 全链路流程测试 ===</color>");

        // --- 步骤 1: 获取数据源 ---
        LevelGraphData graphData = LevelGraphContext.CurrentGraph;
        
        // 自动容错：如果没有数据，尝试从 LevelTestManager 获取
        if (graphData == null && LevelTestManager.Instance != null)
        {
            graphData = LevelTestManager.Instance.CurrentLevelGraph;
        }

        if (graphData == null || graphData.nodeLookup == null || graphData.nodeLookup.Count == 0)
        {
            Debug.LogError("❌ [步骤 1 失败] 缺少剧本数据！\n请先运行 LevelTestManager 加载剧本。");
            return;
        }
        Debug.Log($"✅ [步骤 1: 数据准备] 获取到剧本数据，节点数量: {graphData.nodeLookup.Count}");

        // --- 步骤 2: 验证 Prompt 和 Request (预演) ---
        // 虽然 AIManager 会自动做这一步，但为了“输出每一步数据”，我们在这里手动调一次看结果
        Debug.Log("🔍 [步骤 2: 数据预演] 正在尝试构建 Request Payload...");
        
        string payloadPreview = AIRefereeModel.CreateRequestPayload(graphData, phaseId, playerInput);
        
        if (string.IsNullOrEmpty(payloadPreview))
        {
            Debug.LogError("❌ [步骤 2 失败] Request Payload 构建结果为空！");
            return;
        }
        
        if (printFullJson)
        {
            Debug.Log($"📄 [步骤 2: 数据内容] 即将发送的 JSON:\n<color=grey>{payloadPreview}</color>");
        }
        else
        {
            Debug.Log($"✅ [步骤 2: 数据内容] JSON 构建成功 (长度: {payloadPreview.Length})");
        }

        // --- 步骤 3: 触发事件 (正式开始) ---
        Debug.Log($"🚀 [步骤 3: 触发事件] 正在分发 OnPlayerInputString 事件...\n输入内容: {playerInput}");
        
        // 这行代码会唤醒 AIManager -> 调用 AIClient -> 发送网络请求
        AIEventDispatcher.DispatchPlayerInputString(graphData, phaseId, playerInput);
        
        Debug.Log("⏳ [步骤 4: 等待网络] 请求已发出，正在等待回调...");
    }

    // =========================================================
    // 回调处理
    // =========================================================
    private void OnFinalResultReceived(AIResponseData response)
    {
        Debug.Log("<color=green>=== 🎉 全链路跑通！收到最终结果 ===</color>");

        if (response.HasError)
        {
            Debug.LogError($"❌ [结果异常] {response.ErrorMessage}");
            return;
        }

        // 检查是否有 Referee 结果
        if (response.RefereeResult != null)
        {
            var result = response.RefereeResult;

            Debug.Log($"✅ <b>[Referee Result]</b> 收到裁判结果：");
            // Debug.Log($"🧠 <b>[Reasoning]</b>:\n{result.Reasoning}");

            // 打印通过的节点列表
            if (result.PassedNodeIds != null && result.PassedNodeIds.Count > 0)
            {
                string passedNodesStr = string.Join(", ", result.PassedNodeIds);
                Debug.Log($"🎯 <b>[通过判定的节点 (Passed Nodes)]</b>:\n<color=cyan>{passedNodesStr}</color>");
            }
            else
            {
                Debug.Log("⚠️ <b>[Node]</b>: 没有节点通过判定阈值。");
            }

            // 打印关键词
            if (result.EntityList != null && result.EntityList.Count > 0)
            {
                string entitiesStr = string.Join(", ", result.EntityList);
                Debug.Log($"🗝️ <b>[Entity List]</b>:\n<color=yellow>[{entitiesStr}]</color>");
            }
            else
            {
                Debug.Log("ℹ️ [Entity List] 为空");
            }
        }
        else
        {
            Debug.LogWarning("AIResponseData 中不包含 RefereeResult (可能是其他类型的 AI 返回)");
        }
        
        Debug.Log("<color=yellow>=== 测试结束 ===</color>");
    }
}