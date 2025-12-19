using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using FrontendEngine.Dialogue.Models;

namespace FrontendEngine.Dialogue
{
    /// <summary>
    /// 场景背景显示组件
    /// 职责:
    ///   1. 显示和切换场景背景
    ///   2. 控制背景透明度和光照效果
    ///   3. 支持背景过渡动画
    ///   4. 管理背景资源加载
    /// </summary>
    public class SceneView : MonoBehaviour
    {
        [Header("UI引用")]
        [SerializeField]
        [Tooltip("背景图片组件")]
        private Image backgroundImage;

        [SerializeField]
        [Tooltip("用于淡入淡出的第二层背景 (切换时使用)")]
        private Image transitionBackgroundImage;

        [SerializeField]
        [Tooltip("光照遮罩 (用于调节亮度)")]
        private Image lightMaskImage;

        [SerializeField]
        [Tooltip("整体容器的CanvasGroup")]
        private CanvasGroup canvasGroup;

        [Header("动画设置")]
        [SerializeField]
        [Tooltip("背景切换动画时长")]
        private float transitionDuration = 0.5f;

        [SerializeField]
        [Tooltip("光照变化动画时长")]
        private float lightTransitionDuration = 0.3f;

        [Header("资源配置")]
        [SerializeField]
        [Tooltip("场景资源根路径")]
        private string sceneResourceBasePath = "Scenes/";

        [Header("调试")]
        [SerializeField]
        private bool debugLogging = true;

        private string currentSceneId;
        private Coroutine transitionCoroutine;
        private Coroutine lightCoroutine;

        void Awake()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            // 自动查找组件
            if (backgroundImage == null)
            {
                backgroundImage = GetComponent<Image>();
                if (backgroundImage == null)
                {
                    var images = GetComponentsInChildren<Image>();
                    if (images.Length > 0)
                        backgroundImage = images[0];
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

            // 初始化为可见
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
        }

        /// <summary>
        /// 更新场景显示
        /// </summary>
        public void UpdateScene(SceneDisplayInfo sceneInfo)
        {
            if (sceneInfo == null)
            {
                Debug.LogWarning("[SceneView] SceneDisplayInfo 为 null");
                return;
            }

            if (debugLogging)
                Debug.Log($"[SceneView] 更新场景: {sceneInfo.Id}");

            currentSceneId = sceneInfo.Id;

            // 加载背景 (如果路径不为空)
            if (!string.IsNullOrEmpty(sceneInfo.BackgroundResourcePath))
            {
                LoadBackground(sceneInfo.BackgroundResourcePath, sceneInfo.BackgroundAlpha);
            }

            // 更新可见性
            SetVisible(sceneInfo.IsBackgroundVisible);

            // 更新光照
            UpdateLighting(sceneInfo.LightIntensity);
        }

        /// <summary>
        /// 加载背景图片
        /// </summary>
        private void LoadBackground(string backgroundPath, float alpha = 1f)
        {
            if (backgroundImage == null)
            {
                Debug.LogError("[SceneView] backgroundImage 未设置");
                return;
            }

            if (string.IsNullOrEmpty(backgroundPath))
            {
                if (debugLogging)
                    Debug.LogWarning("[SceneView] 背景路径为空");
                return;
            }

            // 尝试从Resources加载
            Sprite sprite = Resources.Load<Sprite>(backgroundPath);

            if (sprite != null)
            {
                // 如果当前有背景且启用了过渡图层，使用过渡动画
                if (backgroundImage.sprite != null && transitionBackgroundImage != null)
                {
                    TransitionToNewBackground(sprite, alpha);
                }
                else
                {
                    // 直接切换
                    backgroundImage.sprite = sprite;
                    SetBackgroundAlpha(alpha);
                }

                if (debugLogging)
                    Debug.Log($"[SceneView] 成功加载背景: {backgroundPath}");
            }
            else
            {
                Debug.LogWarning($"[SceneView] 未找到背景资源: {backgroundPath}");
            }
        }

        /// <summary>
        /// 平滑过渡到新背景
        /// </summary>
        private void TransitionToNewBackground(Sprite newSprite, float targetAlpha)
        {
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
            }

            transitionCoroutine = StartCoroutine(BackgroundTransitionCoroutine(newSprite, targetAlpha));
        }

