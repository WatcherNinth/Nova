using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Text;

namespace FrontendEngine.UI
{
    [RequireComponent(typeof(TMP_Text))]
    public class RichTextSelectionHandler : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private TMP_Text _textComponent;
        private string _originalText; 
        private int _startIndex = -1;
        private int _currentIndex = -1;

        private const string MARK_PREFIX = "<mark=#0055FF80>"; 
        private const string MARK_SUFFIX = "</mark>";

        // [新增] 缓存 Canvas 引用，用于判断相机模式
        private Canvas _rootCanvas;

        private void Awake()
        {
            _textComponent = GetComponent<TMP_Text>();
            // 向上查找 Canvas
            _rootCanvas = GetComponentInParent<Canvas>();
            if (_rootCanvas == null) Debug.LogError("[Selection] 找不到父级 Canvas组件！");
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            Debug.Log($"<color=yellow>[Selection] OnPointerDown 触发 at {eventData.position}</color>");

            // 1. 获取点击索引
            int index = GetCharacterIndexAtPosition(eventData.position);
            
            Debug.Log($"[Selection] 点击检测结果 Index: {index}");

            if (index != -1)
            {
                // 初始化原始文本
                if (string.IsNullOrEmpty(_originalText) || !_textComponent.text.Contains(MARK_PREFIX))
                {
                    _originalText = _textComponent.text;
                    Debug.Log($"[Selection] 缓存原始文本 (长度: {_originalText.Length})");
                }
                else
                {
                    // 还原脏数据
                    _textComponent.text = _originalText;
                    Debug.Log("[Selection] 还原脏文本");
                }

                _startIndex = index;
                _currentIndex = index;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_startIndex == -1) return;

            int index = GetCharacterIndexAtPosition(eventData.position);

            // [调试] 如果一直拖拽，不要每帧都打印，只有变化时打印
            if (index != -1 && index != _currentIndex)
            {
                Debug.Log($"[Selection] Drag 更新索引: {index} (原: {_currentIndex})");
                _currentIndex = index;
                UpdateSelectionVisual();
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Debug.Log($"[Selection] OnPointerUp 触发");

            if (_startIndex == -1) return;

            if (_startIndex == _currentIndex)
            {
                Debug.Log("[Selection] 原地点击，清除选中");
                ClearSelection();
            }
            else
            {
                Debug.Log($"[Selection] 选中完成。范围: {_startIndex} -> {_currentIndex}");
            }
        }

        // --- [核心修复] 获取相机逻辑 ---
        private int GetCharacterIndexAtPosition(Vector2 screenPos)
        {
            Camera cam = null;

            // 关键：如果 Canvas 模式不是 Overlay，必须传 Camera，否则检测不到！
            if (_rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                cam = null;
            }
            else
            {
                cam = _rootCanvas.worldCamera;
                if (cam == null) cam = Camera.main; // 兜底
            }

            return TMP_TextUtilities.FindIntersectingCharacter(_textComponent, screenPos, cam, true);
        }

        private void UpdateSelectionVisual()
        {
            if (string.IsNullOrEmpty(_originalText)) return;

            int start = Mathf.Min(_startIndex, _currentIndex);
            int end = Mathf.Max(_startIndex, _currentIndex);

            // 安全检查
            if (start < 0) start = 0;
            if (end >= _originalText.Length) end = _originalText.Length - 1;

            // Debug.Log($"[Selection] 重绘高亮: {start} - {end}");

            StringBuilder sb = new StringBuilder();
            
            // 头部
            sb.Append(_originalText.Substring(0, start));
            // 标记头
            sb.Append(MARK_PREFIX);
            // 选中内容
            int length = end - start + 1;
            sb.Append(_originalText.Substring(start, length));
            // 标记尾
            sb.Append(MARK_SUFFIX);
            // 尾部
            if (end + 1 < _originalText.Length)
            {
                sb.Append(_originalText.Substring(end + 1));
            }

            _textComponent.text = sb.ToString();
            
            // 强制刷新 Mesh，防止视觉延迟
            _textComponent.SetAllDirty();
        }

        public void ClearSelection()
        {
            if (!string.IsNullOrEmpty(_originalText))
            {
                _textComponent.text = _originalText;
            }
            _startIndex = -1;
            _currentIndex = -1;
        }

        /// <summary>
        /// [新增] 当外部更新了文本内容时调用此方法，强制清除旧缓存
        /// </summary>
        public void ResetCache()
        {
            _originalText = null;
            _startIndex = -1;
            _currentIndex = -1;
            
            // 可选：如果当前正好处于选中状态，可能需要视觉重置，
            // 但既然是刷新整个Log，文本内容本身就已经变了，这里只需清空变量即可。
            Debug.Log("[Selection] 缓存已重置 (文本内容更新)");
        }
    }
}