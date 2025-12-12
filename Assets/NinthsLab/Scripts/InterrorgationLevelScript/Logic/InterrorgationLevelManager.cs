
using UnityEngine;
using LogicEngine.LevelGraph;
using Interrorgation.MidLayer;
using AIEngine.Network;
using System.IO;
using LogicEngine.Parser;

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
        }

        private void OnDisable()
        {
            // [修改5] 向 Context 注销自己
            LevelGraphContext.Unregister(this);

            GameEventDispatcher.OnPlayerInputString -= HandlePlayerInput;
            AIEventDispatcher.OnResponseReceived -= HandleResponseReceived;
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
    }
}