        /// <summary>
        /// 背景过渡协程
        /// </summary>
        private IEnumerator BackgroundTransitionCoroutine(Sprite newSprite, float targetAlpha)
        {
            if (transitionBackgroundImage == null)
            {
                // 无过渡图层，直接切换
                backgroundImage.sprite = newSprite;
                SetBackgroundAlpha(targetAlpha);
                yield break;
            }

            // 设置过渡图层为新背景
            transitionBackgroundImage.sprite = newSprite;
            transitionBackgroundImage.color = new Color(1f, 1f, 1f, 0f);
            transitionBackgroundImage.enabled = true;

            // 淡入新背景
            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / transitionDuration;
                
                // 新背景淡入
                float newAlpha = Mathf.Lerp(0f, targetAlpha, t);
                transitionBackgroundImage.color = new Color(1f, 1f, 1f, newAlpha);

                // 旧背景淡出
                float oldAlpha = Mathf.Lerp(targetAlpha, 0f, t);
                backgroundImage.color = new Color(1f, 1f, 1f, oldAlpha);

                yield return null;
            }

            // 切换：将过渡图层的内容移到主图层
            backgroundImage.sprite = newSprite;
            backgroundImage.color = new Color(1f, 1f, 1f, targetAlpha);
            transitionBackgroundImage.enabled = false;
        }

        /// <summary>
        /// 设置背景透明度
        /// </summary>
        private void SetBackgroundAlpha(float alpha)
        {
            if (backgroundImage != null)
            {
                Color color = backgroundImage.color;
                color.a = alpha;
                backgroundImage.color = color;
            }
        }

        /// <summary>
        /// 更新光照效果
        /// </summary>
        private void UpdateLighting(float intensity)
        {
            if (lightMaskImage == null)
            {
                // 无光照遮罩，跳过
                return;
            }

            if (lightCoroutine != null)
            {
                StopCoroutine(lightCoroutine);
            }

            lightCoroutine = StartCoroutine(LightTransitionCoroutine(intensity));
        }

        /// <summary>
        /// 光照过渡协程
        /// </summary>
        private IEnumerator LightTransitionCoroutine(float targetIntensity)
        {
            if (lightMaskImage == null) yield break;

            float startAlpha = lightMaskImage.color.a;
            // 光照强度越高，遮罩越透明
            float targetAlpha = 1f - targetIntensity;

            float elapsed = 0f;
            while (elapsed < lightTransitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / lightTransitionDuration;
                
                float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                Color color = lightMaskImage.color;
                color.a = currentAlpha;
                lightMaskImage.color = color;

                yield return null;
            }

            // 确保最终值准确
            Color finalColor = lightMaskImage.color;
            finalColor.a = targetAlpha;
            lightMaskImage.color = finalColor;
        }

        /// <summary>
        /// 设置场景可见性
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
            }
        }

        /// <summary>
        /// 淡入场景
        /// </summary>
        public void FadeIn(float duration = 0.5f)
        {
            if (canvasGroup != null)
            {
                StartCoroutine(FadeCanvasGroup(canvasGroup, 1f, duration));
            }
        }

        /// <summary>
        /// 淡出场景
        /// </summary>
        public void FadeOut(float duration = 0.5f)
        {
            if (canvasGroup != null)
            {
                StartCoroutine(FadeCanvasGroup(canvasGroup, 0f, duration));
            }
        }

        /// <summary>
        /// CanvasGroup淡入淡出协程
        /// </summary>
        private IEnumerator FadeCanvasGroup(CanvasGroup group, float targetAlpha, float duration)
        {
            float startAlpha = group.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                group.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            group.alpha = targetAlpha;
        }

        /// <summary>
        /// 清除场景
        /// </summary>
        public void Clear()
        {
            currentSceneId = null;

            if (backgroundImage != null)
            {
                backgroundImage.sprite = null;
            }

            if (transitionBackgroundImage != null)
            {
                transitionBackgroundImage.sprite = null;
                transitionBackgroundImage.enabled = false;
            }

            if (debugLogging)
                Debug.Log("[SceneView] 清除场景");
        }

        /// <summary>
        /// 获取当前场景ID
        /// </summary>
        public string GetCurrentSceneId()
        {
            return currentSceneId;
        }
    }
}
