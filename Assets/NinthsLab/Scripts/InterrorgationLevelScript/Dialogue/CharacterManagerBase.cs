using UnityEngine;
using System.Collections.Generic;
using DialogueSystem;

namespace FrontendEngine
{
    public abstract class CharacterManagerBase : MonoBehaviour
    {
        // 角色配置列表 (所有模式通用)
        [SerializeField] protected List<CharacterAsset> characterAssets;
        
        protected Dictionary<string, CharacterAsset> assetMap = new Dictionary<string, CharacterAsset>();
        
        // 存储当前显示的立绘对象
        protected Dictionary<string, GameObject> activeCharacters = new Dictionary<string, GameObject>();

        protected virtual void Awake()
        {
            foreach (var asset in characterAssets)
            {
                if(asset) assetMap[asset.CharacterId] = asset;
            }
        }

        protected virtual void OnEnable()
        {
            // 监听指令
            DialogueEventDispatcher.OnCharacterCommand += HandleCommand;
        }

        protected virtual void OnDisable()
        {
            DialogueEventDispatcher.OnCharacterCommand -= HandleCommand;
        }

        private void HandleCommand(ScriptCommand cmd)
        {
            string charId = cmd.Args.Length > 0 ? cmd.Args[0] : "";
            if (!assetMap.ContainsKey(charId)) return;

            if (cmd.Type == CommandType.Show)
            {
                string variant = cmd.Args.Length > 1 ? cmd.Args[1] : "";
                Sprite sprite = assetMap[charId].LoadSprite(variant);
                
                // 调用抽象方法：具体怎么显示 Sprite，由子类决定
                ShowCharacterVisuals(charId, sprite);
            }
            else if (cmd.Type == CommandType.Hide)
            {
                HideCharacterVisuals(charId);
            }
        }

        // --- 抽象方法 ---
        
        // 普通模式可能是在屏幕中间实例化一个 Image
        // 审讯模式可能是漫画分镜切入
        protected abstract void ShowCharacterVisuals(string id, Sprite sprite);
        
        protected abstract void HideCharacterVisuals(string id);
    }
}