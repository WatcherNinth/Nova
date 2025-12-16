
using UnityEngine;
using LogicEngine.LevelGraph;
using Interrorgation.MidLayer;
using AIEngine.Network;
using System.IO;
using LogicEngine.Parser;

using System.Collections.Generic; // 引入 List

namespace LogicEngine.LevelLogic
{
    public class InterrorgationLevelManager : MonoBehaviour, ILevelGraphProvider
    {
        #region 单例
        // ==========================================
        // 稳健的单例模式 (适配 Edit Mode)
        // ==========================================
        private static InterrorgationLevelManager _instance;

        public static InterrorgationLevelManager Instance
        {
            get
            {
                // 如果为空，尝试在场景中寻找
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<InterrorgationLevelManager>();
                }
                return _instance;
            }
            private set
            {
                _instance = value;
            }
        }

        #endregion
        #region ILevelGraphProvider实现

        public int Priority => 0;

        public LevelGraphData GetLevelGraph()
        {
            return currentLevelGraph;
        }
        #endregion
        private LevelGraphData currentLevelGraph;
        private string currentPhaseId;

        private PlayerMindMapManager playerMindMapManager;
        private void OnEnable()
        {
            _instance = this;
            // [修改4] 向 Context 注册自己
            LevelGraphContext.Register(this);

            GameEventDispatcher.OnPlayerInputString += HandlePlayerInput;
            AIEventDispatcher.OnResponseReceived += HandleResponseReceived;

            GameEventDispatcher.OnNodeOptionSubmitted += HandleNodeSubmit;
            GameEventDispatcher.OnPlayerSubmitTemplateAnswer += HandleTemplateSubmit; // [新增]
        }

        private void OnDisable()
        {
            // [修改5] 向 Context 注销自己
            LevelGraphContext.Unregister(this);

            GameEventDispatcher.OnPlayerInputString -= HandlePlayerInput;
            AIEventDispatcher.OnResponseReceived -= HandleResponseReceived;

            GameEventDispatcher.OnNodeOptionSubmitted -= HandleNodeSubmit;
            GameEventDispatcher.OnPlayerSubmitTemplateAnswer -= HandleTemplateSubmit; // [新增]
        }

        private void Start()
        {
            
        }

        private void HandlePlayerInput(string input)
        {
            AIEventDispatcher.DispatchPlayerInputString(currentLevelGraph, currentPhaseId, input);
        }

        private void HandleResponseReceived(AIResponseData responseData)
        {
            if (playerMindMapManager == null)
            {
                Debug.LogWarning("[InterrorgationLevelManager] 收到 AI 响应，但关卡尚未初始化 (playerMindMapManager is null)。忽略此次更新。");
                return;
            }
            playerMindMapManager.ProcessAIResponse(responseData);
        }
        #region 加载关卡
        public static string GetLevelFilePath(string name)
        {
            string relativePath = "Resources/TestResources";
            // 1. 获取完整路径
            string fullPath = Path.Combine(Application.dataPath, relativePath, $"{name}.json");
            if (!Directory.Exists(fullPath))
            {
                Debug.LogError($"[InterrorgationLevelManager] 找不到关卡文件路径: {fullPath}");
                return null;
            }
            return fullPath;
        }

        public void LoadLevel(string name)
        {
            string levelJson = File.ReadAllText(GetLevelFilePath(name));
            currentLevelGraph = LevelGraphParser.TryParseAndInit(levelJson);
            playerMindMapManager = new PlayerMindMapManager(ref currentLevelGraph);
        }
        #endregion

        // [新增] 处理节点提交
        private void HandleNodeSubmit(string nodeId)
        {
            if (playerMindMapManager != null)
            {
                bool success = playerMindMapManager.TryProveNode(nodeId);
                // 可以在这里加 Log 输出结果
            }
        }

        // [新增] 启动逻辑 (激活初始阶段)
        public void StartGameLogic()
        {
            if (playerMindMapManager != null)
            {
                // 默认激活 phase1
                playerMindMapManager.SetPhaseStatus("phase1", RuntimePhaseStatus.Active);
                currentPhaseId = "phase1";
            }
        }       

        private void HandleTemplateSubmit(string templateId, List<string> inputs)
        {
            if (playerMindMapManager != null)
            {
                // 1. 验证答案
                string targetNodeId = playerMindMapManager.ValidateTemplateAnswer(templateId, inputs);
                
                if (!string.IsNullOrEmpty(targetNodeId))
                {
                    Debug.Log($"[LevelManager] 填空验证成功！目标节点: {targetNodeId}");
                    // 2. 验证成功，尝试证明节点
                    bool success = playerMindMapManager.TryProveNode(targetNodeId);
                    if (!success)
                    {
                        Debug.LogWarning($"[LevelManager] 填空正确但无法证明节点 (可能前置条件未满足)。");
                    }
                }
                else
                {
                    Debug.Log($"[LevelManager] 填空验证失败。");
                    // 这里可以触发一个 UI 事件通知玩家“答案错误”
                }
            }
        } 
    }
}