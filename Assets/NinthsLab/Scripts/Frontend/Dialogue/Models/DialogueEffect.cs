using System.Collections.Generic;
using UnityEngine;

namespace FrontendEngine.Dialogue.Models
{
    /// <summary>
    /// 对话特效数据模型 (纯数据, 无逻辑)
    /// 职责: 描述UI呈现时应应用的特效
    /// 用途: DialogueUIPanel 根据此数据播放对应的动画或视觉效果
    /// 设计: 特效应在UI层完全实现，后端仅需声明特效类型和参数
    /// </summary>
    public class DialogueEffect
    {
        /// <summary>
        /// 特效类型
        /// </summary>
        public DialogueEffectType Type { get; set; }

        /// <summary>
        /// 特效持续时间 (秒)
        /// </summary>
        public float Duration { get; set; } = 0.5f;

        /// <summary>
        /// 特效是否应该在显示文本时播放 (true) 还是在隐藏时播放 (false)
        /// </summary>
        public bool PlayOnShow { get; set; } = true;

        /// <summary>
        /// 特效参数 (用于灵活的特效配置)
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        public override string ToString()
        {
            return $"Effect({Type}, Duration={Duration}s, OnShow={PlayOnShow})";
        }
    }

    /// <summary>
    /// 对话特效类型枚举
    /// </summary>
    public enum DialogueEffectType
    {
        /// <summary>
        /// 无特效
        /// </summary>
        None = 0,

        /// <summary>
        /// 渐隐渐显 (Alpha 淡入淡出)
        /// </summary>
        FadeInOut = 1,

        /// <summary>
        /// 弹跳入场
        /// </summary>
        BounceIn = 2,

        /// <summary>
        /// 平移进入
        /// </summary>
        SlideIn = 3,

        /// <summary>
        /// 抖动特效
        /// </summary>
        Shake = 4,

        /// <summary>
        /// 放大动画
        /// </summary>
        ScaleUp = 5,

        /// <summary>
        /// 文字逐个显示 (打字效果)
        /// </summary>
        TypewriterEffect = 6,

        /// <summary>
        /// 闪光特效
        /// </summary>
        Flash = 7,

        /// <summary>
        /// 旋转进入
        /// </summary>
        RotateIn = 8
    }
}
