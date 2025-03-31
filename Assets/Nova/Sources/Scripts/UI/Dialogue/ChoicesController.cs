using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class ChoicesController : MonoBehaviour
    {
        [SerializeField] private ChoiceButtonController choiceButtonPrefab;
        [SerializeField] private GameObject backPanel;
        [SerializeField] private string imageFolder;

        private GameState gameState;
        private Button[] buttons;
        public int activeChoiceCount { get; private set; }
        private bool _buttonsEnabled = true;

        // 新增：存储当前的选项数据
        private IReadOnlyList<ChoiceOccursData.Choice> currentChoices;

        public bool buttonsEnabled
        {
            get => _buttonsEnabled;
            set
            {
                if (buttons == null || value == _buttonsEnabled)
                {
                    return;
                }
                foreach (var button in buttons)
                {
                    button.enabled = value;
                }
                _buttonsEnabled = value;
            }
        }

        private void Awake()
        {
            RemoveAllChoices();
            gameState = Utils.FindNovaController().GameState;
            gameState.choiceOccurs.AddListener(OnChoiceOccurs);
            gameState.restoreStarts.AddListener(OnRestoreStarts);
        }

        private void OnDestroy()
        {
            gameState.choiceOccurs.RemoveListener(OnChoiceOccurs);
            gameState.restoreStarts.RemoveListener(OnRestoreStarts);
        }

        private void OnChoiceOccurs(ChoiceOccursData data)
        {
            RaiseChoices(data.choices);
        }

        public void RaiseChoices(IReadOnlyList<ChoiceOccursData.Choice> choices)
        {
            if (choices.Count == 0)
            {
                throw new ArgumentException("Nova: No active selection.");
            }

            // 存储当前的选项数据，方便在按钮点击时获取文本
            currentChoices = choices;

            if (backPanel != null)
            {
                backPanel.SetActive(true);
            }

            for (var i = 0; i < choices.Count; i++)
            {
                var choice = choices[i];
                var index = i;
                var button = Instantiate(choiceButtonPrefab, transform);
                // 防止在初始化之前显示按钮
                button.gameObject.SetActive(false);
                button.Init(choice.texts, choice.imageInfo, imageFolder, () => Select(index), choice.interactable);
                button.gameObject.SetActive(true);
            }

            buttons = GetComponentsInChildren<Button>();
            activeChoiceCount = choices.Count;
        }

        public void Select(int index)
        {
            if (currentChoices != null && index < currentChoices.Count)
            {
                // 获取当前选项的文本，假设使用默认语言 I18n.DefaultLocale
                string selectedText = currentChoices[index].texts[I18n.DefaultLocale];
                Debug.Log("Selected Choice Text: " + selectedText);

                // 获取 LogController 实例，将分支选择记录到日志中
                LogController logController = FindObjectOfType<LogController>();
                if (logController != null)
                {
                    logController.AddChoiceEntry(selectedText);
                }
            }

            RemoveAllChoices();
            gameState.SignalFence(index);
        }

        private void OnRestoreStarts(bool isInitial)
        {
            RemoveAllChoices();
        }

        private void RemoveAllChoices()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
            activeChoiceCount = 0;
            if (backPanel != null)
            {
                backPanel.SetActive(false);
            }
            buttons = null;
            currentChoices = null;
        }
    }
}
