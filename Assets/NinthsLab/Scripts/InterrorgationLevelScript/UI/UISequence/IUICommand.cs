using System;
using Interrorgation.MidLayer;

namespace Interrorgation.UI.UISequence
{
    public interface IUICommand
    {
        string CommandId { get; }
        bool IsBlocking { get; }
        string DedupKey { get; }
        void Execute();
    }

    /// <summary>
    /// 标准 UI 表现命令（支持信号反馈）
    /// </summary>
    public class UINotifyCommand : IUICommand
    {
        public string CommandId { get; }
        public bool IsBlocking { get; }
        private Action<string> _action;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="id">用于信号回传的 ID</param>
        /// <param name="action">具体的执行逻辑，Action 携带 id 方便 UI 侧回传信号</param>
        /// <param name="isBlocking">是否阻塞队列</param>
        /// <param name="dedupDescription">去重描述字符串</param>
        public UINotifyCommand(string id, Action<string> action, bool isBlocking = false, string dedupDescription = "")
        {
            CommandId = id;
            _action = action;
            IsBlocking = isBlocking;
            _dedupDescription = dedupDescription;
        }

        public string DedupKey => CommandId + "|" + (string.IsNullOrEmpty(_dedupDescription) ? "" : _dedupDescription);
        private string _dedupDescription;

        public void Execute()
        {
            // 派发表现逻辑
            _action?.Invoke(CommandId);
            
            // 如果是非阻塞命令，派发完毕后立即通报队列完成
            if (!IsBlocking)
            {
                UIEventDispatcher.DispatchActionCompleted(CommandId);
            }
        }
    }

    /// <summary>
    /// 对话系统专用命令 (阻塞队列直到对话 Batch 结束)
    /// </summary>
    public class UIDialogueCommand : IUICommand
    {
        public string CommandId { get; }
        public bool IsBlocking => true;
        private Action _action;

        public UIDialogueCommand(string id, Action action, string dedupDescription = "")
        {
            CommandId = id;
            _action = action;
            _dedupDescription = dedupDescription;
        }

        public string DedupKey => CommandId + "|" + (string.IsNullOrEmpty(_dedupDescription) ? "" : _dedupDescription);
        private string _dedupDescription;

        public void Execute()
        {
            _action?.Invoke();
        }
    }
}
