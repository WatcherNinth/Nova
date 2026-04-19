using System.Collections.Generic;
using Interrorgation.MidLayer;
using UnityEngine;

namespace Interrorgation.UI.UISequence
{
    public class UISequenceManager : MonoBehaviour
    {
        private static UISequenceManager _instance;
        public static UISequenceManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<UISequenceManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("UISequenceManager");
                        _instance = go.AddComponent<UISequenceManager>();
                    }
                }
                return _instance;
            }
        }

        private Queue<IUICommand> _commandQueue = new Queue<IUICommand>();
        private IUICommand _currentJob = null;
        private HashSet<string> _dedupKeys = new HashSet<string>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void OnEnable()
        {
            UIEventDispatcher.OnActionCompleted += HandleActionCompleted;
        }

        private void OnDisable()
        {
            UIEventDispatcher.OnActionCompleted -= HandleActionCompleted;
        }

        public void Enqueue(IUICommand command, string dedupDescription = "")
        {
            // 去重检查
            if (_dedupKeys.Contains(command.DedupKey))
            {
                Debug.LogWarning($"[UISequenceManager] 丢弃重复命令: {command.DedupKey}");
                return;
            }

            _commandQueue.Enqueue(command);
            _dedupKeys.Add(command.DedupKey);
            ProcessNext();
        }

        private void ProcessNext()
        {
            if (_currentJob != null || _commandQueue.Count == 0) return;

            _currentJob = _commandQueue.Dequeue();
            Debug.Log($"[UISequenceManager] 执行命令: {_currentJob.CommandId} (Blocking: {_currentJob.IsBlocking})");
            _currentJob.Execute();

            // 如果是非阻塞命令，由于 Execute 内部已经立即调用了 DispatchActionCompleted，
            // 所以会通过 HandleActionCompleted 递归调用下一次 ProcessNext。
        }

        private void HandleActionCompleted(string actionId)
        {
            if (_currentJob != null && _currentJob.CommandId == actionId)
            {
                Debug.Log($"[UISequenceManager] 命令完成: {actionId}");
                _dedupKeys.Remove(_currentJob.DedupKey);
                _currentJob = null;
                ProcessNext();
            }
        }
    }
}
