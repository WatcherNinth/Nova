using UnityEngine;
using System.IO;
using LogicEngine;
using LogicEngine.LevelGraph;
using LogicEngine.Parser;
using LogicEngine.Tests; // 引用 LevelTestManager
using AIEngine.Prompts;
using AIEngine.Network;

namespace AIEngine.Tests
{
    [RequireComponent(typeof(AIClient))]
    public class AIEngineUnifiedTester : MonoBehaviour
    {
        [Header("1. 文件加载设置")]
        [Tooltip("输入要测试的文件名 (必须在 LevelTestManager 指定的 Resources/Levels 文件夹下)")]
        public string targetFileName = "demo_v2.json";

        [Header("2. 模拟环境")]
        [Tooltip("模拟当前处于哪个阶段 (例如 phase1)")]
        public string currentPhaseId = "phase1";

        [Tooltip("模拟玩家发送给 AI 的话")]
        [TextArea(3, 5)]
        public string playerInput = "十五楼的血迹是谁的？";

        [Header("3. AI 配置")]
        public string modelName = "qwen3-max-2025-09-23";

        [Header("4. 本地验证预期")]
        [Tooltip("Prompt 中必须包含的关键词 (验证是否包含当前阶段节点)")]
        public string mustContainString = "fifteenth_floor";
        [Tooltip("Prompt 中不能包含的关键词 (验证是否屏蔽了剧透节点)")]
        public string mustNotContainString = "murderer_did_it";

        // --- 内部状态 ---
        private AIClient _aiClient;
        private string _cachedJsonPayload; // 缓存生成的 JSON，供步骤 2 使用

        private void Awake()
        {
            _aiClient = GetComponent<AIClient>();
        }

        // =========================================================
        // 按钮 1 功能：自动加载文件 -> 生成 Prompt -> 本地验证
        // =========================================================
        public void GenerateAndVerify()
        {
            Debug.Log($"<color=yellow>=== [步骤 1] 加载 '{targetFileName}' 并生成 Prompt ===</color>");

            // --- A. 自动加载逻辑 (集成自 LevelTestManager) ---
            if (!LoadLevelData())
            {
                return; // 加载失败，中断流程
            }

            // --- B. 获取数据 (此时 Context 中已有数据) ---
            LevelGraphData graphData = LevelGraphContext.CurrentGraph;

            // --- C. 构建 Prompt ---
            AIPromptData promptData = AIPromptBuilder.Build(graphData, currentPhaseId, playerInput);

            // --- D. 本地验证 ---
            bool passIncluded = string.IsNullOrEmpty(mustContainString) || promptData.DynamicContext.Contains(mustContainString);
            bool passExcluded = string.IsNullOrEmpty(mustNotContainString) || !promptData.DynamicContext.Contains(mustNotContainString);

            if (passIncluded && passExcluded)
                Debug.Log("✅ [本地验证] Prompt 内容符合预期规则。");
            else
                Debug.LogError($"❌ [本地验证] 失败！\n包含 '{mustContainString}': {passIncluded}\n排除 '{mustNotContainString}': {passExcluded}");

            // --- E. 构建 Request JSON 并缓存 ---
            _cachedJsonPayload = AIRequestBuilder.ConstructPayload(promptData, modelName);

            // --- F. 打印预览 ---
            Debug.Log($"<b>[Prompt Context 预览]</b>:\n<color=cyan>{Truncate(promptData.DynamicContext, 500)}</color>");
            Debug.Log($"<b>[JSON Payload 已准备就绪]</b> (长度: {_cachedJsonPayload.Length})。请点击 [步骤 2] 发送请求。");
        }

