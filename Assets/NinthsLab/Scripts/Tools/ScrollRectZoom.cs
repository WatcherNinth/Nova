using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(ScrollRect))]
public class ScrollRectZoom : MonoBehaviour, IScrollHandler
{
    [Header("Zoom Settings")]
    [Tooltip("缩放速度")]
    public float zoomSpeed = 0.1f;
    [Tooltip("最大缩放倍数")]
    public float maxScale = 3.0f;
    [Tooltip("最小缩放倍数")]
    public float minScale = 0.5f;

    private ScrollRect scrollRect;
    private RectTransform content;
    private RectTransform viewport;
    
    // 用于记录ScrollRect原本的灵敏度，以便在Ctrl按下时禁用滚动
    private float originalScrollSensitivity;

    void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
        content = scrollRect.content;
        viewport = scrollRect.viewport != null ? scrollRect.viewport : scrollRect.GetComponent<RectTransform>();
        originalScrollSensitivity = scrollRect.scrollSensitivity;
    }

    void Update()
    {
        // 检测 Ctrl 键状态来控制 ScrollRect 是否可以滚动
        // 如果按下了 Ctrl，我们将灵敏度设为0，防止滚轮造成页面滚动，只响应缩放
        bool isCtrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        scrollRect.scrollSensitivity = isCtrlHeld ? 0 : originalScrollSensitivity;
    }

    public void OnScroll(PointerEventData eventData)
    {
        // 只有按住 Ctrl 时才执行缩放逻辑
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            HandleZoom(eventData.scrollDelta.y);
        }
    }

    private void HandleZoom(float scrollDelta)
    {
        if (content == null || viewport == null) return;

        // 1. 计算缩放前的相关参数
        // 获取视口中心在世界坐标中的位置
        Vector3 viewportCenter = viewport.TransformPoint(viewport.rect.center);
        // 获取该中心点在 Content 局部坐标系下的位置 (这是我们缩放的锚点)
        Vector3 pivotInContent = content.InverseTransformPoint(viewportCenter);

        // 2. 计算并应用新的缩放
        Vector3 currentScale = content.localScale;
        float targetScaleVal = currentScale.x + scrollDelta * zoomSpeed;
        
        // 限制缩放范围
        targetScaleVal = Mathf.Clamp(targetScaleVal, minScale, maxScale);
        Vector3 newScale = new Vector3(targetScaleVal, targetScaleVal, 1f);

        // 应用缩放
        content.localScale = newScale;

        // 3. 位置修正：保持视口中心点对应的 Content 内容不变（即以中心缩放）
        // 缩放后，原本那个 pivotInContent 在世界坐标中跑偏了，我们需要把它拉回 viewportCenter
        Vector3 newPointInWorld = content.TransformPoint(pivotInContent);
        Vector3 diff = viewportCenter - newPointInWorld;
        content.position += diff;

        // 4. 边界修正：防止露出空白（核心需求）
        ClampContentToViewport();
    }

    /// <summary>
    /// 检查并限制 Content 的位置，确保不会露出空白背景
    /// </summary>
    private void ClampContentToViewport()
    {
        // 获取 Viewport 和 Content 的世界坐标角落
        // 顺序: 0=左下, 1=左上, 2=右上, 3=右下
        Vector3[] viewportCorners = new Vector3[4];
        Vector3[] contentCorners = new Vector3[4];
        
        viewport.GetWorldCorners(viewportCorners);
        content.GetWorldCorners(contentCorners);

        // 计算 Viewport 和 Content 在世界空间下的尺寸
        float viewportWidth = viewportCorners[2].x - viewportCorners[0].x;
        float viewportHeight = viewportCorners[2].y - viewportCorners[0].y;
        
        float contentWidth = contentCorners[2].x - contentCorners[0].x;
        float contentHeight = contentCorners[2].y - contentCorners[0].y;

        Vector3 offset = Vector3.zero;

        // --- 水平方向处理 ---
        if (contentWidth > viewportWidth)
        {
            // 如果内容比视口宽：
            // 1. 检查左边：如果 Content 左边比 Viewport 左边还要靠右（露出了左边空白），向左移
            if (contentCorners[0].x > viewportCorners[0].x)
            {
                offset.x = viewportCorners[0].x - contentCorners[0].x;
            }
            // 2. 检查右边：如果 Content 右边比 Viewport 右边还要靠左（露出了右边空白），向右移
            else if (contentCorners[2].x < viewportCorners[2].x)
            {
                offset.x = viewportCorners[2].x - contentCorners[2].x;
            }
        }
        else
        {
            // 如果内容比视口窄（比如缩放得很小），通常选择居中显示
            float centerX = viewportCorners[0].x + viewportWidth / 2;
            float contentCenterX = contentCorners[0].x + contentWidth / 2;
            offset.x = centerX - contentCenterX;
        }

        // --- 垂直方向处理 ---
        if (contentHeight > viewportHeight)
        {
            // 如果内容比视口高：
            // 1. 检查底边：如果 Content 底边比 Viewport 底边高（露出了底部空白），向下移
            if (contentCorners[0].y > viewportCorners[0].y)
            {
                offset.y = viewportCorners[0].y - contentCorners[0].y;
            }
            // 2. 检查顶边：如果 Content 顶边比 Viewport 顶边低（露出了顶部空白），向上移
            else if (contentCorners[1].y < viewportCorners[1].y)
            {
                offset.y = viewportCorners[1].y - contentCorners[1].y;
            }
        }
        else
        {
            // 如果内容比视口矮，居中显示
            float centerY = viewportCorners[0].y + viewportHeight / 2;
            float contentCenterY = contentCorners[0].y + contentHeight / 2;
            offset.y = centerY - contentCenterY;
        }

        // 应用修正偏移
        content.position += offset;
    }
}