// using UnityEngine;
// using LogicEngine; 
// using LogicEngine.LevelGraph;
// using AIEngine.Prompts;
// using AIEngine.Network;

// namespace AIEngine.Tests
// {
//     [RequireComponent(typeof(AIClient))] 
//     public class AIEngineNetworkTester : MonoBehaviour
//     {
//         [Header("1. 模型配置")]
//         [Tooltip("指定要使用的 LLM 模型名称，例如 qwen-plus, qwen-max, gpt-4")]
//         public string modelName = "qwen3-max-2025-09-23";

//         [Header("2. 模拟环境")]
//         [Tooltip("模拟当前处于哪个阶段")]
//         public string currentPhaseId = "phase1";

//         [Tooltip("模拟玩家输入")]
//         [TextArea(3, 5)]
//         public string playerInput = "十五楼的血迹是谁的？";

//         [Header("3. 调试反馈")]
//         [Tooltip("是否在控制台打印详细步骤")]
//         public bool showDebugLogs = true;

//         private AIClient _aiClient;

//         private void Awake()
//         {
//             _aiClient = GetComponent<AIClient>();
//         }

//         [ContextMenu("🚀 发送 AI 请求 (Send Request)")]
//         public void RunFullFlow()
//         {
//             if (showDebugLogs) Debug.Log("<color=yellow>=== 开始 AI 全流程网络测试 ===</color>");

//             // --- 步骤 1: 获取剧本数据 ---
//             LevelGraphData graphData = LevelGraphContext.CurrentGraph;
//             if (graphData == null || graphData.nodeLookup == null || graphData.nodeLookup.Count == 0)
//             {
//                 Debug.LogError("❌ [流程终止] 缺少剧本数据！\n请先运行 LevelTestManager 加载一个剧本。");
//                 return;
//             }

//             // --- 步骤 2: 构建 Prompt 数据 ---
//             if (showDebugLogs) Debug.Log("1. 正在构建 Prompt...");
//             var promptData = AIPromptBuilder.Build(graphData, currentPhaseId, playerInput);

//             // --- 步骤 3: 构建 Request JSON ---
//             if (showDebugLogs) Debug.Log($"2. 正在构建 Request Body (Model: {modelName})...");
//             string jsonPayload = AIRequestBuilder.ConstructPayload(promptData, modelName);

//             if (string.IsNullOrEmpty(jsonPayload))
//             {
//                 Debug.LogError("❌ [流程终止] JSON 构建失败。");
//                 return;
//             }

//             // --- 步骤 4: 发送网络请求 ---
//             if (showDebugLogs) Debug.Log("3. 正在发送网络请求...");
            
//             if (_aiClient == null) _aiClient = GetComponent<AIClient>();
            
//             // 这里调用 SendRequest，下面的 OnSuccess 和 OnFailure 签名必须匹配 AIClient 的定义
//             _aiClient.SendRequest(jsonPayload, OnSuccess, OnFailure);
//         }

//         // =================================================
//         // 回调处理 (此处进行了修改以匹配新的 AIClient)
//         // =================================================
        
//         // 修改点 1: 增加 string rawJson 参数
//         private void OnSuccess(AIRefereeResult result, string rawJson)
//         {
//             Debug.Log("<color=green>✅ [请求成功] AI 返回结果如下：</color>");
            
//             // 打印 AI 的思考过程
//             Debug.Log($"<b>[AI 思考 (Reasoning)]</b>:\n{result.Reasoning}");

//             // 打印节点置信度
//             if (result.NodeConfidence != null)
//             {
//                 string confidenceStr = "";
//                 foreach (var kvp in result.NodeConfidence)
//                 {
//                     // 高亮显示高置信度的结果
//                     string color = kvp.Value > 0.7f ? "green" : "grey";
//                     confidenceStr += $"<color={color}>{kvp.Key}: {kvp.Value}</color>\n";
//                 }
//                 Debug.Log($"<b>[节点判定]</b>:\n{confidenceStr}");
//             }

//             // 打印关键词提取
//             if (result.PartialMatch != null && result.PartialMatch.Count > 0)
//             {
//                 string matchStr = "";
//                 foreach (var kvp in result.PartialMatch)
//                 {
//                     matchStr += $"{kvp.Key}: [{string.Join(", ", kvp.Value)}]\n";
//                 }
//                 Debug.Log($"<b>[关键词提取]</b>:\n{matchStr}");
//             }
//         }

//         // 修改点 2: 增加 long responseCode 参数
//         private void OnFailure(long responseCode, string error)
//         {
//             Debug.LogError($"❌ [请求失败] 状态码: {responseCode}\n错误信息: {error}");
//         }
//     }
// }