        /// <summary>
        /// 核心复用逻辑：从 LevelTestManager 的路径读取文件并注入
        /// </summary>
        private bool LoadLevelData()
        {
            var testManager = LevelTestManager.Instance;
            if (testManager == null)
            {
                Debug.LogError("❌ [错误] 场景中找不到 LevelTestManager！请确保它存在且已激活。");
                return false;
            }

            // 拼接路径：利用 LevelTestManager 配置的 relativePath
            string folderPath = Path.Combine(Application.dataPath, testManager.relativePath);
            string fullPath = Path.Combine(folderPath, targetFileName);

            if (!File.Exists(fullPath))
            {
                Debug.LogError($"❌ [错误] 找不到文件: {fullPath}\n请检查文件名拼写或 LevelTestManager 的路径配置。");
                return false;
            }

            try
            {
                string jsonText = File.ReadAllText(fullPath);
                LevelGraphData graphData = LevelGraphParser.Parse(jsonText);

                if (graphData == null)
                {
                    Debug.LogError("❌ [错误] JSON 解析失败，返回 null。");
                    return false;
                }

                // 必须初始化运行时数据
                graphData.InitializeRuntimeData();
                
                // 【注入数据】这样 LevelGraphContext.CurrentGraph 就能访问到了
                testManager.CurrentLevelGraph = graphData;
                Debug.Log($"✅ [数据注入] 成功加载文件: {targetFileName} (包含节点数: {graphData.nodeLookup.Count})");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ [异常] 加载文件时出错: {ex.Message}");
                return false;
            }
        }

        // =========================================================
        // 按钮 2 功能：发送缓存的 JSON 到服务器
        // =========================================================
        public void SendRequest()
        {
            Debug.Log("<color=yellow>=== [步骤 2] 发送网络请求 ===</color>");

            if (string.IsNullOrEmpty(_cachedJsonPayload))
            {
                Debug.LogError("❌ [流程错误] 没有可发送的数据！请先点击 [步骤 1] 按钮生成数据。");
                return;
            }

            if (_aiClient == null) _aiClient = GetComponent<AIClient>();

            Debug.Log($"🚀 正在向服务器发送请求 (Model: {modelName})...");
            
            // 发送请求，并注册回调
            _aiClient.SendRequest(_cachedJsonPayload, OnSuccess, OnFailure);
        }

        // --- 成功回调：接收解析结果 + 原始 JSON ---
        private void OnSuccess(AIRefereeResult result, string rawJson)
        {
            Debug.Log("<color=green>✅ [请求成功 200 OK]</color>");
            
            // 打印 AI 的推理过程
            Debug.Log($"<b>[AI 思考 (Reasoning)]</b>:\n{result.Reasoning}");
            
            // 打印节点判定详情
            if(result.NodeConfidence != null && result.NodeConfidence.Count > 0)
            {
                string confStr = "";
                foreach (var kvp in result.NodeConfidence)
                {
                    // 高亮显示高置信度 (>= 0.8) 的结果
                    string color = kvp.Value >= 0.8f ? "green" : "grey";
                    confStr += $"<color={color}>{kvp.Key}: {kvp.Value}</color>\n";
                }
                Debug.Log($"<b>[节点判定]</b>:\n{confStr}");
            }
            else
            {
                Debug.Log("<b>[节点判定]</b>: 无结果");
            }

            // 打印关键词提取详情
            if(result.PartialMatch != null && result.PartialMatch.Count > 0)
            {
                string matchStr = "";
                foreach (var kvp in result.PartialMatch)
                {
                    matchStr += $"{kvp.Key}: [{string.Join(", ", kvp.Value)}]\n";
                }
                Debug.Log($"<b>[关键词提取]</b>:\n{matchStr}");
            }
            else
            {
                Debug.Log("<b>[关键词提取]</b>: 无结果");
            }

            // 可选：打印原始 JSON (用于 Debug)
            // Debug.Log($"[Raw Response]: {rawJson}");
        }

        // --- 失败回调：接收状态码 + 错误信息 ---
        private void OnFailure(long responseCode, string error)
        {
            Debug.LogError($"❌ [请求失败] HTTP状态码: {responseCode}\n错误详情: {error}");
        }

        // --- 辅助方法 ---
        private string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Length > max ? s.Substring(0, max) + " ...[省略]" : s;
        }
    }
}