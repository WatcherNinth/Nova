using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Newtonsoft.Json;

namespace Nova
{
    // LogEntry 类扩展：新增 branchChoice 字段用于记录分支选择日志
    // public class LogEntry
    // {
    //     public float height;
    //     public float prefixHeight;
    //     public readonly long nodeOffset;
    //     public readonly long checkpointOffset;
    //     public readonly ReachedDialogueData dialogueData;
    //     public readonly DialogueDisplayData displayData;
    //     public string branchChoice;  // 如果不为 null，则表示这是一个分支选择记录

    //     public LogEntry(float height, float prefixHeight, long nodeOffset, long checkpointOffset,
    //         ReachedDialogueData dialogueData, DialogueDisplayData displayData)
    //     {
    //         this.height = height;
    //         this.prefixHeight = prefixHeight;
    //         this.nodeOffset = nodeOffset;
    //         this.checkpointOffset = checkpointOffset;
    //         this.dialogueData = dialogueData;
    //         this.displayData = displayData;
    //         this.branchChoice = null;
    //     }
    // }

    public class LogController : ViewControllerBase, IRestorable, LoopScrollPrefabSource, LoopScrollDataSource, LoopScrollSizeHelper
    {
        [Serializable]
        private class LogEntryRestoreData
        {
            public readonly DialogueDisplayData displayData;
            public int index;

            public LogEntryRestoreData(DialogueDisplayData displayData, int index)
            {
                this.displayData = displayData;
                this.index = index;
            }
        }

        [SerializeField] private int maxLogEntryNum = 1000;
        [SerializeField] private LogEntryController logEntryPrefab;
        [SerializeField] private Button closeButton;

        private const string LogViewFirstShownKey = ConfigManager.FirstShownKeyPrefix + "LogView";
        private const string FontSizeKey = "FontSize";

        private GameState gameState;
        private CheckpointManager checkpointManager;
        private ConfigManager configManager;

        private LoopVerticalScrollRectWithSwitch scrollRect;
        private VerticalLayoutGroup scrollLayout;

        private LogEntryController logEntryForTest;
        private TextProxy contentForTest;
        private float contentDefaultWidth;
        private bool needRefreshEntryHeight;

        private readonly List<LogEntry> logEntries = new List<LogEntry>();
        private readonly List<LogEntryRestoreData> logEntriesRestoreData = new List<LogEntryRestoreData>();

        protected override void Awake()
        {
            base.Awake();

            var controller = Utils.FindNovaController();
            gameState = controller.GameState;
            checkpointManager = controller.CheckpointManager;
            configManager = controller.ConfigManager;

            // 初始化 UI 组件（如果存在）
            scrollRect = myPanel.GetComponentInChildren<LoopVerticalScrollRectWithSwitch>();
            if (scrollRect != null)
            {
                scrollRect.prefabSource = this;
                scrollRect.dataSource = this;
                scrollRect.sizeHelper = this;
                scrollLayout = scrollRect.GetComponentInChildren<VerticalLayoutGroup>();
            }

            myPanel.GetComponent<Button>().onClick.AddListener(Hide);
            closeButton.onClick.AddListener(Hide);

            gameState.gameStarted.AddListener(Clear);
            gameState.dialogueChanged.AddListener(OnDialogueChanged);
            gameState.AddRestorable(this);

            I18n.LocaleChanged.AddListener(OnFontSizeChanged);
            configManager.AddValueChangeListener(FontSizeKey, OnFontSizeChanged);
        }

        protected override void ForceRebuildLayoutAndResetTransitionTarget()
        {
            if (scrollRect == null) return;

            if (logEntryForTest == null)
            {
                logEntryForTest = Instantiate(logEntryPrefab, scrollRect.content);
            }

            logEntryForTest.transform.SetParent(scrollRect.content, false);
            logEntryForTest.gameObject.SetActive(true);

            base.ForceRebuildLayoutAndResetTransitionTarget();

            contentForTest = logEntryForTest.transform.Find("Text").GetComponent<TextProxy>();
            contentDefaultWidth = contentForTest.GetComponent<RectTransform>().rect.width;
            logEntryForTest.gameObject.SetActive(false);
            logEntryForTest.transform.SetParent(scrollRect.transform, false);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            myPanel.GetComponent<Button>().onClick.RemoveListener(Hide);
            closeButton.onClick.RemoveListener(Hide);

            gameState.gameStarted.RemoveListener(Clear);
            gameState.dialogueChanged.RemoveListener(OnDialogueChanged);
            gameState.RemoveRestorable(this);

            I18n.LocaleChanged.RemoveListener(OnFontSizeChanged);
            configManager.RemoveValueChangeListener(FontSizeKey, OnFontSizeChanged);
        }

