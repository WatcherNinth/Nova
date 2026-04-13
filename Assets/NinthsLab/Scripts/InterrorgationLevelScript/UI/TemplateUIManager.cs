using System.Collections.Generic;
using UnityEngine;
using LogicEngine;
using Interrorgation.MidLayer;

namespace FrontendEngine
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

        private void Awake()
        {
            if (_autoSearchOnAwake)
            {
                InitializeMap();
            }
        }

        private void OnEnable()
        {
            UIEventDispatcher.OnDiscoveredNewTemplates += HandleNewTemplates;
        }

        private void OnDisable()
        {
            UIEventDispatcher.OnDiscoveredNewTemplates -= HandleNewTemplates;
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

            // 目前逻辑：显示列表中的第一个匹配 ID 的模板
            foreach (var runtimeData in templates)
            {
                if (_templateMap.TryGetValue(runtimeData.Id, out var uiScript))
                {
                    // 确保物体先激活，以便其内部逻辑（如 Awake/OnEnable）或 UI 布局组件能正常工作
                    uiScript.gameObject.SetActive(true); 
                    uiScript.ShowTemplate(runtimeData);
                    break;
                }
                else
                {
                    Debug.LogError($"[TemplateUIManager] Received template data with ID {runtimeData.Id}, but no matching UI element was found under root.");
                }
            }
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
