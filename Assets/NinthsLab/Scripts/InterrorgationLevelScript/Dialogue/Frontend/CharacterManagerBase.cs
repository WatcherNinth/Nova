using System.Collections.Generic;
using UnityEngine;
using DialogueSystem;
using FrontendEngine.Logic; // 引用 IStageLayoutProvider
using FrontendEngine.Data;  // 引用 NovaTransformData

namespace FrontendEngine
{
    public abstract class CharacterManagerBase : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] protected List<CharacterAsset> characterAssets;

        [Header("Dependencies")]
        [Tooltip("请拖入场景中的 StageLayoutManager")]
        [SerializeField] private MonoBehaviour stageLayoutProviderRef;
        
        // 实际使用的接口引用
        protected IStageLayoutProvider _layoutProvider;

        // 角色ID -> 资源配置的映射
        protected Dictionary<string, CharacterAsset> assetMap = new Dictionary<string, CharacterAsset>();

        protected virtual void Awake()
        {
            // 1. 初始化接口
            if (stageLayoutProviderRef is IStageLayoutProvider provider)
            {
                _layoutProvider = provider;
            }
            else if (stageLayoutProviderRef != null)
            {
                Debug.LogError($"[CharacterManager] 赋值的 StageLayoutProviderRef ({stageLayoutProviderRef.name}) 没有实现 IStageLayoutProvider 接口！");
            }

            // 2. 初始化资源索引
            foreach (var asset in characterAssets)
            {
                if (asset) assetMap[asset.CharacterId] = asset;
            }
        }

        protected virtual void OnEnable()
        {
            DialogueEventDispatcher.OnCharacterCommand += HandleCommand;
        }

        protected virtual void OnDisable()
        {
            DialogueEventDispatcher.OnCharacterCommand -= HandleCommand;
        }

        private void HandleCommand(ScriptCommand cmd)
        {
            // 安全检查：至少要有 CharacterID
            if (cmd.Args == null || cmd.Args.Length == 0) return;
            
            string charId = cmd.Args[0];

            if (cmd.Type == CommandType.Show)
            {
                // 参数解析
                // Arg[0]: ID
                // Arg[1]: Variant (差分名), 可选
                // Arg[2]: Position (位置Key 或 Table字符串), 可选
                
                string variant = cmd.Args.Length > 1 ? cmd.Args[1] : null;
                string posRaw = cmd.Args.Length > 2 ? cmd.Args[2] : null;

                // 加载立绘资源
                Sprite sprite = null;
                if (assetMap.TryGetValue(charId, out var asset))
                {
                    sprite = asset.LoadSprite(variant);
                }
                else
                {
                    Debug.LogWarning($"[CharacterManager] 未找到角色配置: {charId}");
                }

                // 1. 显示/创建立绘 (由子类实现具体逻辑)
                // 子类必须返回创建/获取到的 GameObject 实例，以便基类应用坐标
                GameObject instance = ShowCharacter(charId, sprite);

                // 2. 应用坐标变换 (如果有位置参数且实例有效)
                if (instance != null && !string.IsNullOrEmpty(posRaw))
                {
                    if (_layoutProvider != null)
                    {
                        NovaTransformData transformData = _layoutProvider.ResolvePosition(posRaw);
                        ApplyTransform(instance, transformData);
                    }
                    else
                    {
                        Debug.LogWarning("[CharacterManager] 收到位置参数，但 StageLayoutProvider 未连接，无法解析坐标。");
                    }
                }
            }
            else if (cmd.Type == CommandType.Hide)
            {
                HideCharacter(charId);
            }
        }

        /// <summary>
        /// 将解析后的坐标数据应用到立绘物体上
        /// </summary>
        private void ApplyTransform(GameObject charObj, NovaTransformData data)
        {
            if (data == null) return;

            RectTransform rt = charObj.GetComponent<RectTransform>();
            if (rt == null) return;

            // 1. 位置 (X, Y) -> AnchoredPosition
            if (data.X.HasValue || data.Y.HasValue)
            {
                Vector2 anchored = rt.anchoredPosition;
                if (data.X.HasValue) anchored.x = data.X.Value;
                if (data.Y.HasValue) anchored.y = data.Y.Value;
                rt.anchoredPosition = anchored;
            }

            // 2. 深度 (Z) -> LocalPosition.z
            if (data.Z.HasValue)
            {
                Vector3 localPos = rt.localPosition;
                localPos.z = data.Z.Value;
                rt.localPosition = localPos;
            }

            // 3. 缩放 (Scale)
            if (data.Scale.HasValue)
            {
                rt.localScale = data.Scale.Value;
            }

            // 4. 旋转 (Angle)
            if (data.Angle.HasValue)
            {
                rt.localEulerAngles = data.Angle.Value;
            }
        }

        // --- 抽象方法：由具体实现类 (如 StandardCharacterManager) 完成 ---
        
        /// <summary>
        /// 显示角色。如果角色已存在，则更新图片；如果不存在，则创建。
        /// </summary>
        /// <returns>返回立绘的 GameObject 实例</returns>
        protected abstract GameObject ShowCharacter(string id, Sprite sprite);

        /// <summary>
        /// 隐藏或销毁角色
        /// </summary>
        protected abstract void HideCharacter(string id);
    }
}