        private void OnDialogueChanged(DialogueChangedData data)
        {
            if (data.dialogueData.needInterpolate)
            {
                logEntriesRestoreData.Add(new LogEntryRestoreData(data.displayData, logEntries.Count));
            }

            if (!gameState.isUpgrading)
            {
                AddEntry(data.nodeRecord, data.checkpointOffset, data.dialogueData, data.displayData);
            }
        }

        private void UpdateContentForTest()
        {
            if (scrollRect != null && scrollRect.content.childCount > 0)
            {
                var newContentForTest = scrollRect.content.GetChild(0).GetComponent<TextProxy>();
                if (newContentForTest != null)
                {
                    contentForTest = newContentForTest;
                }
            }
            if (contentForTest != null)
            {
                contentForTest.GetComponent<FontSizeReader>().UpdateValue();
                contentForTest.ForceRefresh();
            }
        }

        // 添加对话日志记录（对话记录 displayData 不为空）
        private void AddEntry(NodeRecord nodeRecord, long checkpointOffset, ReachedDialogueData dialogueData,
            DialogueDisplayData displayData)
        {
            var text = displayData.FormatNameDialogue();
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            UpdateContentForTest();
            float height = (contentForTest != null) ? contentForTest.GetPreferredHeight(text, contentDefaultWidth) : 0f;
            int cnt = logEntries.Count;
            float prefixHeight = height + (cnt > 0 ? logEntries[cnt - 1].prefixHeight : 0);
            logEntries.Add(new LogEntry(height, prefixHeight, nodeRecord.offset, checkpointOffset, dialogueData, displayData));

            if (scrollRect != null && !RestrainLogEntryNum(maxLogEntryNum))
            {
                scrollRect.totalCount = logEntries.Count;
            }

            // 每次添加对话日志后，输出当前最新日志到 Console
            ExportLatestLogEntry();
        }

        // 新增方法：记录分支选择日志（此时 displayData 为 null）
        public void AddChoiceEntry(string choiceText)
        {
            float height = 0f;
            float prefixHeight = (logEntries.Count > 0 ? logEntries[logEntries.Count - 1].prefixHeight : 0f);
            LogEntry entry = new LogEntry(height, prefixHeight, 0, 0, null, null);
            entry.branchChoice = choiceText;
            logEntries.Add(entry);
            if (scrollRect != null)
            {
                scrollRect.totalCount = logEntries.Count;
                scrollRect.RefillCellsFromEnd();
            }
            ExportLatestLogEntry();
        }

        // 输出最新一条日志记录到 Console（只输出当前这条记录的内容）
        // 输出最新一条日志记录到 Console，并按照指定格式显示对话和所有选项
        public void ExportLatestLogEntry()
        {
            if (logEntries.Count == 0)
                return;

            LogEntry currentEntry = logEntries[logEntries.Count - 1];
            string outputStr = "";

            // 如果是对话记录（displayData 不为空）
            if (currentEntry.displayData != null)
            {
                var displayDTO = new DialogueDisplayDataDTO(currentEntry.displayData);
                string displayName = "";
                string dialogueText = "";
                if (displayDTO.displayNames != null && displayDTO.displayNames.TryGetValue("ChineseSimplified", out var dn))
                {
                    displayName = dn;
                }
                if (displayDTO.dialogues != null && displayDTO.dialogues.TryGetValue("ChineseSimplified", out var dText))
                {
                    dialogueText = dText;
                }
                outputStr = displayName + "\n" + dialogueText;
            }
            // 如果最新记录为分支记录，则只显示分支信息
            else if (currentEntry.branchChoice != null)
            {
                outputStr = "选项：{\n" + currentEntry.branchChoice + "\n}";
            }
            else
            {
                outputStr = "";
            }

            // 收集所有分支选择日志（无论是否最新）
            var branchChoices = logEntries
                                .Where(entry => entry.branchChoice != null)
                                .Select(entry => entry.branchChoice)
                                .ToList();

            if (branchChoices.Count > 0)
            {
                outputStr += "\n选项：{\n";
                foreach (var branch in branchChoices)
                {
                    outputStr += branch + "\n";
                }
                outputStr += "}";
            }

            Debug.Log(outputStr);
        }


