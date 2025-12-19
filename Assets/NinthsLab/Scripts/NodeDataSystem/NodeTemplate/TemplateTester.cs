using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using LogicEngine.Templates; 
using LogicEngine.LevelLogic; // [新增] 引用 Runtime 数据
using Interrorgation.MidLayer; // [新增] 引用 Dispatcher
using Newtonsoft.Json.Linq;  
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LogicEngine.Tests
{
    public class TemplateTester : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text _textPrefab;         
        [SerializeField] private TMP_Dropdown _dropdownPrefab; 
        [SerializeField] private TMP_InputField _inputPrefab;  
        [SerializeField] private Transform _container;         
        [SerializeField] private Button _checkButton;
        [SerializeField] private TMP_Text _resultText;

        [Header("Debug / Test Mode")]
        [Tooltip("如果勾选，将忽略 Inspector 的 JSON，仅等待后端事件")]
        [SerializeField] private bool _listenToGameEvents = true;

        [Header("JSON Input (Test Only)")]
        [TextArea(10, 20)] 
        [SerializeField] private string _jsonContent = @"{
    ""text"": ""这是一个测试文本，请选择{0}，然后输入{1}。"",
    ""dropdown_blank_1"": [
        ""苹果"",
        ""香蕉"",
        ""橘子""
    ],
    ""answer"": {
        ""this"": [
            ""香蕉"",
            ""好吃的""
        ],
        ""other_argument_id"": [
            ""苹果"",
            ""不好吃""
        ]
    }
}"; // (保留默认值以便测试)

        // 运行时状态
        private TemplateData _runtimeData;
        private string _currentTemplateId; // [新增] 记录当前显示的模板 ID
        private Dictionary<int, object> _inputControls = new Dictionary<int, object>();
        private List<int> _slotOrder = new List<int>();

        // =========================================================
        // [新增] 事件监听与生命周期
        // =========================================================
        private void OnEnable()
        {
            if (_listenToGameEvents)
            {
                // 监听后端发来的新模板
                UIEventDispatcher.OnDiscoveredNewTemplates += HandleNewTemplates;
            }
        }

        private void OnDisable()
        {
            if (_listenToGameEvents)
            {
                UIEventDispatcher.OnDiscoveredNewTemplates -= HandleNewTemplates;
            }
        }

        private void Start()
        {
            // 初始化容器 (如果没挂 Layout)
            if (_container.GetComponent<SimpleFlowLayout>() == null)
            {
                var layout = _container.gameObject.AddComponent<SimpleFlowLayout>();
                layout.padding = new RectOffset(20, 20, 20, 20);
                layout.SpacingX = 5;
                layout.SpacingY = 10;
            }

            // 绑定按钮事件
            if (_checkButton != null)
            {
                _checkButton.onClick.RemoveAllListeners();
                _checkButton.onClick.AddListener(OnSubmitButtonClicked);
            }

            // 如果不是监听模式，或者还没收到数据，尝试加载测试 JSON
            if (!_listenToGameEvents && !string.IsNullOrEmpty(_jsonContent))
            {
                LoadTestJson();
            }
        }

        // =========================================================
        // [新增] 响应后端事件
        // =========================================================
        private void HandleNewTemplates(List<RuntimeTemplateData> templates)
        {
            if (templates == null || templates.Count == 0) return;

            // 这里简单处理：如果有新模板，直接显示列表中的第一个（覆盖旧的）
            // 如果需要显示列表，这里的逻辑需要改成实例化多个 Panel，但在当前脚本结构下，我们复用现有的 Panel
            var target = templates[0];
            
            Debug.Log($"[TemplateTester] 收到后端模板: {target.Id}");
            
            // 更新 ID 和数据
            _currentTemplateId = target.Id;
            _runtimeData = target.r_TemplateData;

            // 重建 UI
            BuildUI(_runtimeData);
            
            // 清空结果文本
            if (_resultText) _resultText.text = "";
        }

        // =========================================================
        // UI 构建逻辑 (保留原逻辑)
        // =========================================================
        private void LoadTestJson()
        {
            if (TryParseJson(out _runtimeData))
            {
                _currentTemplateId = "Test_Template_Local"; // 测试模式下的假 ID
                BuildUI(_runtimeData);
            }
        }

        private bool TryParseJson(out TemplateData data)
        {
            data = null;
            if (string.IsNullOrWhiteSpace(_jsonContent)) return false;
            try
            {
                JObject jsonObj = JObject.Parse(_jsonContent);
                data = TemplateParser.Parse(jsonObj, "test_owner");
                return data != null;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
        }

        private void BuildUI(TemplateData data)
        {
            // 1. 清理旧物体
            foreach (Transform child in _container) Destroy(child.gameObject);
            _inputControls.Clear();
            _slotOrder.Clear();

            string rawText = data.RawText;
            if (string.IsNullOrEmpty(rawText)) return;

            // 2. 正则解析占位符
            Regex regex = new Regex(@"\{(\d+)\}");
            MatchCollection matches = regex.Matches(rawText);
            int lastIndex = 0;

            foreach (Match match in matches)
            {
                if (match.Index > lastIndex)
                {
                    string textPart = rawText.Substring(lastIndex, match.Index - lastIndex);
                    CreateTextNode(textPart);
                }

                string idStr = match.Groups[1].Value;
                if (int.TryParse(idStr, out int slotId))
                {
                    CreateInputNode(slotId, data);
                    _slotOrder.Add(slotId);
                }
                lastIndex = match.Index + match.Length;
            }

            if (lastIndex < rawText.Length)
            {
                CreateTextNode(rawText.Substring(lastIndex));
            }
        }

        private void CreateTextNode(string content)
        {
            // TODO: LocaleHelper.GetText(content)
            var textObj = Instantiate(_textPrefab, _container);
            textObj.gameObject.SetActive(true);
            textObj.text = content; 
        }

        private void CreateInputNode(int slotId, TemplateData data)
        {
            if (data.DropdownOptions.ContainsKey(slotId))
            {
                var dropdown = Instantiate(_dropdownPrefab, _container);
                dropdown.gameObject.SetActive(true);
                dropdown.ClearOptions();
                
                // 填充选项
                // TODO: LocaleHelper 处理
                dropdown.AddOptions(data.DropdownOptions[slotId]);
                
                _inputControls[slotId] = dropdown;
            }
            else
            {
                var inputField = Instantiate(_inputPrefab, _container);
                inputField.gameObject.SetActive(true);
                _inputControls[slotId] = inputField;
            }
        }

        // =========================================================
        // [修改] 提交逻辑：区分测试模式和游戏模式
        // =========================================================
        private void OnSubmitButtonClicked()
        {
            if (_runtimeData == null) return;

            // 1. 收集输入
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
                            // 尝试获取原始 Key
                            if (_runtimeData.DropdownOptions.TryGetValue(slotId, out var keys) && idx < keys.Count)
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

            // 2. 分支处理
            if (_listenToGameEvents && Application.isPlaying)
            {
                // [游戏模式] 发送给后端
                Debug.Log($"[TemplateTester] 发送答案: TemplateID={_currentTemplateId}, Inputs={string.Join(",", userInputs)}");
                
                // 调用 UIEventDispatcher (请确保该方法已定义)
                UIEventDispatcher.DispatchPlayerSubmitTemplateAnswer(_currentTemplateId, userInputs);
                
                if (_resultText) _resultText.text = "已提交...等待结果";
            }
            else
            {
                // [测试模式] 本地验证
                string resultId = TemplateLogic.CheckResult(_runtimeData, userInputs);
                if (!string.IsNullOrEmpty(resultId))
                {
                    if (_resultText) _resultText.text = $"<color=green>Correct!</color> Target: {resultId}";
                }
                else
                {
                    if (_resultText) _resultText.text = $"<color=red>Wrong Answer</color>";
                }
            }
        }
    }
}