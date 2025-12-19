namespace FrontendEngine.Dialogue.Models
{
    /// <summary>
    /// 角色显示信息 (纯数据, 无逻辑)
    /// 职责: 封装角色相关的UI显示数据
    /// 用途: DialogueUIPanel 用此信息定位并显示角色立绘、名字等
    /// </summary>
    public class CharacterDisplayInfo
    {
        /// <summary>
        /// 角色唯一标识符 (用于查询立绘资源)
        /// </summary>
        public string Id { get; set; } = "";

        /// <summary>
        /// 角色显示名字
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// 角色立绘资源路径 (e.g. "Characters/Alice/happy")
        /// </summary>
        public string SpriteResourcePath { get; set; } = "";

        /// <summary>
        /// 角色在屏幕上的位置 (Left, Center, Right)
        /// </summary>
        public CharacterPosition Position { get; set; } = CharacterPosition.Center;

        /// <summary>
        /// 角色是否应该显示 (用于过渡动画)
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// 角色透明度 (0-1)
        /// </summary>
        public float Alpha { get; set; } = 1f;

        /// <summary>
        /// 角色立绘缩放
        /// </summary>
        public float Scale { get; set; } = 1f;

        public override string ToString()
        {
            return $"Character({Name}, Pos={Position}, Visible={IsVisible})";
        }
    }

    /// <summary>
    /// 角色屏幕位置枚举
    /// </summary>
    public enum CharacterPosition
    {
        Left,
        Center,
        Right,
        BackgroundLeft,
        BackgroundRight
    }
}
