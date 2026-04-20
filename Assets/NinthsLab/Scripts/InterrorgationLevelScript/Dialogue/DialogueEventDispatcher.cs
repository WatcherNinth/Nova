using System;
using UnityEngine;

namespace DialogueSystem
{
    public static class DialogueEventDispatcher
    {
        // --- 逻辑层 -> 表现层 ---

        // 1. 显示对话 (通知 UI 刷新文字)
        public static event Action<DialogueEntry> OnShowDialogue;
        public static void DispatchShowDialogue(DialogueEntry entry)
        {
            OnShowDialogue?.Invoke(entry);
        }

        // 2. 执行立绘指令 (通知 CharacterManager)
        public static event Action<ScriptCommand> OnCharacterCommand;
        public static void DispatchCharacterCommand(ScriptCommand cmd)
        {
            OnCharacterCommand?.Invoke(cmd);
        }

        // 3. 对话结束 (当前 Batch 播放完毕)
        public static event Action OnDialogueBatchEnded;
        public static void DispatchDialogueBatchEnded()
        {
            OnDialogueBatchEnded?.Invoke();
        }

        // 4. 对话来源变更 (当前对话的来源信息更新)
        public static event Action<DialogueSource> OnDialogueSourceChanged;
        public static void DispatchDialogueSourceChanged(DialogueSource source)
        {
            OnDialogueSourceChanged?.Invoke(source);
        }

        // --- 表现层 -> 逻辑层 ---

        // 5. 请求下一句 (UI 打字结束且玩家点击后触发)
        public static event Action OnRequestNextDialogue;
        public static void DispatchRequestNext()
        {
            OnRequestNextDialogue?.Invoke();
        }

        public static event Func<bool> IsInDialogueQueryEvent;
        public static bool GetIsInDialogue() => IsInDialogueQueryEvent.Invoke();

        public static event Func<DialogueSource> GetCurrentDialogueSourceEvent;
        public static DialogueSource GetCurrentDialogueSource() => GetCurrentDialogueSourceEvent?.Invoke();
    }
}