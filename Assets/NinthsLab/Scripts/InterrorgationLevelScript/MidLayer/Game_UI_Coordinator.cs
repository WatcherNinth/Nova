
using DG.Tweening.Plugins.Options;
using UnityEngine;

namespace Interrorgation.MidLayer
{
    public class Game_UI_Coordinator : MonoBehaviour
    {
        #region 单例
        // ==========================================
        // 稳健的单例模式 (适配 Edit Mode)
        // ==========================================
        private static Game_UI_Coordinator _instance;

        public static Game_UI_Coordinator Instance
        {
            get
            {
                // 如果为空，尝试在场景中寻找
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<Game_UI_Coordinator>();
                }
                return _instance;
            }
            private set
            {
                _instance = value;
            }
        }

        #endregion

        void OnEnable()
        {
            UIEventDispatcher.OnPlayerSubmitInput += HandlePlayerSubmitInput;
        }

        void OnDisable()
        {
            UIEventDispatcher.OnPlayerSubmitInput -= HandlePlayerSubmitInput;
        }

        void Awake()
        {

        }
        
        void HandlePlayerSubmitInput(string input)
        {
            GameEventDispatcher.DispatchPlayerInputString(input);
        }
    }
}