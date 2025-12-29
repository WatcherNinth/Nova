using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FrontendEngine
{
    public class StandardCharacterManager : CharacterManagerBase
    {
        [Header("Scene Refs")]
        [SerializeField] private Transform characterRoot;
        [SerializeField] private GameObject characterPrefab;

        private Dictionary<string, GameObject> _instances = new Dictionary<string, GameObject>();

        // [修复] 返回类型改为 GameObject，与基类保持一致
        protected override GameObject ShowCharacter(string id, Sprite sprite)
        {
            GameObject instance;
            Image img;
            TMP_Text debugLabel;

            // 1. 获取或创建实例
            if (!_instances.TryGetValue(id, out instance))
            {
                instance = Instantiate(characterPrefab, characterRoot);
                instance.name = $"Char_{id}";
                _instances[id] = instance;
            }
            
            instance.SetActive(true);
            img = instance.GetComponent<Image>();
            
            // 尝试获取调试文本
            debugLabel = instance.GetComponentInChildren<TMP_Text>();

            // 2. 设置图片逻辑
            if (sprite != null)
            {
                // A. 有资源
                img.sprite = sprite;
                img.color = Color.white; 
                img.SetNativeSize();
                
                if(debugLabel) debugLabel.text = ""; 
            }
            else
            {
                // B. 无资源 (调试模式)
                Debug.LogWarning($"[CharacterManager] 缺失立绘资源: {id}");
                
                img.sprite = null; 
                img.color = new Color(0.5f, 0.7f, 1f, 0.8f); 
                img.rectTransform.sizeDelta = new Vector2(300, 600); 

                if (debugLabel)
                {
                    debugLabel.text = $"{id}\n(No Image)";
                    debugLabel.color = Color.black;
                }
            }

            // [修复] 必须返回当前操作的 GameObject 实例
            return instance;
        }

        protected override void HideCharacter(string id)
        {
            if (_instances.TryGetValue(id, out var instance))
            {
                instance.SetActive(false);
            }
        }
    }
}