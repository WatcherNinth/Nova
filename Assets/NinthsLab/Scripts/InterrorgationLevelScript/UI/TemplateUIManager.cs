using System.Collections.Generic;
using Interrorgation.MidLayer;
using Interrorgation.UI.UIState;
using LogicEngine;
using LogicEngine.LevelLogic;
using UnityEngine;
using UnityEngine.UI;

namespace Interrorgation.UI
{
    /// <summary>
    /// 模板 UI 管理器
    /// 负责管理场景中所有预设的 TemplateUIScript，根据后端发来的 ID 调度显示。
    /// </summary>
    public class TemplateUIManager : UIStateController<TemplateUIManager.DeductionBoardState>
    {
        public enum DeductionBoardState
        {
            Closed,
            Open,
            Minimized   // 未来扩展：最小化状态
        }

        [SerializeField] private GameObject deductionBoardPrefab;
        [SerializeField] private Button toggleButton;
        [Header("Settings")]
        [SerializeField] private RectTransform _uiRoot;
        [SerializeField] private bool _autoSearchOnAwake = true;

        private Dictionary<string, TemplateUIScript> _templateMap = new Dictionary<string, TemplateUIScript>();
        private Dictionary<string, NodeOptionUIScript> _optionMap = new Dictionary<string, NodeOptionUIScript>();
        private Dictionary<string, RunTimeTemplateDataStatus> _statusCache = new Dictionary<string, RunTimeTemplateDataStatus>();
        private bool _isDirty = false;

        protected override DeductionBoardState InitialState => DeductionBoardState.Closed;

        protected override Dictionary<DeductionBoardState, List<DeductionBoardState>> DefineTransitions()
        {
            return new Dictionary<DeductionBoardState, List<DeductionBoardState>>
            {
                { DeductionBoardState.Closed,   new List<DeductionBoardState> { DeductionBoardState.Open } },
                { DeductionBoardState.Open,     new List<DeductionBoardState> { DeductionBoardState.Closed, DeductionBoardState.Minimized } },
                { DeductionBoardState.Minimized, new List<DeductionBoardState> { DeductionBoardState.Open, DeductionBoardState.Closed } },
            };
        }

        protected override bool CheckIsVisualActive(DeductionBoardState state) => state == DeductionBoardState.Open;

        protected override void Awake()
        {
            base.Awake();
            if (_autoSearchOnAwake)
            {
                InitializeMap();
            }
        }

        private void Start()
        {
            toggleButton.onClick.AddListener(OnDeducitonBoardToggleButtonClicked);
        }

        protected override void SubscribeEvents()
        {
            UIEventDispatcher.OnDiscoveredNewTemplate += HandleNewTemplate;
            UIEventDispatcher.OnTemplateStatusChanged += HandleTemplateStatusChanged;
            UIEventDispatcher.OnTemplateAnswerResult += HandleTemplateSettlement;
        }

        protected override void UnsubscribeEvents()
        {
            UIEventDispatcher.OnDiscoveredNewTemplate -= HandleNewTemplate;
            UIEventDispatcher.OnTemplateStatusChanged -= HandleTemplateStatusChanged;
            UIEventDispatcher.OnTemplateAnswerResult -= HandleTemplateSettlement;
        }

        /// <summary>
        /// 扫描 root 下所有的 TemplateUIScript 并建立索引
        /// </summary>
        public void InitializeMap()
        {
            _templateMap.Clear();
            if (_uiRoot == null) _uiRoot = GetComponent<RectTransform>();

            // 深度搜索所有子物体中的 TemplateUIScript
            var allScripts = _uiRoot.GetComponentsInChildren<TemplateUIScript>(true);
            foreach (var script in allScripts)
            {
                if (string.IsNullOrEmpty(script.TemplateId))
                {
                    Debug.LogWarning($"[TemplateUIManager] Found TemplateUIScript on {script.name} with empty ID.");
                    continue;
                }

                if (_templateMap.ContainsKey(script.TemplateId))
                {
                    Debug.LogError($"[TemplateUIManager] Duplicate Template ID found: {script.TemplateId} on {script.name}. Overwriting previous.");
                }
                _templateMap[script.TemplateId] = script;
                script.Init();
            }

            // 深度搜索所有子物体中的 OptionButton
            var allOptions = _uiRoot.GetComponentsInChildren<NodeOptionUIScript>(true);
            foreach (var option in allOptions)
            {
                if (string.IsNullOrEmpty(option.nodeId))
                {
                    Debug.LogWarning($"[TemplateUIManager] Found OptionButton on {option.name} with empty ID.");
                    continue;
                }

                if (_optionMap.ContainsKey(option.nodeId))
                {
                    Debug.LogError($"[TemplateUIManager] Duplicate Option ID found: {option.nodeId} on {option.name}. Overwriting previous.");
                }
                _optionMap[option.nodeId] = option;
            }

            Debug.Log($"[TemplateUIManager] Initialized with {_templateMap.Count} templates and {_optionMap.Count} options.");
        }

