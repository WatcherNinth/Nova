
using UnityEngine;
using LogicEngine.LevelGraph;
using Interrorgation.MidLayer;

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
        private LevelGraphData currentLevelGraph;
        public int Priority => 0;

        public LevelGraphData GetLevelGraph()
        {
            return currentLevelGraph;
        }
        #endregion

        private void OnEnable()
        {
            _instance = this;
            // [修改4] 向 Context 注册自己
            LevelGraphContext.Register(this);

            GameEventDispatcher.OnPlayerInputString += HandlePlayerInput;
        }

        private void OnDisable()
        {
            // [修改5] 向 Context 注销自己
            LevelGraphContext.Unregister(this);

            GameEventDispatcher.OnPlayerInputString -= HandlePlayerInput;
        }

        private void Start()
        {

        }

        private void HandlePlayerInput(string input)
        {
            
        }
    }
}