using System.Collections.Generic;
using System.Text.RegularExpressions;
using Interrorgation.MidLayer;
using LogicEngine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Interrorgation.UI
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
        [SerializeField] private Transform _optionContainer;
        [SerializeField] private Button _submitButton;
        [SerializeField] private Button _closeButton;


        [Header("Prefabs")]
        [SerializeField] private TMP_Text _textPrefab;
        [SerializeField] private TMP_Dropdown _dropdownPrefab;
        [SerializeField] private TMP_InputField _inputPrefab;
        [SerializeField] private TMP_Text _outcomePrefab;
        [SerializeField] private NodeOptionUIScript _optionButtonPrefab;


        // 运行时状态
        private TemplateData _currentData;
        private Dictionary<int, object> _inputControls = new Dictionary<int, object>();
        private List<int> _slotOrder = new List<int>();
        private bool isUsed = false;

        private void Awake()
        {
            if (_submitButton)
            {
                _submitButton.onClick.AddListener(OnSubmitButtonClicked);
            }

            if (_closeButton)
            {
                _closeButton.onClick.AddListener(Hide);
            }
        }

        public void Init()
        {
            if (_currentData == null) getDataFromContext();
        }

        public void HideTemplate()
        {
            _outcomePrefab.text = "这是一个未被发现的模板"; 
        }

        public void DiscoverTemplate()
        {
            BuildQuestion(_currentData);
            _outcomePrefab.text = "";
        }

        public void OnTemplateUsed()
        {
            _outcomePrefab.text = "你已经发现了这个模板的所有答案！";
            // todo：这里之后有演出的话需要强制播放showtemplate的演出来显示，现在临时启用一下。因为可能showtemplate和used会一起发生。
            _panelRoot.SetActive(true);
            _submitButton.interactable = false;
            if (TemplateId.StartsWith("nodeTemplate"))
            {
                // 根据 templatedata 和唯一答案生成完整答案
                if (_currentData.Answers != null && _currentData.Answers.Count > 0)
                {
                    string raw = _currentData.RawText;
                    var answer = _currentData.Answers[0];
                    string fullText = raw;

                    var graph = LevelGraphContext.CurrentGraph;

                    for (int i = 0; i < answer.RequiredInputs.Count; i++)
                    {
                        string displayVal = answer.RequiredInputs[i];

                        // 如果该位不是下拉菜单（即输入框），则说明 RequiredInputs[i] 是 EntityID，需要转义
                        if (!_currentData.DropdownOptions.ContainsKey(i))
                        {
                            if (graph != null && graph.entityListData.Data.TryGetValue(displayVal, out var entity))
                            {
                                displayVal = entity.Name;
                            }
                        }

                        fullText = fullText.Replace("{" + i + "}", displayVal);
                    }

                    // 清理旧物体并显示完整答案
                    foreach (Transform child in _container)
                    {
                        Destroy(child.gameObject);
                    }

                    var textObj = Instantiate(_textPrefab, _container);
                    textObj.gameObject.SetActive(true);
                    textObj.text = fullText;
                }
            }
            isUsed = true;
        }

        public void Hide()
        {
            _panelRoot.SetActive(false);
        }

        public void HandleTemplateSettlement(GameEventDispatcher.TemplateSettlementContext context)
        {
            if (context.IsSuccess && !isUsed)
            {
                LevelGraphContext.CurrentGraph.nodeLookup.TryGetValue(context.TargetNodeId, out var node);
                _outcomePrefab.text = $"回答正确！结果: {node.Node.Basic.Description}";
            }
            if (!context.IsSuccess)
            {
                _outcomePrefab.text = $"回答错误，请再试一次。";
            }
        }

        private void BuildQuestion(TemplateData data)
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
            Debug.Log($"[TemplateUI] Submit: ID={TemplateId}, Inputs={string.Join(", ", userInputs)}");
            UIEventDispatcher.DispatchPlayerSubmitTemplateAnswer(TemplateId, userInputs);

        }
        private void getDataFromContext()
        {
            LevelGraphContext.CurrentGraph.allTemplates.TryGetValue(_templateId, out _currentData);
        }
    }
}
