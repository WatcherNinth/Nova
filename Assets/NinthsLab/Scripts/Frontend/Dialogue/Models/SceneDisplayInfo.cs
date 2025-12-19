namespace FrontendEngine.Dialogue.Models
{
    /// <summary>
    /// 场景显示信息 (纯数据, 无逻辑)
    /// 职责: 封装场景相关的UI显示数据
    /// 用途: DialogueUIPanel 用此信息更新背景、特效等场景元素
    /// </summary>
    public class SceneDisplayInfo
    {
        /// <summary>
        /// 场景唯一标识符
        /// </summary>
        public string Id { get; set; } = "";

        /// <summary>
        /// 场景背景资源路径 (e.g. "Scenes/Office/day")
        /// </summary>
        public string BackgroundResourcePath { get; set; } = "";

        /// <summary>
        /// 场景背景是否应该显示
        /// </summary>
        public bool IsBackgroundVisible { get; set; } = true;

        /// <summary>
        /// 背景透明度 (0-1)
        /// </summary>
        public float BackgroundAlpha { get; set; } = 1f;

        /// <summary>
        /// 场景环境光强度 (用于调节整体亮度)
        /// </summary>
        public float LightIntensity { get; set; } = 1f;

        /// <summary>
        /// 是否显示对话框背景
        /// </summary>
        public bool ShowDialogueBoxBackground { get; set; } = true;

        public override string ToString()
        {
            return $"Scene({Id}, BgVisible={IsBackgroundVisible}, Alpha={BackgroundAlpha})";
        }
    }
}
