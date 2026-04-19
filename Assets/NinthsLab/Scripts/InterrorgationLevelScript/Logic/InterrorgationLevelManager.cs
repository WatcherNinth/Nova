
using System.Collections.Generic; // 引入 List
using System.IO;
using AIEngine.Network;
using Interrorgation.MidLayer;
using LogicEngine.LevelGraph;
using LogicEngine.Parser;
using UnityEngine;

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

        public PlayerMindMapManager playerMindMapManager;
        private GamePhaseManager gamePhaseManager;
        private NodeLogicManager nodeLogicManager;
        private GameScopeManager gameScopeManager;
        private TemplateLogicManager templateLogicManager;


        private void OnEnable()
        {
            _instance = this;
            // [修改4] 向 Context 注册自己
            LevelGraphContext.Register(this);

            //这个删除
            GameEventDispatcher.OnPlayerInputString += HandlePlayerInput;

            GameEventDispatcher.OnNodeOptionSubmitted += HandleNodeSubmit;
            GameEventDispatcher.OnPlayerRequestPhaseSwitch += HandlePhaseSwitchRequest;
            handleRegister_ScopeManager(true);
        }

        private void OnDisable()
        {
            // [修改5] 向 Context 注销自己
            LevelGraphContext.Unregister(this);

            GameEventDispatcher.OnPlayerInputString -= HandlePlayerInput;

            GameEventDispatcher.OnNodeOptionSubmitted -= HandleNodeSubmit;
            GameEventDispatcher.OnPlayerRequestPhaseSwitch -= HandlePhaseSwitchRequest;
            handleRegister_ScopeManager(false);

            // [新增] 清理逻辑管理器
            templateLogicManager?.Dispose();
            playerMindMapManager?.UnsubscribeEvents();
        }

        private void Start()
        {

        }

        private void HandlePlayerInput(string input)
        {
            AIEventDispatcher.DispatchPlayerInputString(currentLevelGraph, currentPhaseId, input);
        }

        #region 加载关卡
        public static string GetLevelFilePath(string name)
        {
            string relativePath = "Resources/TestResources";
            // 1. 获取完整路径
            string fullPath = Path.Combine(Application.dataPath, relativePath, $"{name}.json");
            if (!File.Exists(fullPath))
            {
                Debug.LogError($"[InterrorgationLevelManager] 找不到关卡文件: {fullPath}");
                return null;
            }
            return fullPath;
        }

        public void LoadLevel(string name)
        {
            string path = GetLevelFilePath(name);
            if (path == null) return;

            // [新增] 在重新加载前清理旧的逻辑管理器，防止事件重复订阅
            templateLogicManager?.Dispose();
            playerMindMapManager?.UnsubscribeEvents();

            string levelJson = File.ReadAllText(path);
            currentLevelGraph = LevelGraphParser.Parse(levelJson);
            currentLevelGraph.InitializeRuntimeData();

            GameEventDispatcher.DispatchLevelGraphLoaded(currentLevelGraph);

            // 1. 初始化 MindMap
            playerMindMapManager = new PlayerMindMapManager(currentLevelGraph);
            playerMindMapManager.SubscribeEvents();

            // 2. 初始化 Phase
            gamePhaseManager = new GamePhaseManager(playerMindMapManager);

            // 3. 初始化 Logic
            nodeLogicManager = new NodeLogicManager(playerMindMapManager);

            // 4. [新增] 初始化 Scope
            gameScopeManager = new GameScopeManager(playerMindMapManager);

            // 5. [新增] 初始化 Template Logic
            templateLogicManager = new TemplateLogicManager(playerMindMapManager, nodeLogicManager);

            // 6. 注入依赖 (互相连接)
            nodeLogicManager.SetPhaseManager(gamePhaseManager);
            nodeLogicManager.SetScopeManager(gameScopeManager); // Logic -> Scope
            gameScopeManager.SetLogicManager(nodeLogicManager); // Scope -> Logic

            GameEventDispatcher.DispatchLogicInitialized();

            // 7. 启动逻辑
            if (currentLevelGraph.levelStartDialogue != null)
            {
                GameEventDispatcher.DispatchDialogueGenerated(new List<string> { currentLevelGraph.levelStartDialogue });
            }
        }
        #endregion

        // [新增] 处理节点提交
        private void HandleNodeSubmit(string nodeId)
        {
            if (nodeLogicManager != null)
            {
                // 调用 LogicManager 进行验证
                bool success = nodeLogicManager.TryProveNode(nodeId);
                if (success)
                {
                    Debug.Log($"[LevelManager] 节点 {nodeId} 证明成功。");
                    nodeLogicManager.OnProveSuccess(nodeId);
                }
                else
                {
                    Debug.Log($"[LevelManager] 节点 {nodeId} 证明失败。");
                    nodeLogicManager.OnProveFailed(nodeId);
                }
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
        public string getcurrrentPhaseId()
        {
            return currentPhaseId;
        }

        private void handleRegister_ScopeManager(bool register)
        {
            if (register)
            {
                GameEventDispatcher.OnGetCurrentScopeStack += handleGetCurrentScopeStack;
            }
            else
            {
                GameEventDispatcher.OnGetCurrentScopeStack -= handleGetCurrentScopeStack;
            }
        }

        private List<string> handleGetCurrentScopeStack()
        {
            if (gameScopeManager != null)
            {
                return gameScopeManager.GetCurrentScopeStack();
            }
            return null;
        }
    }
}