        private void OnFontSizeChanged()
        {
            if (scrollRect != null && active && scrollRect.totalCount > 0)
            {
                var firstIdx = scrollRect.GetFirstItem(out var _);
                var lastIdx = scrollRect.GetLastItem(out _);
                RefreshEntryHeight();
                if (lastIdx >= scrollRect.totalCount - 1)
                {
                    scrollRect.RefillCellsFromEnd();
                }
                else
                {
                    scrollRect.RefillCells(firstIdx);
                }
            }
            else
            {
                needRefreshEntryHeight = true;
            }
        }

        private void RefreshEntryHeight()
        {
            UpdateContentForTest();
            for (var i = 0; i < logEntries.Count; i++)
            {
                var text = (logEntries[i].displayData != null) ? logEntries[i].displayData.FormatNameDialogue() : "";
                float height = (contentForTest != null) ? contentForTest.GetPreferredHeight(text, contentDefaultWidth) : 0f;
                float prefixHeight = height + (i > 0 ? logEntries[i - 1].prefixHeight : 0);
                logEntries[i].height = height;
                logEntries[i].prefixHeight = prefixHeight;
            }
            needRefreshEntryHeight = false;
        }

        private bool RestrainLogEntryNum(int num)
        {
            int cnt = logEntries.Count;
            if (cnt <= num) return false;
            RemoveRange(0, cnt - num);
            return true;
        }

        private void RemoveRange(int index, int count)
        {
            logEntries.RemoveRange(index, count);
            for (int i = index; i < logEntries.Count; ++i)
            {
                logEntries[i].prefixHeight = logEntries[i].height + (i > 0 ? logEntries[i - 1].prefixHeight : 0);
            }
            logEntriesRestoreData.RemoveAll(x => x.index >= index && x.index < index + count);
            logEntriesRestoreData.ForEach(x =>
            {
                if (x.index >= index) x.index -= count;
            });
            if (scrollRect != null)
            {
                scrollRect.totalCount = logEntries.Count;
                scrollRect.RefillCellsFromEnd();
            }
        }

        public void Clear()
        {
            RemoveRange(0, logEntries.Count);
        }

        public LogEntry GetRandomLogEntry(System.Random random)
        {
            return random.Next(logEntries);
        }

        #region LoopScrollRect 接口实现

        public Vector2 GetItemsSize(int itemsCount)
        {
            if (itemsCount <= 0) return new Vector2(0, 0);
            itemsCount = Mathf.Min(itemsCount, logEntries.Count);
            var height = scrollLayout.padding.top + logEntries[itemsCount - 1].prefixHeight;
            if (itemsCount == logEntries.Count)
            {
                height += scrollLayout.padding.bottom;
            }
            return new Vector2(0, height);
        }

        private readonly Stack<Transform> pool = new Stack<Transform>();

        public GameObject GetObject(int index)
        {
            if (pool.Count == 0)
            {
                var element = Instantiate(logEntryPrefab, scrollRect.content);
                return element.gameObject;
            }
            Transform candidate = pool.Pop();
            candidate.SetParent(scrollRect.content, false);
            var go = candidate.gameObject;
            go.SetActive(true);
            return go;
        }

        public void ReturnObject(Transform trans)
        {
            trans.gameObject.SetActive(false);
            trans.SetParent(scrollRect.transform, false);
            pool.Push(trans);
        }

        public void ProvideData(Transform transform, int idx)
        {
            var logEntry = logEntries[idx];
            // 当这是分支选择记录时，避免调用 Init（因为 displayData 为 null），直接输出到 Console即可
            if (logEntry.branchChoice != null && logEntry.displayData == null)
            {
                Debug.Log("ProvideData: Branch Log Entry detected. Branch text: " + logEntry.branchChoice);
                return;
            }

            UnityAction onGoBackButtonClicked = () => MoveBack(logEntry, idx);
            UnityAction onPlayVoiceButtonClicked = null;
            var voices = logEntry.dialogueData?.voices;
            if (voices != null && GameCharacterController.CanPlayVoice(voices))
            {
                onPlayVoiceButtonClicked = () => GameCharacterController.ReplayVoice(voices);
            }
            var logEntryController = transform.GetComponent<LogEntryController>();
            logEntryController.Init(logEntry.displayData, onGoBackButtonClicked, onPlayVoiceButtonClicked, logEntry.height);
        }

        #endregion

        public void MoveBackWithCallback(LogEntry logEntry, Action onFinish)
        {
            var nodeRecord = checkpointManager.GetNodeRecord(logEntry.nodeOffset);
            gameState.MoveBackTo(nodeRecord, logEntry.checkpointOffset, logEntry.dialogueData.dialogueIndex);
            this.Hide(onFinish);
        }

