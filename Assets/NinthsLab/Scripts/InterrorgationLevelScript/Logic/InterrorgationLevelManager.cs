
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
        private GamePhaseManager gamePhaseManager;
        private NodeLogicManager nodeLogicManager;
        private GameScopeManager gameScopeManager;
        private void OnEnable()
        {
            _instance = this;
            // [修改4] 向 Context 注册自己
            LevelGraphContext.Register(this);

            GameEventDispatcher.OnPlayerInputString += HandlePlayerInput;
            AIEventDispatcher.OnResponseReceived += HandleResponseReceived;

            GameEventDispatcher.OnNodeOptionSubmitted += HandleNodeSubmit;
            GameEventDispatcher.OnPlayerSubmitTemplateAnswer += HandleTemplateSubmit; // [新增]
            GameEventDispatcher.OnPlayerRequestPhaseSwitch += HandlePhaseSwitchRequest;
        }

        private void OnDisable()
        {
            // [修改5] 向 Context 注销自己
            LevelGraphContext.Unregister(this);

            GameEventDispatcher.OnPlayerInputString -= HandlePlayerInput;
            AIEventDispatcher.OnResponseReceived -= HandleResponseReceived;

            GameEventDispatcher.OnNodeOptionSubmitted -= HandleNodeSubmit;
            GameEventDispatcher.OnPlayerSubmitTemplateAnswer -= HandleTemplateSubmit; // [新增]
            GameEventDispatcher.OnPlayerRequestPhaseSwitch -= HandlePhaseSwitchRequest;
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
            if (playerMindMapManager == null) return;
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
            string path = GetLevelFilePath(name);
            if(path == null) return;

            string levelJson = File.ReadAllText(path);
            currentLevelGraph = LevelGraphParser.Parse(levelJson);
            currentLevelGraph.InitializeRuntimeData();

            // 1. 初始化 MindMap
            playerMindMapManager = new PlayerMindMapManager(currentLevelGraph);

            // 2. 初始化 Phase
            gamePhaseManager = new GamePhaseManager(playerMindMapManager);

            // 3. 初始化 Logic
            nodeLogicManager = new NodeLogicManager(playerMindMapManager);
            
            // 4. [新增] 初始化 Scope
            gameScopeManager = new GameScopeManager(playerMindMapManager);

            // 5. 注入依赖 (互相连接)
            nodeLogicManager.SetPhaseManager(gamePhaseManager);
            nodeLogicManager.SetScopeManager(gameScopeManager); // Logic -> Scope
            gameScopeManager.SetLogicManager(nodeLogicManager); // Scope -> Logic
        }
        #endregion

        // [新增] 处理节点提交
        private void HandleNodeSubmit(string nodeId)
        {
            if (nodeLogicManager != null)
            {
                // 调用 LogicManager
                bool success = nodeLogicManager.TryProveNode(nodeId);
                if (success) Debug.Log($"[LevelManager] 节点 {nodeId} 证明成功。");
            }
        }
        // [新增] 启动逻辑 (激活初始阶段)
        public void StartGameLogic()
        {
            if (gamePhaseManager != null)
            {
                // [修改] 调用 PhaseManager
                gamePhaseManager.SetPhaseStatus("phase1", RuntimePhaseStatus.Active);
                currentPhaseId = "phase1";
            }
        } 

        private void HandleTemplateSubmit(string templateId, List<string> inputs)
        {
            if (playerMindMapManager != null && nodeLogicManager != null)
            {
                // 1. MindMap 验证
                string targetNodeId = playerMindMapManager.ValidateTemplateAnswer(templateId, inputs);
                
                if (!string.IsNullOrEmpty(targetNodeId))
                {
                    // 2. Logic 证明
                    nodeLogicManager.TryProveNode(targetNodeId);
                }
                else
                {
                    Debug.Log($"[LevelManager] 填空验证失败。");
                }
            }
        }

        private void HandlePhaseSwitchRequest(string targetPhaseId)
        {
            if (gamePhaseManager != null)
            {
                bool success = gamePhaseManager.SwitchToPhase(targetPhaseId);
                if (success)
                {
                    Debug.Log($"[LevelManager] 成功切换至阶段: {targetPhaseId}");
                    currentPhaseId = targetPhaseId; // 同步本地记录
                }
                else
                {
                    Debug.LogWarning($"[LevelManager] 切换阶段失败: {targetPhaseId}");
                }
            }
        }
    }
}