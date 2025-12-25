using System.Collections.Generic;
using UnityEngine;

namespace DialogueSystem
{
    /// <summary>
    /// 日志管理器 (Logic Layer)
    /// 职责：监听对话事件，维护历史记录数据。不涉及任何 UI 显示。
    /// </summary>
    public class DialogueLogManager : MonoBehaviour
    {
        // 单例，方便 UI 层获取数据
        public static DialogueLogManager Instance { get; private set; }

        [Header("Settings")]
        [Tooltip("最大记录条数，防止内存无限增长")]
        public int maxLogCount = 100;

        // 核心数据：存储完整的 DialogueEntry 对象
        private List<DialogueEntry> _history = new List<DialogueEntry>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void OnEnable()
        {
            // 监听对话显示事件
            DialogueEventDispatcher.OnShowDialogue += RecordDialogue;
        }

        private void OnDisable()
        {
            DialogueEventDispatcher.OnShowDialogue -= RecordDialogue;
        }

        private void RecordDialogue(DialogueEntry entry)
        {
            // 过滤掉没有内容的条目 (纯指令)
            if (string.IsNullOrEmpty(entry.Content)) return;

            _history.Add(entry);

            // 维护最大数量
            if (_history.Count > maxLogCount)
            {
                _history.RemoveAt(0);
            }
        }

        /// <summary>
        /// 获取当前的只读历史列表
        /// </summary>
        public IEnumerable<DialogueEntry> GetLogs()
        {
            return _history;
        }

        /// <summary>
        /// 清空日志 (比如重新开始游戏时)
        /// </summary>
        public void ClearLogs()
        {
            _history.Clear();
        }
    }
}