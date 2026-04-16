using System.Collections.Generic;
using Interrorgation.MidLayer;
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
    public class TemplateUIManager : MonoBehaviour, UISequence.IUIBacklogModule
    {
        [SerializeField] private GameObject deductionBoardPrefab;
        [SerializeField] private Button toggleButton;
        [Header("Settings")]
        [SerializeField] private RectTransform _uiRoot;
        [SerializeField] private bool _autoSearchOnAwake = true;

        private Dictionary<string, TemplateUIScript> _templateMap = new Dictionary<string, TemplateUIScript>();

        // [改进] 通用表现积压列表
        private List<System.Action> _backlog = new List<System.Action>();

        // [新增] 状态缓存与脏标记，用于优化 UI 刷新逻辑
        private Dictionary<string, RunTimeTemplateDataStatus> _statusCache = new Dictionary<string, RunTimeTemplateDataStatus>();
        private bool _isDirty = false;

        public bool IsVisualActive => deductionBoardPrefab.activeSelf;

        private void Awake()
        {
            if (_autoSearchOnAwake)
            {
                InitializeMap();
            }
            // 确保在Awake处订阅，即使面板没有激活也能在后台接收事件
            UIEventDispatcher.OnDiscoveredNewTemplates += HandleNewTemplates;
            UIEventDispatcher.OnTemplateStatusChanged += HandleTemplateStatusChanged;
            UIEventDispatcher.OnTemplateAnswerResult += HandleTemplateSettlement;
        }

        private void Start()
        {
            toggleButton.onClick.AddListener(OnDeducitonBoardToggleButtonClicked);
        }

        private void OnDestroy()
        {
            UIEventDispatcher.OnDiscoveredNewTemplates -= HandleNewTemplates;
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
            }

            Debug.Log($"[TemplateUIManager] Initialized with {_templateMap.Count} templates.");
        }

        private bool IsUIActive()
        {
            return deductionBoardPrefab.activeSelf;
        }

        /// <summary>
        /// 响应发现新模板的消息
        /// </summary>
        private void HandleNewTemplates(List<TemplateData> templates, string actionId)
        {
            if (templates == null || templates.Count == 0) return;

            // 使用通用逻辑：如果面板没开，则存入积压并立即回传 ID
            if (!IsVisualActive)
            {
                RecordBacklog(() => FlushCache());
                UIEventDispatcher.DispatchActionCompleted(actionId);
                return;
            }

            // 活跃状态下，直接通过全表同步来保证数据一致性
            FlushCache();
            // 注意：目前的 FlushCache 是瞬时的，如果以后有动画，需要在动画结束处调用 DispatchActionCompleted
            UIEventDispatcher.DispatchActionCompleted(actionId);
        }

        /// <summary>
        /// 响应具体的模板状态变更（例如从 Discovered 变为 Used）
        /// </summary>
        private void HandleTemplateStatusChanged(RuntimeTemplateData templateData, string actionId)
        {
            if (templateData == null)
            {
                UIEventDispatcher.DispatchActionCompleted(actionId);
                return;
            }

            if (!IsVisualActive)
            {
                RecordBacklog(() => {
                    ApplyTemplateStatus(templateData);
                    _statusCache[templateData.Id] = templateData.Status;
                });
                UIEventDispatcher.DispatchActionCompleted(actionId);
                return;
            }

            ApplyTemplateStatus(templateData);
            _statusCache[templateData.Id] = templateData.Status;
            UIEventDispatcher.DispatchActionCompleted(actionId);
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
                        uiScript.ShowTemplate(runtimeData.r_TemplateData);
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

            if (!IsVisualActive)
            {
                RecordBacklog(() => uiScript.HandleTemplateSettlement(context));
                UIEventDispatcher.DispatchActionCompleted(actionId);
                return;
            }

            uiScript.HandleTemplateSettlement(context);
            // 假设 HandleTemplateSettlement 内部有动画，UI 侧应在动画完成后调用某个反馈。
            // 暂时简写：立即反馈完成
            UIEventDispatcher.DispatchActionCompleted(actionId);
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
        public void ShowTemplateById(string id, TemplateData data)
        {
            if (_templateMap.TryGetValue(id, out var ui))
            {
                ui.ShowTemplate(data);
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
            if (deductionBoardPrefab.gameObject.activeSelf)
            {
                HideDeductionBoard();
            }
            else
            {
                ShowDeductionBoard();
                ProcessBacklog();
            }
        }

        #region IUIBacklogModule 实现
        public void RecordBacklog(System.Action action)
        {
            _backlog.Add(action);
            _isDirty = true;
        }

        public void ProcessBacklog()
        {
            if (_backlog.Count == 0) return;

            Debug.Log($"[TemplateUIManager] 开始播放积压演出，共 {_backlog.Count} 项");
            foreach (var action in _backlog)
            {
                action?.Invoke();
            }
            _backlog.Clear();
        }
        #endregion
    }
}
