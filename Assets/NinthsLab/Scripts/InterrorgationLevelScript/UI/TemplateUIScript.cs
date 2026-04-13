using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LogicEngine;
using LogicEngine.LevelLogic;
using Interrorgation.MidLayer;

namespace FrontendEngine
{
    /// <summary>
    /// 模板 UI 脚本 (正式版)
    /// 负责展示来自后端的推理模板，收集玩家输入并提交。
    /// </summary>
    public class TemplateUIScript : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private string _templateId;
        public string TemplateId => _templateId;

        [Header("UI References")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private Transform _container;
        [SerializeField] private Button _submitButton;
        [SerializeField] private Button _closeButton;

        [Header("Prefabs")]
        [SerializeField] private TMP_Text _textPrefab;
        [SerializeField] private TMP_Dropdown _dropdownPrefab;
        [SerializeField] private TMP_InputField _inputPrefab;
        [SerializeField] private TMP_Text _outcomePrefab;

        // 运行时状态
        private TemplateData _currentData;
        private string _currentTemplateId;
        private Dictionary<int, object> _inputControls = new Dictionary<int, object>();
        private List<int> _slotOrder = new List<int>();

        private void Awake()
        {
            if (_panelRoot) _panelRoot.SetActive(false);
            
            if (_submitButton)
            {
                _submitButton.onClick.AddListener(OnSubmitButtonClicked);
            }

            if (_closeButton)
            {
                _closeButton.onClick.AddListener(Hide);
            }
        }

        public void ShowTemplate(TemplateData runtimeData)
        {
            if (runtimeData == null) return;

            _currentTemplateId = runtimeData.Id;
            _currentData = runtimeData;

            BuildUI(_currentData);
            
            if (_panelRoot) _panelRoot.SetActive(true);
        }

        public void Hide()
        {
            if (_panelRoot) _panelRoot.SetActive(false);
        }
        
        public void HandleTemplateSettlement(GameEventDispatcher.TemplateSettlementContext context)
        {
            if (context.IsSuccess)
            {
                LevelGraphContext.CurrentGraph.nodeLookup.TryGetValue(context.TargetNodeId, out var node);
                _outcomePrefab.text = $"回答正确！结果: {node.Node.Basic.Description}";
            }
            else
            {
                _outcomePrefab.text = $"回答错误，请再试一次。";
            }
        }

        private void BuildUI(TemplateData data)
        {
            // 1. 清理旧物体
            foreach (Transform child in _container)
            {
                // 仅销毁实例化出的节点，不销毁可能存在的固定装饰
                Destroy(child.gameObject);
            }
            _inputControls.Clear();
            _slotOrder.Clear();

            string rawText = data.RawText;
            if (string.IsNullOrEmpty(rawText)) return;

            // 2. 正则解析占位符 {0}, {1} ...
            Regex regex = new Regex(@"\{(\d+)\}");
            MatchCollection matches = regex.Matches(rawText);
            int lastIndex = 0;

            foreach (Match match in matches)
            {
                // 添加占位符之前的文本
                if (match.Index > lastIndex)
                {
                    string textPart = rawText.Substring(lastIndex, match.Index - lastIndex);
                    CreateTextNode(textPart);
                }

                // 添加输入控件
                string idStr = match.Groups[1].Value;
                if (int.TryParse(idStr, out int slotId))
                {
                    CreateInputNode(slotId, data);
                    _slotOrder.Add(slotId);
                }
                lastIndex = match.Index + match.Length;
            }

            // 添加剩余文本
            if (lastIndex < rawText.Length)
            {
                CreateTextNode(rawText.Substring(lastIndex));
            }
        }

        private void CreateTextNode(string content)
        {
            var textObj = Instantiate(_textPrefab, _container);
            textObj.gameObject.SetActive(true);
            textObj.text = content;
        }

        private void CreateInputNode(int slotId, TemplateData data)
        {
            if (data.DropdownOptions.ContainsKey(slotId))
            {
                // 下拉框模式
                var dropdown = Instantiate(_dropdownPrefab, _container);
                dropdown.gameObject.SetActive(true);
                dropdown.ClearOptions();
                dropdown.AddOptions(data.DropdownOptions[slotId]);
                _inputControls[slotId] = dropdown;
            }
            else
            {
                // 输入框模式
                var inputField = Instantiate(_inputPrefab, _container);
                inputField.gameObject.SetActive(true);
                _inputControls[slotId] = inputField;
            }
        }

        private void OnSubmitButtonClicked()
        {
            if (_currentData == null) return;

            // 1. 收集所有输入框的内容
            List<string> userInputs = new List<string>();
            foreach (int slotId in _slotOrder)
            {
                if (_inputControls.TryGetValue(slotId, out object control))
                {
                    string val = "";
                    if (control is TMP_Dropdown dropdown)
                    {
                        if (dropdown.options.Count > 0)
                        {
                            int idx = dropdown.value;
                            // 优先获取原始 Key（对应 JSON 中的列表项）
                            if (_currentData.DropdownOptions.TryGetValue(slotId, out var keys) && idx < keys.Count)
                            {
                                val = keys[idx];
                            }
                        }
                    }
                    else if (control is TMP_InputField inputField)
                    {
                        val = inputField.text;
                    }
                    userInputs.Add(val);
                }
            }

            // 2. 通过中间层提交给后端逻辑
            Debug.Log($"[TemplateUI] Submit: ID={_currentTemplateId}, Inputs={string.Join(", ", userInputs)}");
            UIEventDispatcher.DispatchPlayerSubmitTemplateAnswer(_currentTemplateId, userInputs);
            
            // 提交后通常可以关闭面板，或等待后端反馈后再关闭
            // Hide(); 
        }
    }
}
