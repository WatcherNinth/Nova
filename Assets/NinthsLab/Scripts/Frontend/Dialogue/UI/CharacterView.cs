using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using FrontendEngine.Dialogue.Models;

namespace FrontendEngine.Dialogue
{
    /// <summary>
    /// 角色立绘显示组件
    /// 职责:
    ///   1. 显示角色立绘图片
    ///   2. 根据 CharacterDisplayInfo 更新显示状态
    ///   3. 支持淡入淡出、位置切换等简单动画
    ///   4. 管理立绘资源的加载和卸载
    /// </summary>
    public class CharacterView : MonoBehaviour
    {
        [Header("UI引用")]
        [SerializeField]
        [Tooltip("立绘图片组件")]
        private Image characterImage;

        [SerializeField]
        [Tooltip("立绘容器 (用于整体控制)")]
        private CanvasGroup canvasGroup;

        [Header("位置配置")]
        [SerializeField]
        [Tooltip("此View对应的屏幕位置")]
        private CharacterPosition assignedPosition = CharacterPosition.Center;

        [SerializeField]
        [Tooltip("是否使用RectTransform的锚点位置")]
        private bool useAnchorPosition = true;

        [Header("动画设置")]
        [SerializeField]
        [Tooltip("淡入淡出动画时长")]
        private float fadeDuration = 0.3f;

        [SerializeField]
        [Tooltip("缩放动画时长")]
        private float scaleDuration = 0.2f;

        [Header("资源加载")]
        [SerializeField]
        [Tooltip("立绘资源根路径")]
        private string resourceBasePath = "Characters/";

        [Header("调试")]
        [SerializeField]
        private bool debugLogging = true;

        private string currentCharacterId;
        private Coroutine fadeCoroutine;
        private Coroutine scaleCoroutine;

        void Awake()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            // 自动查找组件
            if (characterImage == null)
            {
                characterImage = GetComponent<Image>();
                if (characterImage == null)
                {
                    characterImage = GetComponentInChildren<Image>();
                }
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            // 初始化为隐藏状态
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
        }

        /// <summary>
        /// 更新角色显示
        /// </summary>
        public void UpdateCharacter(CharacterDisplayInfo characterInfo)
        {
            if (characterInfo == null)
            {
                Debug.LogWarning("[CharacterView] CharacterDisplayInfo 为 null");
                return;
            }

            if (debugLogging)
                Debug.Log($"[CharacterView] 更新角色: {characterInfo.Name} (Position={characterInfo.Position})");

            // 检查位置是否匹配 (如果设置了位置)
            if (assignedPosition != characterInfo.Position && assignedPosition != CharacterPosition.Center)
            {
                // 此View不负责显示这个位置的角色
                return;
            }

            // 保存当前角色ID
            currentCharacterId = characterInfo.Id;

            // 加载立绘
            LoadCharacterSprite(characterInfo.SpriteResourcePath);

            // 更新显示状态
            UpdateDisplayState(characterInfo);
        }

        /// <summary>
        /// 加载角色立绘
        /// </summary>
        private void LoadCharacterSprite(string spritePath)
        {
            if (characterImage == null)
            {
                Debug.LogError("[CharacterView] characterImage 未设置");
                return;
            }

            if (string.IsNullOrEmpty(spritePath))
            {
                if (debugLogging)
                    Debug.LogWarning("[CharacterView] 立绘路径为空");
                return;
            }

            // 尝试从Resources加载
            Sprite sprite = Resources.Load<Sprite>(spritePath);

            if (sprite != null)
            {
                characterImage.sprite = sprite;
                characterImage.enabled = true;

                if (debugLogging)
                    Debug.Log($"[CharacterView] 成功加载立绘: {spritePath}");
            }
            else
            {
                Debug.LogWarning($"[CharacterView] 未找到立绘资源: {spritePath}");
                
                // 显示占位图或隐藏
                characterImage.enabled = false;
            }
        }

        /// <summary>
        /// 更新显示状态 (透明度、缩放等)
        /// </summary>
        private void UpdateDisplayState(CharacterDisplayInfo info)
        {
            // 更新透明度 (带动画)
            if (info.IsVisible)
            {
                FadeIn(info.Alpha);
            }
            else
            {
                FadeOut();
            }

            // 更新缩放 (带动画)
            if (info.Scale != transform.localScale.x)
            {
                AnimateScale(info.Scale);
            }
        }

        /// <summary>
        /// 淡入显示
        /// </summary>
        public void FadeIn(float targetAlpha = 1f)
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }

            fadeCoroutine = StartCoroutine(FadeCoroutine(targetAlpha));
        }

        /// <summary>
        /// 淡出隐藏
        /// </summary>
        public void FadeOut()
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }

            fadeCoroutine = StartCoroutine(FadeCoroutine(0f));
        }

        /// <summary>
        /// 淡入淡出协程
        /// </summary>
        private IEnumerator FadeCoroutine(float targetAlpha)
        {
            if (canvasGroup == null) yield break;

            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
        }

        /// <summary>
        /// 缩放动画
        /// </summary>
        private void AnimateScale(float targetScale)
        {
            if (scaleCoroutine != null)
            {
                StopCoroutine(scaleCoroutine);
            }

            scaleCoroutine = StartCoroutine(ScaleCoroutine(targetScale));
        }

        /// <summary>
        /// 缩放协程
        /// </summary>
        private IEnumerator ScaleCoroutine(float targetScale)
        {
            Vector3 startScale = transform.localScale;
            Vector3 endScale = new Vector3(targetScale, targetScale, 1f);
            float elapsed = 0f;

            while (elapsed < scaleDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / scaleDuration;
                transform.localScale = Vector3.Lerp(startScale, endScale, t);
                yield return null;
            }

            transform.localScale = endScale;
        }

        /// <summary>
        /// 隐藏角色
        /// </summary>
        public void Hide()
        {
            FadeOut();

            if (debugLogging)
                Debug.Log("[CharacterView] 隐藏角色");
        }

        /// <summary>
        /// 显示角色 (使用当前设置)
        /// </summary>
        public void Show()
        {
            FadeIn();

            if (debugLogging)
                Debug.Log("[CharacterView] 显示角色");
        }

        /// <summary>
        /// 立即设置透明度 (无动画)
        /// </summary>
        public void SetAlphaImmediate(float alpha)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = alpha;
            }
        }

        /// <summary>
        /// 立即设置缩放 (无动画)
        /// </summary>
        public void SetScaleImmediate(float scale)
        {
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        /// <summary>
        /// 清除当前显示的角色
        /// </summary>
        public void Clear()
        {
            Hide();
            currentCharacterId = null;

            if (characterImage != null)
            {
                characterImage.sprite = null;
                characterImage.enabled = false;
            }
        }

        /// <summary>
        /// 获取当前角色ID
        /// </summary>
        public string GetCurrentCharacterId()
        {
            return currentCharacterId;
        }

        /// <summary>
        /// 检查是否正在显示指定角色
        /// </summary>
        public bool IsShowingCharacter(string characterId)
        {
            return currentCharacterId == characterId && canvasGroup != null && canvasGroup.alpha > 0f;
        }
    }
}
