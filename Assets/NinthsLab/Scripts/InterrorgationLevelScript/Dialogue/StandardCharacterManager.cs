using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // 引用 TMP

namespace FrontendEngine
{
    public class StandardCharacterManager : CharacterManagerBase
    {
        [Header("Scene Refs")]
        [SerializeField] private Transform characterRoot;
        [SerializeField] private GameObject characterPrefab;

        // [新增] 调试模式开关
        [SerializeField] private bool debugMode = true;

        private Dictionary<string, GameObject> _instances = new Dictionary<string, GameObject>();

        protected override void ShowCharacterVisuals(string id, Sprite sprite)
        {
            GameObject instance;
            Image img;
            TMP_Text debugLabel; // 用于显示 "AnQiao (Smile)"

            // 1. 获取或创建实例
            if (!_instances.TryGetValue(id, out instance))
            {
                instance = Instantiate(characterPrefab, characterRoot);
                instance.name = $"Char_{id}";
                _instances[id] = instance;
            }
            
            instance.SetActive(true);
            img = instance.GetComponent<Image>();
            
            // 尝试获取 Prefab 里的 Text 组件（用于调试显示名字）
            // 如果你懒得改 Prefab，这里可以用 GetComponentInChildren
            debugLabel = instance.GetComponentInChildren<TMP_Text>();

            // 2. 设置图片逻辑 (增强版)
            if (sprite != null)
            {
                // A. 有资源：正常显示
                img.sprite = sprite;
                img.color = Color.white; // 确保不透明
                img.SetNativeSize();
                
                if(debugLabel) debugLabel.text = ""; // 有图就不显示字了
            }
            else
            {
                // B. 无资源 (测试模式)：显示色块
                Debug.LogWarning($"[CharacterManager] 缺失立绘资源: {id}。使用调试占位符。");
                
                img.sprite = null; // 也就是默认的白色方块
                // 给个随机颜色或者固定颜色，区分不同角色
                img.color = new Color(0.5f, 0.7f, 1f, 0.8f); 
                
                // 强制设置一个固定大小，不然 null sprite 只有 10x10 像素
                img.rectTransform.sizeDelta = new Vector2(300, 600); 

                // 如果有文本组件，显示 ID
                if (debugLabel)
                {
                    // 获取不到 Variant 参数没关系，至少显示 ID
                    debugLabel.text = $"{id}\n(No Image)";
                    debugLabel.color = Color.black;
                }
            }
        }

        protected override void HideCharacterVisuals(string id)
        {
            if (_instances.TryGetValue(id, out var instance))
            {
                instance.SetActive(false);
            }
        }
    }
}