        private void HandleNewTemplate(TemplateData template, string actionId)
        {
            if (template == null)
            {
                UIEventDispatcher.DispatchActionCompleted(actionId);
                return;
            }
            DispatchOrBacklog(() => FlushCache(), actionId);
        }

        private void HandleTemplateStatusChanged(RuntimeTemplateData templateData, string actionId)
        {
            if (templateData == null)
            {
                UIEventDispatcher.DispatchActionCompleted(actionId);
                return;
            }
            DispatchOrBacklog(() =>
            {
                ApplyTemplateStatus(templateData);
                _statusCache[templateData.Id] = templateData.Status;
            }, actionId);
        }

        /// <summary>
        /// 将逻辑层最新的全量状态应用到 UI
        /// </summary>
        public void FlushCache()
        {
            var allStatus = GameEventDispatcher.GetAllTemplateStatus();
            if (allStatus == null)
            {
                Debug.LogWarning("[TemplateUIManager] FlushCache: GetAllTemplateStatus returned null. Logic may not be initialized.");
                return;
            }

            foreach (var pair in allStatus)
            {
                string id = pair.Key;
                RuntimeTemplateData runtimeData = pair.Value;

                // 仅在状态发生变化时调用 UI 更新，减少性能消耗
                if (!_statusCache.TryGetValue(id, out var cachedStatus) || cachedStatus != runtimeData.Status)
                {
                    ApplyTemplateStatus(runtimeData);
                    _statusCache[id] = runtimeData.Status;
                }
            }

            _isDirty = false;
            Debug.Log("[TemplateUIManager] UI 状态已同步至最新。");
        }

        /// <summary>
        /// 执行具体的 UI 显隐和展示逻辑
        /// </summary>
        private void ApplyTemplateStatus(RuntimeTemplateData runtimeData)
        {
            if (_templateMap.TryGetValue(runtimeData.Id, out var uiScript))
            {
                switch (runtimeData.Status)
                {
                    case RunTimeTemplateDataStatus.Hidden:
                        uiScript.gameObject.SetActive(false);
                        break;
                    case RunTimeTemplateDataStatus.Discovered:
                        uiScript.gameObject.SetActive(true);
                        uiScript.ShowTemplate();
                        break;
                    case RunTimeTemplateDataStatus.Used:
                        uiScript.OnTemplateUsed();
                        break;
                }
            }
        }

        private void HandleTemplateSettlement(GameEventDispatcher.TemplateSettlementContext context, string actionId)
        {
            if (!_templateMap.TryGetValue(context.TemplateId, out var uiScript))
            {
                Debug.LogError($"[TemplateUIManager] Template Settlement received for ID {context.TemplateId}, but no matching UI element was found.");
                UIEventDispatcher.DispatchActionCompleted(actionId);
                return;
            }
            DispatchOrBacklog(() => uiScript.HandleTemplateSettlement(context), actionId);
        }

        /// <summary>
        /// 隐藏所有模板 UI
        /// </summary>
        public void HideAll()
        {
            foreach (var ui in _templateMap.Values)
            {
                ui.Hide();
            }
        }

        /// <summary>
        /// 根据 ID 查找并显示特定模板 (手动触发用)
        /// </summary>
        public void ShowTemplateById(string id)
        {
            if (_templateMap.TryGetValue(id, out var ui))
            {
                ui.ShowTemplate();
            }
        }

        public void ShowDeductionBoard()
        {
            // 显示推理板 UI
            deductionBoardPrefab.gameObject.SetActive(true);

            if (_isDirty)
            {
                FlushCache();
            }
        }

        public void HideDeductionBoard()
        {
            // 隐藏推理板 UI
            deductionBoardPrefab.gameObject.SetActive(false);
            // 这里可以根据实际情况销毁推理板预制件，或者隐藏已经存在的推理板 UI 对象
        }

        public void OnDeducitonBoardToggleButtonClicked()
        {
            if (StateMachine.CurrentState == DeductionBoardState.Open)
                StateMachine.TryTransitionTo(DeductionBoardState.Closed);
            else
                StateMachine.TryTransitionTo(DeductionBoardState.Open);
        }

        public override void OnStateEnter(DeductionBoardState state)
        {
            switch (state)
            {
                case DeductionBoardState.Closed:
                    deductionBoardPrefab.SetActive(false);
                    break;
                case DeductionBoardState.Open:
                    deductionBoardPrefab.SetActive(true);
                    if (_isDirty) FlushCache();
                    break;
                case DeductionBoardState.Minimized:
                    // 未来：缩小面板但不完全关闭
                    deductionBoardPrefab.SetActive(false);
                    break;
            }
        }
    }
}