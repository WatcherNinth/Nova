namespace FrontendEngine.Logic
{
    using FrontendEngine.Data;

    public interface IStageLayoutProvider
    {
        /// <summary>
        /// 将原始位置参数（可能是 Key，也可能是 Lua Table 字符串）解析为统一的数据结构
        /// </summary>
        /// <param name="posRaw">例如 "pos_left" 或 "{0, -100, {1,1,1}, 0, {0,0,45}}"</param>
        NovaTransformData ResolvePosition(string posRaw);
    }
}