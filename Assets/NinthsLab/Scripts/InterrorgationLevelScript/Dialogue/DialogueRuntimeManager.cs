using System.Collections.Generic;
using Interrorgation.MidLayer;
using UnityEngine;

namespace DialogueSystem
{
    public class DialogueRuntimeManager : MonoBehaviour
    {
        private Queue<DialogueEntry> _currentQueue = new Queue<DialogueEntry>();
        private bool _isPlaying = false;
        private DialogueSource _currentSource;

        private void OnEnable()
        {
            DialogueEventDispatcher.OnRequestNextDialogue += PlayNext;
            DialogueEventDispatcher.IsInDialogueQueryEvent += GetIsPlaying;
            DialogueEventDispatcher.GetCurrentDialogueSourceEvent += GetCurrentSource;
        }

        private void OnDisable()
        {
            DialogueEventDispatcher.OnRequestNextDialogue -= PlayNext;
            DialogueEventDispatcher.IsInDialogueQueryEvent -= GetIsPlaying;
            DialogueEventDispatcher.GetCurrentDialogueSourceEvent -= GetCurrentSource;
        }

        private DialogueSource GetCurrentSource()
        {
            return _currentSource;
        }

        /// <summary>
        /// 接收来自后端(Coordinator)的新数据
        /// </summary>
        public void PushNewBatch(List<string> rawLines, DialogueSource source = null)
        {
            _currentSource = source;
            if (source != null)
            {
                DialogueEventDispatcher.DispatchDialogueSourceChanged(source);
            }

            var batch = NovaScriptParser.ParseBatch(rawLines);

            // 将新批次加入队列 (或者清空旧的，取决于设计，通常是追加)
            // 这里我们简单处理：如果之前播完了，直接开始；没播完就追加
            bool wasEmpty = _currentQueue.Count == 0;

            foreach (var entry in batch.Entries)
            {
                entry.Source = _currentSource;
                _currentQueue.Enqueue(entry);
            }

            if (!_isPlaying || wasEmpty)
            {
                _isPlaying = true;
                UIEventDispatcher.DispatchShowDialogues(rawLines);
                PlayNext();
            }
        }

        private void PlayNext()
        {
            if (!_isPlaying) return;
            if (_currentQueue.Count > 0)
            {
                var entry = _currentQueue.Dequeue();

                // 1. 执行伴随指令
                foreach (var cmd in entry.Commands)
                {
                    DialogueEventDispatcher.DispatchCharacterCommand(cmd);
                }

                // 2. 显示文本
                if (!string.IsNullOrEmpty(entry.Content))
                {
                    DialogueEventDispatcher.DispatchShowDialogue(entry);
                }
                else
                {
                    // 如果是纯指令节点（无文本），自动跳到下一条
                    // 避免卡在空文本上等待点击
                    PlayNext();
                }
            }
            else
            {
                _isPlaying = false;
                DialogueEventDispatcher.DispatchDialogueBatchEnded();
                UIEventDispatcher.DispatchActionCompleted("DialogueBatch");
                Debug.Log("[DialogueSystem] 当前批次播放完毕");
            }
        }
        public bool GetIsPlaying()
        {
            return _isPlaying;
        }
    }
}