using System;

namespace Interrorgation.UI.UISequence
{
    /// <summary>
    /// 通用 UI 积压模块接口
    /// 实现此接口的模块应能自主决定是立即播放演出还是存入积压。
    /// </summary>
    public interface IUIBacklogModule
    {
        /// <summary>
        /// 当前视觉面板是否处于活跃/可播放动画状态
        /// </summary>
        bool IsVisualActive { get; }

        /// <summary>
        /// 执行或记录积压操作
        /// 如果模块发现当前不可播放动画，应将 action 缓存并返回 false；
        /// 如果可以立即执行或已开始播放，返回 true。
        /// </summary>
        void RecordBacklog(Action action);

        /// <summary>
        /// 顺序执行该模块下所有积压的演出操作
        /// </summary>
        void ProcessBacklog();
    }
}