        private int selectedLogEntryIndex = -1;

        private void MoveBack(LogEntry logEntry, int index)
        {
            if (index == selectedLogEntryIndex)
            {
                selectedLogEntryIndex = -1;
                Alert.Show(
                    null,
                    "log.moveback.confirm",
                    () => MoveBackWithCallback(logEntry, null),
                    null,
                    "LogMoveBack"
                );
            }
            else
            {
                selectedLogEntryIndex = index;
            }
        }

        public override void Show(bool doTransition, Action onFinish)
        {
            if (configManager.GetInt(LogViewFirstShownKey) == 0)
            {
                Alert.Show("log.first.hint");
                configManager.SetInt(LogViewFirstShownKey, 1);
            }
            base.Show(doTransition, onFinish);
            if (needRefreshEntryHeight)
            {
                RefreshEntryHeight();
            }
            scrollRect.RefillCellsFromEnd();
            scrollRect.verticalNormalizedPosition = 1f;
            selectedLogEntryIndex = -1;
            scrollRect.scrollable = false;
        }

        private const float MaxScrollIdleTime = 0.2f;
        private float scrollIdleTime;
        private float lastScrollPosition;

        protected override void OnActivatedUpdate()
        {
            base.OnActivatedUpdate();
            var delta = Mouse.current?.scroll.ReadValue().y ?? 0f;
            if (delta < -1e-3f)
            {
                scrollRect.GetVerticalOffsetAndSize(out var contentHeight, out _);
                if (contentHeight < scrollRect.viewport.rect.height)
                {
                    Hide();
                    return;
                }
                if (scrollIdleTime > MaxScrollIdleTime && lastScrollPosition > 1f - 1e-3f)
                {
                    Hide();
                    return;
                }
                scrollIdleTime = 0f;
            }
            else if (delta > 1e-3f)
            {
                scrollIdleTime = 0f;
            }
            else
            {
                scrollIdleTime += Time.unscaledDeltaTime;
            }
            if (scrollIdleTime > MaxScrollIdleTime)
            {
                lastScrollPosition = scrollRect.verticalNormalizedPosition;
            }
            if (!scrollRect.scrollable && scrollIdleTime > MaxScrollIdleTime)
            {
                scrollRect.scrollable = true;
            }
            if (scrollRect.scrollable)
            {
                if (Keyboard.current?[Key.UpArrow].isPressed == true)
                {
                    Cursor.visible = false;
                    scrollRect.velocity += scrollRect.scrollSensitivity * Vector2.down;
                }
                if (Keyboard.current?[Key.DownArrow].isPressed == true)
                {
                    Cursor.visible = false;
                    scrollRect.velocity += scrollRect.scrollSensitivity * Vector2.up;
                }
            }
        }

        #region Restoration

        public string restorableName => "LogController";

        [Serializable]
        private class LogControllerRestoreData : IRestoreData
        {
            public readonly List<LogEntryRestoreData> logEntriesRestoreData;
            public LogControllerRestoreData(LogController parent)
            {
                logEntriesRestoreData = parent.logEntriesRestoreData;
            }
        }

        public IRestoreData GetRestoreData()
        {
            return new LogControllerRestoreData(this);
        }

        public void Restore(IRestoreData restoreData)
        {
            logEntriesRestoreData.Clear();
            Clear();
            if (gameState.isUpgrading)
            {
                return;
            }
            var data = restoreData as LogControllerRestoreData;
            logEntriesRestoreData.AddRange(data.logEntriesRestoreData);
            int i = 0;
            var logEntryRestoreData = logEntriesRestoreData.FirstOrDefault();
            foreach (var pos in gameState.GetDialogueHistory(maxLogEntryNum))
            {
                var dialogueData = checkpointManager.GetReachedDialogueData(pos.nodeRecord.name, pos.dialogueIndex);
                DialogueDisplayData displayData = null;
                if (logEntryRestoreData != null && logEntryRestoreData.index == logEntries.Count)
                {
                    displayData = logEntryRestoreData.displayData;
                    ++i;
                    logEntryRestoreData = i < logEntriesRestoreData.Count ? logEntriesRestoreData[i] : null;
                }
                if (displayData == null)
                {
                    var node = gameState.GetNode(pos.nodeRecord.name);
                    var entry = node.GetDialogueEntryAt(pos.dialogueIndex);
                    displayData = entry.GetDisplayData();
                }
                AddEntry(pos.nodeRecord, pos.checkpointOffset, dialogueData, displayData);
            }
        }

        #endregion
    }
}
