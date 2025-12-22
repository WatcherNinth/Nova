using System.Collections.Generic;
using UnityEngine;

namespace FrontendEngine
{
    /// <summary>
    /// 角色配置数据
    /// 负责定义角色的基本信息以及立绘所在的资源路径
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharacter", menuName = "AVG/Character Asset")]
    public class CharacterAsset : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("角色的唯一ID，对应剧本指令中的 ID，例如 'AnQiao'")]
        public string CharacterId;

        [Tooltip("默认显示的名称，例如 '安乔'")]
        public string DefaultDisplayName;

        [Header("Resource Path")]
        [Tooltip("立绘资源文件夹路径 (相对于 Resources 文件夹)。\n例如: 'Characters/AnQiao'")]
        public string ImageFolderPath;

        [Tooltip("默认的差分名，当指令没有指定具体差分时使用。例如 'Normal'")]
        public string DefaultVariant = "Normal";

        // =========================================================
        // 运行时缓存 (Runtime Cache)
        // 避免每次显示同一张立绘都重新 Resource.Load，造成性能浪费
        // =========================================================
        private Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

        /// <summary>
        /// 根据差分名加载立绘 Sprite
        /// </summary>
        /// <param name="variant">差分文件名，例如 'Smile'</param>
        /// <returns>加载成功的 Sprite，如果失败返回 null</returns>
        public Sprite LoadSprite(string variant)
        {
            // 1. 处理空值，使用默认差分
            if (string.IsNullOrEmpty(variant))
            {
                variant = DefaultVariant;
            }

            // 2. 检查缓存
            if (_spriteCache.TryGetValue(variant, out Sprite cachedSprite))
            {
                if (cachedSprite != null) return cachedSprite;
                // 如果缓存里的对象被销毁了（比如切换场景），则从字典移除，重新加载
                _spriteCache.Remove(variant);
            }

            // 3. 构建完整资源路径
            // 路径格式: 文件夹路径/差分名
            // 例如: "Characters/AnQiao/Smile"
            string fullPath = $"{ImageFolderPath}/{variant}";

            // 4. 从 Resources 加载
            Sprite loadedSprite = Resources.Load<Sprite>(fullPath);

            if (loadedSprite != null)
            {
                _spriteCache[variant] = loadedSprite;
                return loadedSprite;
            }
            else
            {
                Debug.LogWarning($"[CharacterAsset] 无法在路径 '{fullPath}' 加载到立绘差分: {variant}");
                return null;
            }
        }

        /// <summary>
        /// 卸载缓存资源（可在关卡结束或内存警告时调用）
        /// </summary>
        public void UnloadCache()
        {
            _spriteCache.Clear();
            // 注意：Resources.UnloadUnusedAssets() 通常由全局管理器调用
        }
    }
}