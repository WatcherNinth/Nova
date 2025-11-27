using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using LogicEngine.Templates; // 引用逻辑层命名空间
using Newtonsoft.Json.Linq;  // 引用Newtonsoft
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LogicEngine.Tests
{
    /// <summary>
    /// Template功能的测试UI控制器。
    /// 现在的版本支持直接输入JSON字符串来模拟真实的数据加载流程。
    /// </summary>
    public class TemplateTester : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text _textPrefab;         // 必须挂载ContentSizeFitter(Horizontal=Preferred)
        [SerializeField] private TMP_Dropdown _dropdownPrefab; // 固定宽度
        [SerializeField] private TMP_InputField _inputPrefab;  // 固定宽度
        [SerializeField] private Transform _container;         // 挂载SimpleFlowLayout
        [SerializeField] private Button _checkButton;
        [SerializeField] private TMP_Text _resultText;

        [Header("Simulation Settings")]
        [Tooltip("模拟当前论点的ID，用于解析 'this' 关键字")]
        [SerializeField] private string _simulatedOwnerId = "argument_001";
        
        [Header("JSON Input")]
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
}";

        // 运行时状态
        private TemplateData _runtimeData;
        private Dictionary<int, object> _inputControls = new Dictionary<int, object>();
        private List<int> _slotOrder = new List<int>();

        private void Start()
        {
            // 1. 初始化容器布局
            SetupContainer();

            // 2. 解析JSON
            if (!TryParseJson(out _runtimeData))
            {
                Debug.LogError("JSON解析失败，无法构建UI。请检查JSON格式。");
                return;
            }

            // 3. 构建UI
            BuildUI(_runtimeData);

            // 4. 绑定按钮事件
            if (_checkButton != null)
            {
                _checkButton.onClick.RemoveAllListeners();
                _checkButton.onClick.AddListener(OnCheckButtonClicked);
            }
        }

        /// <summary>
        /// 尝试解析Inspector里填写的JSON字符串
        /// </summary>
        private bool TryParseJson(out TemplateData data)
        {
            data = null;
            if (string.IsNullOrWhiteSpace(_jsonContent)) return false;

            try
            {
                // 将字符串转为 JObject
                JObject jsonObj = JObject.Parse(_jsonContent);
                
                // 调用之前的 Parser 逻辑
                // 注意：Parser接收的是JToken，且逻辑上对应 ""template"": { ... } 内部的花括号内容
                data = TemplateParser.Parse(jsonObj, _simulatedOwnerId);
                
                return data != null;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
        }

        private void SetupContainer()
        {
            if (_container.GetComponent<SimpleFlowLayout>() == null)
            {
                var layout = _container.gameObject.AddComponent<SimpleFlowLayout>();
                layout.padding = new RectOffset(20, 20, 20, 20);
                layout.SpacingX = 5;
                layout.SpacingY = 10;
            }
        }

        private void BuildUI(TemplateData data)
        {
            // 清理旧物体
            foreach (Transform child in _container)
            {
                Destroy(child.gameObject);
            }
            _inputControls.Clear();
            _slotOrder.Clear();

            string rawText = data.RawText;
            if (string.IsNullOrEmpty(rawText))
            {
                CreateTextNode("Template data is empty or invalid.");
                return;
            }

            // 正则匹配 {0}, {1} 等占位符
            Regex regex = new Regex(@"\{(\d+)\}");
            MatchCollection matches = regex.Matches(rawText);

            int lastIndex = 0;

            foreach (Match match in matches)
            {
                // 1. 处理占位符前的纯文本
                if (match.Index > lastIndex)
                {
                    string textPart = rawText.Substring(lastIndex, match.Index - lastIndex);
                    CreateTextNode(textPart);
                }

                // 2. 处理占位符
                string idStr = match.Groups[1].Value;
                if (int.TryParse(idStr, out int slotId))
                {
                    CreateInputNode(slotId, data);
                    _slotOrder.Add(slotId);
                }

                lastIndex = match.Index + match.Length;
            }

            // 3. 处理末尾剩余的文本
            if (lastIndex < rawText.Length)
            {
                string tailPart = rawText.Substring(lastIndex);
                CreateTextNode(tailPart);
            }
        }

        private void CreateTextNode(string content)
        {
            // 本地化占位（测试时直接用原文本）
            string localizedText = LocaleHelper.GetText(content);

            var textObj = Instantiate(_textPrefab, _container);
            textObj.gameObject.SetActive(true);
            textObj.text = localizedText;
            textObj.name = $"Text_{Math.Abs(content.GetHashCode())}";
        }

        private void CreateInputNode(int slotId, TemplateData data)
        {
            // 检查是否有 Dropdown 配置
            if (data.DropdownOptions.ContainsKey(slotId))
            {
                // 创建 Dropdown
                var dropdown = Instantiate(_dropdownPrefab, _container);
                dropdown.gameObject.SetActive(true);
                dropdown.name = $"Dropdown_{slotId}";
                
                var options = data.DropdownOptions[slotId];
                dropdown.ClearOptions();
                
                // 本地化选项文本
                List<string> localizedOptions = new List<string>();
                foreach (var opt in options)
                {
                    localizedOptions.Add(LocaleHelper.GetText(opt));
                }
                dropdown.AddOptions(localizedOptions);

                _inputControls[slotId] = dropdown;
            }
            else
            {
                // 创建普通 InputField
                var inputField = Instantiate(_inputPrefab, _container);
                inputField.gameObject.SetActive(true);
                inputField.name = $"Input_{slotId}";
                _inputControls[slotId] = inputField;
            }
        }

        private void OnCheckButtonClicked()
        {
            if (_runtimeData == null) return;

            List<string> userInputs = new List<string>();

            // 按照文本中出现的顺序收集答案
            foreach (int slotId in _slotOrder)
            {
                if (_inputControls.TryGetValue(slotId, out object control))
                {
                    string val = "";
                    if (control is TMP_Dropdown dropdown)
                    {
                        // Dropdown 需要获取对应的 Key，而不是显示的 Text (虽然目前LocaleHelper直接返回Key)
                        // 逻辑层校验的是原始Key
                        if (dropdown.options.Count > 0)
                        {
                            // 获取当前选中的索引
                            int selectedIndex = dropdown.value;
                            // 从数据源获取原始Key
                            if (_runtimeData.DropdownOptions.TryGetValue(slotId, out var keys) && selectedIndex < keys.Count)
                            {
                                val = keys[selectedIndex];
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

            // 调用逻辑层验证
            string resultId = TemplateLogic.CheckResult(_runtimeData, userInputs);

            // 显示结果
            if (!string.IsNullOrEmpty(resultId))
            {
                string displayId = resultId == _simulatedOwnerId ? "THIS (Self)" : resultId;
                _resultText.text = $"<color=green>SUCCESS!</color>\nTriggered ID: <b>{displayId}</b>";
            }
            else
            {
                _resultText.text = $"<color=red>FAILED</color>\nNo answer matched.";
            }
        }
        
        // 用于在Inspector中手动触发重新构建（可选）
        [ContextMenu("Refresh UI")]
        public void Refresh()
        {
            if (Application.isPlaying)
            {
                Start();
            }
        }
    }
}