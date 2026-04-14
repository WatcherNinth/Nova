using System.Collections.Generic;
using UnityEngine;
using LogicEngine;
using Interrorgation.MidLayer;

namespace Interrorgation.UI
{
    /// <summary>
    /// 模板 UI 管理器
    /// 负责管理场景中所有预设的 TemplateUIScript，根据后端发来的 ID 调度显示。
    /// </summary>
    public class TemplateUIManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private RectTransform _uiRoot;
        [SerializeField] private bool _autoSearchOnAwake = true;

        private Dictionary<string, TemplateUIScript> _templateMap = new Dictionary<string, TemplateUIScript>();
        private List<TemplateData> _templateCache = new List<TemplateData>();

        private void Awake()
        {
            if (_autoSearchOnAwake)
            {
                InitializeMap();
            }
            // 确保在Awake处订阅，即使面板没有激活也能在后台接收事件
            UIEventDispatcher.OnDiscoveredNewTemplates += HandleNewTemplates;
            UIEventDispatcher.OnTemplateAnswerResult += HandleTemplateSettlement;
        }

        private void OnEnable()
        {
            
        }

        private void OnDisable()
        {
            // 此处不做注销，使得面板隐藏时仍能缓存事件
        }

        private void OnDestroy()
        {
            UIEventDispatcher.OnDiscoveredNewTemplates -= HandleNewTemplates;
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

        /// <summary>
        /// 响应后端发来的新模板数据
        /// </summary>
        private void HandleNewTemplates(List<TemplateData> templates)
        {
            if (templates == null || templates.Count == 0) return;

            // 如果当前 UI 不处于激活状态，将发现的模板加入缓存
            bool isParentActive = (_uiRoot != null) ? _uiRoot.gameObject.activeInHierarchy : gameObject.activeInHierarchy;
            if (!isParentActive)
            {
                foreach (var t in templates)
                {
                    if (!_templateCache.Exists(x => x.Id == t.Id))
                    {
                        _templateCache.Add(t);
                    }
                }
                return;
            }

            ProcessTemplates(templates);
        }

        public void FlushCache()
        {
            if (_templateCache.Count > 0)
            {
                ProcessTemplates(_templateCache);
                _templateCache.Clear();
            }
        }

        private void ProcessTemplates(List<TemplateData> templates)
        {
            // 一次性更新所有相关状态
            foreach (var runtimeData in templates)
            {
                if (_templateMap.TryGetValue(runtimeData.Id, out var uiScript))
                {
                    // 确保物体先激活，以便其内部逻辑（如 Awake/OnEnable）或 UI 布局组件能正常工作
                    uiScript.gameObject.SetActive(true);
                    uiScript.ShowTemplate(runtimeData);
                }
                else
                {
                    Debug.LogError($"[TemplateUIManager] Received template data with ID {runtimeData.Id}, but no matching UI element was found under root.");
                }
            }
        }
        
        private void HandleTemplateSettlement(GameEventDispatcher.TemplateSettlementContext context)
        {
            if (!_templateMap.TryGetValue(context.TemplateId, out var uiScript))
            {
                Debug.LogError($"[TemplateUIManager] Template Settlement received for ID {context.TemplateId}, but no matching UI element was found.");
                return;
            }
            uiScript.HandleTemplateSettlement(context);
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
    }
}
