using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LogicEngine.UI
{
    /// <summary>
    /// 简易流式布局组件。
    /// 支持自动换行、统一高度、以及容器自适应或固定高度模式。
    /// </summary>
    [ExecuteAlways] // 允许在编辑模式下实时运行
    [RequireComponent(typeof(RectTransform))]
    public class SimpleFlowLayout : LayoutGroup
    {
        public enum FitMode
        {
            /// <summary>
            /// 容器高度随内容自动扩展 (Overflow)
            /// </summary>
            Expand,
            /// <summary>
            /// 容器高度固定，内容过多时保持容器尺寸不变 (Truncate/Fixed)
            /// </summary>
            Fixed
        }

        [Header("Layout Settings")]
        public float SpacingX = 5f;
        public float SpacingY = 5f;
        
        [Tooltip("如果勾选，所有子元素的高度将被强制设为最高元素的那个高度")]
        public bool UniformItemHeight = false;

        [Header("Container Settings")]
        [Tooltip("Expand: 容器高度自动撑开。\nFixed: 容器高度固定(可在编辑器手动拖拽)，内容超出不影响容器大小。")]
        public FitMode ContainerSizeMode = FitMode.Expand;

        // 缓存子物体列表避免GC
        private readonly List<RectTransform> _childrenList = new List<RectTransform>();

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            CalculateLayout();
        }

        public override void CalculateLayoutInputVertical()
        {
            // Vertical pass is driven by Horizontal pass in this flow layout
            CalculateLayout();
        }

        public override void SetLayoutHorizontal() { }
        public override void SetLayoutVertical() { }

        private void CalculateLayout()
        {
            // 1. 收集有效子物体
            _childrenList.Clear();
            for (int i = 0; i < transform.childCount; i++)
            {
                var rect = transform.GetChild(i) as RectTransform;
                if (rect == null || !rect.gameObject.activeInHierarchy) continue;
                _childrenList.Add(rect);
            }

            // 2. 预计算：如果需要统一高度，先找出最大高度
            float forcedHeight = 0f;
            if (UniformItemHeight)
            {
                foreach (var child in _childrenList)
                {
                    float h = LayoutUtility.GetPreferredHeight(child);
                    if (h > forcedHeight) forcedHeight = h;
                }
            }

            // 3. 排列逻辑
            float containerWidth = rectTransform.rect.width;
            float currentX = padding.left;
            float currentY = padding.top;
            float currentRowMaxHeight = 0f;

            // 为了处理第一行的特殊情况，或者只有一行的情况
            bool isFirstElement = true;

            foreach (var child in _childrenList)
            {
                float childWidth = LayoutUtility.GetPreferredWidth(child);
                // 如果开启了Uniform，则使用预计算的高度，否则使用自身偏好高度
                float childHeight = UniformItemHeight ? forcedHeight : LayoutUtility.GetPreferredHeight(child);

                // 判断是否需要换行
                // currentX + childWidth > AvailableWidth
                if (!isFirstElement && (currentX + childWidth > containerWidth - padding.right))
                {
                    // 换行：X归位，Y增加上一行的高度 + 间距
                    currentX = padding.left;
                    currentY += currentRowMaxHeight + SpacingY;
                    
                    // 重置当前行最高值 (如果是Uniform模式，其实currentRowMaxHeight恒等于forcedHeight)
                    currentRowMaxHeight = 0f;
                }

                // 定位子物体
                SetChildAlongAxis(child, 0, currentX, childWidth);
                SetChildAlongAxis(child, 1, currentY, childHeight);

                // 更新游标
                currentX += childWidth + SpacingX;
                currentRowMaxHeight = Mathf.Max(currentRowMaxHeight, childHeight);
                isFirstElement = false;
            }

            // 4. 处理容器高度
            if (ContainerSizeMode == FitMode.Expand)
            {
                // 计算所需总高度：当前Y + 最后一行高度 + 底部Padding
                // 注意：如果列表为空，currentY是padding.top，currentRowMaxHeight是0
                float totalHeight = currentY + currentRowMaxHeight + padding.bottom;
                
                // 设置自身高度
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, totalHeight);
            }
            // 如果是 FitMode.Fixed，则完全不修改 rectTransform 的高度，保留Inspector里设置的值
        }
    }
}