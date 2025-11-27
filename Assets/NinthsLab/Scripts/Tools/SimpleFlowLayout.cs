using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LogicEngine.UI
{
    /// <summary>
    /// 简易流式布局组件。
    /// 用于解决一行放不下时自动换行的问题，支持不同宽度的子元素。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SimpleFlowLayout : LayoutGroup
    {
        public float SpacingX = 5f;
        public float SpacingY = 5f;
        public bool ChildForceExpandWidth = false;
        public bool ChildForceExpandHeight = false;

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            CalculateLayout();
        }

        public override void CalculateLayoutInputVertical()
        {
            CalculateLayout();
        }

        public override void SetLayoutHorizontal() { }
        public override void SetLayoutVertical() { }

        private void CalculateLayout()
        {
            rectChildren.Clear();
            var children = new List<RectTransform>();
            for (int i = 0; i < transform.childCount; i++)
            {
                var rect = transform.GetChild(i) as RectTransform;
                if (rect == null || !rect.gameObject.activeInHierarchy) continue;
                children.Add(rect);
            }

            float width = rectTransform.rect.width;
            float currentX = padding.left;
            float currentY = padding.top;
            float rowHeight = 0f;

            foreach (var child in children)
            {
                float childWidth = LayoutUtility.GetPreferredWidth(child);
                float childHeight = LayoutUtility.GetPreferredHeight(child);

                // 如果当前行放不下，换行
                if (currentX + childWidth > width - padding.right && currentX > padding.left)
                {
                    currentX = padding.left;
                    currentY += rowHeight + SpacingY;
                    rowHeight = 0f;
                }

                // 设置子物体位置
                SetChildAlongAxis(child, 0, currentX, childWidth);
                SetChildAlongAxis(child, 1, currentY, childHeight);

                currentX += childWidth + SpacingX;
                rowHeight = Mathf.Max(rowHeight, childHeight);
            }

            // 设置自身高度以适应内容
            float totalHeight = currentY + rowHeight + padding.bottom;
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, totalHeight);
        }
    }
}