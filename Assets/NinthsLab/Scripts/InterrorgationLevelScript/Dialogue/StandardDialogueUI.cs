using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // 引用 UI
using DialogueSystem;

namespace FrontendEngine
{
    public class StandardDialogueUI : DialogueUIBase
    {
        [Header("Standard Components")]
        [SerializeField] private GameObject nextArrow;

        [Header("Avatar System")]
        [Tooltip("主角的角色ID (必须与剧本一致)，此角色的对话会显示头像")]
        public string protagonistId = "AnLee"; 

        [SerializeField] private Image avatarImage; // 头像显示的Image组件
        [SerializeField] private GameObject avatarContainer; // 头像的父物体(用于控制显示/隐藏)

        [Header("Data Source")]
        // UI 也需要访问角色配置，以便加载头像图片
        [SerializeField] private List<CharacterAsset> characterAssets;
        
        private Dictionary<string, CharacterAsset> _assetMap = new Dictionary<string, CharacterAsset>();
        private Dictionary<string, string> _nameToIdMap = new Dictionary<string, string>();
        private string _currentProtagonistVariant = "Normal"; // 记录主角当前的面部状态

        protected override void Awake()
        {
            base.Awake();
            
            // 初始化两个索引字典
            foreach (var asset in characterAssets)
            {
                if (!asset) continue;

                // 1. ID 索引
                _assetMap[asset.CharacterId] = asset;

                // 2. [新增] 名字索引 (处理 "安·李" -> "AnLee")
                if (!string.IsNullOrEmpty(asset.DefaultDisplayName))
                {
                    // 防止重复key报错，以后加入的为准
                    if (!_nameToIdMap.ContainsKey(asset.DefaultDisplayName))
                    {
                        _nameToIdMap[asset.DefaultDisplayName] = asset.CharacterId;
                    }
                }
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            // [关键] UI 也需要监听立绘指令，以便更新主角的表情状态
            DialogueEventDispatcher.OnCharacterCommand += HandleCharacterCommand;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            DialogueEventDispatcher.OnCharacterCommand -= HandleCharacterCommand;
        }

        // --- 1. 处理指令 (更新状态) ---
        private void HandleCharacterCommand(ScriptCommand cmd)
        {
            if (cmd.Args != null && cmd.Args.Length > 0)
            {
                string charId = cmd.Args[0];
                
                // [修复] 增加非空判断
                if (!string.IsNullOrEmpty(charId) && _nameToIdMap.TryGetValue(charId, out string realId)) 
                {
                    charId = realId;
                }

                if (charId == protagonistId && cmd.Type == CommandType.Show)
                {
                    string variant = cmd.Args.Length > 1 ? cmd.Args[1] : null;
                    if (!string.IsNullOrEmpty(variant))
                    {
                        _currentProtagonistVariant = variant;
                        RefreshAvatar(charId); 
                    }
                }
            }
        }

        // --- 2. 显示对话前 (决定是否显示头像) ---
        protected override void OnBeforeDisplay(DialogueEntry entry)
        {
            if (nextArrow) nextArrow.SetActive(false);

            // =========================================================
            // [修复] 增加非空判断 (!string.IsNullOrEmpty)
            // =========================================================
            // 只有当名字不为空，且 ID 等于名字时，才尝试去字典查找
            if (!string.IsNullOrEmpty(entry.DisplayName) && entry.CharacterId == entry.DisplayName)
            {
                if (_nameToIdMap.TryGetValue(entry.DisplayName, out string realId))
                {
                    entry.CharacterId = realId;
                }
            }
            // =========================================================

            if (entry.CharacterId == protagonistId)
            {
                ShowAvatar();
            }
            else
            {
                HideAvatar();
            }
        }

        protected override void OnTypingComplete()
        {
            if (nextArrow) nextArrow.SetActive(true);
        }

        // --- 3. 头像逻辑 ---

        private void ShowAvatar()
        {
            if (avatarContainer) avatarContainer.SetActive(true);
            RefreshAvatar(protagonistId);
        }

        private void HideAvatar()
        {
            if (avatarContainer) avatarContainer.SetActive(false);
        }

        private void RefreshAvatar(string charId)
        {
            if (avatarImage == null) return;
            if (!_assetMap.TryGetValue(charId, out var asset)) return;

            Sprite faceSprite = asset.LoadSprite(_currentProtagonistVariant);
            
            if (faceSprite != null)
            {
                avatarImage.sprite = faceSprite;
                avatarImage.SetNativeSize(); 
            }
        }
    }
}