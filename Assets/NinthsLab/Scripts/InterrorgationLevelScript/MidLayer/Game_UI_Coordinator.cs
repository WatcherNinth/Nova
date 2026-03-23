
using DG.Tweening.Plugins.Options;
using UnityEngine;
using System.Collections.Generic;
using DialogueSystem;

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

        [Header("References")]
        [SerializeField] private DialogueRuntimeManager dialogueManager;

        void OnEnable()
        {
            UIEventDispatcher.OnPlayerSubmitInput += HandlePlayerSubmitInput;
            // [UI -> Game] 模板提交
            UIEventDispatcher.OnPlayerSubmitTemplateAnswer += HandlePlayerSubmitTemplateAnswer;
            
            // [新增] 监听后端输出
            GameEventDispatcher.OnDialogueGenerated += HandleDialogueGenerated;
            GameEventDispatcher.OnPhaseUnlockEvents += HandlePhaseUnlock;
            // [Game -> UI] 模板发现
            GameEventDispatcher.OnDiscoveredNewTemplates += HandleDiscoveredTemplates;
        }

        void OnDisable()
        {
            UIEventDispatcher.OnPlayerSubmitInput -= HandlePlayerSubmitInput;
            UIEventDispatcher.OnPlayerSubmitTemplateAnswer -= HandlePlayerSubmitTemplateAnswer;
            
            GameEventDispatcher.OnDialogueGenerated -= HandleDialogueGenerated;
            GameEventDispatcher.OnPhaseUnlockEvents -= HandlePhaseUnlock;
            GameEventDispatcher.OnDiscoveredNewTemplates -= HandleDiscoveredTemplates;
        }

        void Awake()
        {

        }
        
        void HandlePlayerSubmitInput(string input)
        {
            GameEventDispatcher.DispatchPlayerInputString(input);
        }

        // 处理 UI 侧发起的模板提交
        void HandlePlayerSubmitTemplateAnswer(string templateId, List<string> answers)
        {
            // UI -> Game
            GameEventDispatcher.DispatchPlayerSubmitTemplateAnswer(templateId, answers);
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

        // 处理逻辑侧分发的发现新模板
        private void HandleDiscoveredTemplates(List<LogicEngine.LevelLogic.RuntimeTemplateData> templates)
        {
            // Game -> UI
            UIEventDispatcher.DispatchDiscoveredNewTemplates(templates);
        }
    }
}