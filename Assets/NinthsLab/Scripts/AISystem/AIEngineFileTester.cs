using UnityEngine;
using System.IO;
using LogicEngine;
using LogicEngine.LevelGraph;
using LogicEngine.Parser;
using LogicEngine.Tests;
using AIEngine.Prompts;
using AIEngine.Network;

namespace AIEngine.Tests
{
    // 这里不需要 [ExecuteInEditMode] 了，因为我们要用 Play Mode 来模拟真实运行
    public class AIEngineAutoTester : MonoBehaviour
    {
        [Header("1. 测试配置")]
        [Tooltip("要测试的文件名 (必须位于 LevelTestManager 配置的路径下)")]
        public string targetFileName = "demo_v2.json";

        [Tooltip("是否在点击 Play 时自动运行测试")]
        public bool runOnStart = true;

        [Header("2. AI 模拟环境")]
        public string currentPhaseId = "phase1";
        [TextArea(2, 5)]
        public string playerInput = "十五楼的血迹是谁的？";

        [Header("3. 验证预期")]
        public string mustContainString = "fifteenth_floor";
        public string mustNotContainString = "murderer_did_it";

        // Unity 生命周期：点击播放时自动调用
        void Start()
        {
            if (runOnStart)
            {
                RunTest();
            }
        }

        // 也保留手动按钮，方便在运行过程中随时再次测试
        [ContextMenu("手动运行测试")]
        public void RunTest()
        {
            Debug.Log($"<color=cyan>[AIEngineAutoTester] 开始对 {targetFileName} 进行自动化测试...</color>");

            // 1. 获取管理器配置
            if (LevelTestManager.Instance == null)
            {
                Debug.LogError("❌ [错误] 场景中未找到 LevelTestManager！无法获取路径配置。");
                return;
            }

            // 2. 拼接路径 (利用 Manager 中的配置)
            string relativePath = LevelTestManager.Instance.relativePath;
            string fullPath = Path.Combine(Application.dataPath, relativePath, targetFileName);

            if (!File.Exists(fullPath))
            {
                Debug.LogError($"❌ [错误] 文件不存在: {fullPath}");
                return;
            }

            // 3. 读取与解析
            try
            {
                string jsonText = File.ReadAllText(fullPath);
                LevelGraphData graphData = LevelGraphParser.Parse(jsonText);

                if (graphData == null)
                {
                    Debug.LogError("❌ [错误] JSON 解析失败。");
                    return;
                }

                // 4. 初始化数据并注入 (模拟游戏加载完成)
                graphData.InitializeRuntimeData();
                LevelTestManager.Instance.CurrentLevelGraph = graphData;

                Debug.Log($"✅ [数据注入] 已将剧本数据注入 LevelTestManager。节点数: {graphData.nodeLookup.Count}");

                // 5. 执行 AI 逻辑
                TestPromptGeneration(graphData);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ [异常] 测试中断: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void TestPromptGeneration(LevelGraphData data)
        {
            // --- 构建 Prompt ---
            AIPromptData promptData = AIPromptBuilder.Build(data, currentPhaseId, playerInput);

            // --- 验证 ---
            bool passIncluded = string.IsNullOrEmpty(mustContainString) || promptData.DynamicContext.Contains(mustContainString);
            bool passExcluded = string.IsNullOrEmpty(mustNotContainString) || !promptData.DynamicContext.Contains(mustNotContainString);

            if (passIncluded && passExcluded)
            {
                Debug.Log($"<color=green>✅ [测试通过] Prompt 验证成功！</color>");
            }
            else
            {
                Debug.LogError($"❌ [测试失败] 内容验证未通过。\n包含 '{mustContainString}': {passIncluded}\n排除 '{mustNotContainString}': {passExcluded}");
            }

            // --- 打印结果 (截断显示) ---
            string preview = promptData.DynamicContext.Length > 500 
                ? promptData.DynamicContext.Substring(0, 500) + "...(更多内容已隐藏)" 
                : promptData.DynamicContext;

            Debug.Log($"<b>[Prompt Context 预览]</b>:\n<color=yellow>{preview}</color>");
            
            // --- 构建 Request ---
            string requestJson = AIRequestBuilder.ConstructPayload(promptData);
            Debug.Log($"<b>[Request JSON]</b> (长度: {requestJson.Length}):\n{requestJson}");
        }
    }
}