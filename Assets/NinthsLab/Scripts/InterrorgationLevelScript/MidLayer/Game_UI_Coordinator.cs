
using DG.Tweening.Plugins.Options;
using UnityEngine;
using System.Collections.Generic; 

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
            
            // [新增] 监听后端输出
            GameEventDispatcher.OnDialogueGenerated += HandleDialogueGenerated;
            GameEventDispatcher.OnPhaseUnlockEvents += HandlePhaseUnlock;

            GameEventDispatcher.OnDialogueGenerated += HandleDialogueGenerated;
        }

        void OnDisable()
        {
            UIEventDispatcher.OnPlayerSubmitInput -= HandlePlayerSubmitInput;
            
            GameEventDispatcher.OnDialogueGenerated -= HandleDialogueGenerated;
            GameEventDispatcher.OnPhaseUnlockEvents -= HandlePhaseUnlock;

            GameEventDispatcher.OnDialogueGenerated -= HandleDialogueGenerated;
        }

        void Awake()
        {

        }
        
        void HandlePlayerSubmitInput(string input)
        {
            GameEventDispatcher.DispatchPlayerInputString(input);
        }

        void HandleDialogueGenerated(List<string> dialogues)
        {
            Debug.Log($"[Coordinator] 收到 {dialogues.Count} 行对话，转发给 DialogueSystem。");
            
            // 【关键】将数据推入对话系统
            if (dialogueManager != null)
            {
                dialogueManager.PushNewBatch(dialogues);
            }
            else
            {
                Debug.LogError("Coordinator 未绑定 DialogueRuntimeManager！");
            }
        }

        // [修改] 处理阶段解锁
        void HandlePhaseUnlock(string completedName, List<(string id, string name)> nextPhases)
        {
            // 转发给 UI 总线
            UIEventDispatcher.DispatchShowPhaseSelection(completedName, nextPhases);
        }
    }
}