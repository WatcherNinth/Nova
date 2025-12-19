
using DG.Tweening.Plugins.Options;
using UnityEngine;
using System.Collections.Generic;
using Interrorgation.MidLayer.Dialogue;

namespace Interrorgation.MidLayer
{
    /// <summary>
    /// 游戏UI协调器 - 单一中间层入口
    /// 职责:
    ///   1. 作为游戏逻辑 ↔ UI系统的唯一联接点
    ///   2. 管理后端事件和前端事件总线的生命周期
    ///   3. 初始化并管理对话系统的两个适配器
    /// 数据流:
    ///   后端逻辑 → GameEventDispatcher.OnDialogueGenerated → 本类 → DialogueLogicAdapter → FrontendDialogueEventBus → UI
    ///   UI → FrontendDialogueEventBus → DialogueUIAdapter → 本类 → GameEventDispatcher → 后端逻辑
    /// 设计原则: 不实现具体业务逻辑，仅转发和协调
    /// </summary>
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

        #region 适配器引用
        // ==========================================
        // Phase 2: 对话系统适配器 (后端 ↔ 前端)
        // ==========================================

        [SerializeField]
        [Tooltip("对话逻辑适配器 (后端→前端)")]
        private DialogueLogicAdapter dialogueLogicAdapter;

        [SerializeField]
        [Tooltip("对话UI适配器 (前端→后端)")]
        private DialogueUIAdapter dialogueUIAdapter;

        #endregion

        void Awake()
        {
            // 单例初始化
            if (_instance == null)
            {
                _instance = this;
            }

            // 初始化适配器
            InitializeAdapters();
        }

        void OnEnable()
        {
            // 原有的UI事件监听
            UIEventDispatcher.OnPlayerSubmitInput += HandlePlayerSubmitInput;

            // Phase 2: 监听后端对话事件
            GameEventDispatcher.OnDialogueGenerated += HandleDialogueGenerated;
            GameEventDispatcher.OnPhaseUnlockEvents += HandlePhaseUnlock;
        }

        void OnDisable()
        {
            // 取消订阅
            UIEventDispatcher.OnPlayerSubmitInput -= HandlePlayerSubmitInput;
            GameEventDispatcher.OnDialogueGenerated -= HandleDialogueGenerated;
            GameEventDispatcher.OnPhaseUnlockEvents -= HandlePhaseUnlock;
        }

        /// <summary>
        /// 初始化对话适配器
        /// </summary>
        private void InitializeAdapters()
        {
            // 如果未在Inspector中设置，尝试自动查找或创建
            if (dialogueLogicAdapter == null)
            {
                dialogueLogicAdapter = GetComponent<DialogueLogicAdapter>();
                if (dialogueLogicAdapter == null)
                {
                    Debug.LogWarning("[Game_UI_Coordinator] DialogueLogicAdapter not found on GameObject. Creating...");
                    dialogueLogicAdapter = gameObject.AddComponent<DialogueLogicAdapter>();
                }
            }

            if (dialogueUIAdapter == null)
            {
                dialogueUIAdapter = GetComponent<DialogueUIAdapter>();
                if (dialogueUIAdapter == null)
                {
                    Debug.LogWarning("[Game_UI_Coordinator] DialogueUIAdapter not found on GameObject. Creating...");
                    dialogueUIAdapter = gameObject.AddComponent<DialogueUIAdapter>();
                }
            }

            Debug.Log("[Game_UI_Coordinator] 对话适配器初始化完成");
        }

        #region 事件处理器 (后端逻辑 → 前端)

        /// <summary>
        /// 处理玩家输入 (原有逻辑)
        /// </summary>
        private void HandlePlayerSubmitInput(string input)
        {
            GameEventDispatcher.DispatchPlayerInputString(input);
        }

        /// <summary>
        /// 处理后端生成的对话 (Phase 2: 转发给适配器)
        /// </summary>
        private void HandleDialogueGenerated(List<string> dialogues)
        {
            if (dialogueLogicAdapter != null)
            {
                // 转发给后端→前端适配器进行数据转换
                dialogueLogicAdapter.ProcessDialogue(dialogues);
            }
            else
            {
                Debug.LogError("[Game_UI_Coordinator] DialogueLogicAdapter 为 null，无法处理对话");
            }

            // 备用: 仍然保持与旧UI系统的兼容性 (逐步迁移)
            // UIEventDispatcher.DispatchShowDialogues(dialogues);
        }

        /// <summary>
        /// 处理阶段解锁 (原有逻辑)
        /// </summary>
        private void HandlePhaseUnlock(string completedName, List<(string id, string name)> nextPhases)
        {
            // 转发给 UI 总线
            UIEventDispatcher.DispatchShowPhaseSelection(completedName, nextPhases);
        }

        #endregion
    }
}