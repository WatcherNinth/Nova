using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Interrorgation.UI
{
    public enum LineCapType
    {
        None,
        Round,
        Square
    }

    /// <summary>
    /// UI Line Renderer - A custom Graphic component for drawing lines in Unity UI
    /// </summary>
    public class UILineRenderer : MaskableGraphic, IPointerEnterHandler, IPointerExitHandler, ICanvasRaycastFilter
    {
        [Header("Line Settings")]
        [SerializeField] private Vector2 _startPoint = Vector2.zero;
        [SerializeField] private Vector2 _endPoint = new Vector2(100f, 100f);
        [SerializeField] private float _lineWidth = 2f;
        [SerializeField] private LineCapType _capType = LineCapType.None;

        [Header("Dynamic Follow")]
        [SerializeField] private Transform _targetStartTransform;
        [SerializeField] private Transform _targetEndTransform;
        [SerializeField] private bool _enableDynamicUpdate = false;

        [Header("Hover Effect")]
        [SerializeField] private bool _enableHoverEffect = false;
        [SerializeField] private Color _hoverColor = Color.white;

        private Vector3 _cachedStartWorldPos;
        private Vector3 _cachedEndWorldPos;
        private Color _originalColor;
        private bool _isHovering = false;

        public Vector2 startPoint
        {
            get => _startPoint;
            set
            {
                _startPoint = value;
                SetVerticesDirty();
            }
        }

        public Vector2 endPoint
        {
            get => _endPoint;
            set
            {
                _endPoint = value;
                SetVerticesDirty();
            }
        }

        public float lineWidth
        {
            get => _lineWidth;
            set
            {
                _lineWidth = value;
                SetVerticesDirty();
            }
        }

        public LineCapType capType
        {
            get => _capType;
            set
            {
                _capType = value;
                SetVerticesDirty();
            }
        }

        public Transform targetStartTransform
        {
            get => _targetStartTransform;
            set => _targetStartTransform = value;
        }

        public Transform targetEndTransform
        {
            get => _targetEndTransform;
            set => _targetEndTransform = value;
        }

        public bool enableDynamicUpdate
        {
            get => _enableDynamicUpdate;
            set => _enableDynamicUpdate = value;
        }

        public bool enableHoverEffect
        {
            get => _enableHoverEffect;
            set => _enableHoverEffect = value;
        }

        public Color hoverColor
        {
            get => _hoverColor;
            set => _hoverColor = value;
        }

        protected override void Awake()
        {
            base.Awake();
            _originalColor = color;
            InitializeCachedPositions();
        }

        private void InitializeCachedPositions()
        {
            if (_targetStartTransform != null)
                _cachedStartWorldPos = _targetStartTransform.position;
            else
                _cachedStartWorldPos = transform.TransformPoint(_startPoint);

            if (_targetEndTransform != null)
                _cachedEndWorldPos = _targetEndTransform.position;
            else
                _cachedEndWorldPos = transform.TransformPoint(_endPoint);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Vector2 start = _startPoint;
            Vector2 end = _endPoint;

            if (_targetStartTransform != null)
                start = transform.InverseTransformPoint(_targetStartTransform.position);

            if (_targetEndTransform != null)
                end = transform.InverseTransformPoint(_targetEndTransform.position);

            Vector2 direction = (end - start).normalized;
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            Vector2 halfWidth = perpendicular * (_lineWidth * 0.5f);

            Vector2 v1 = start + halfWidth;
            Vector2 v2 = end + halfWidth;
            Vector2 v3 = end - halfWidth;
            Vector2 v4 = start - halfWidth;

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;

            vertex.position = v1;
            vh.AddVert(vertex);

            vertex.position = v2;
            vh.AddVert(vertex);

            vertex.position = v3;
            vh.AddVert(vertex);

            vertex.position = v4;
            vh.AddVert(vertex);

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(0, 2, 3);

            if (_capType == LineCapType.Round)
            {
                AddRoundCaps(vh, start, end, perpendicular, _lineWidth * 0.5f);
            }
            else if (_capType == LineCapType.Square)
            {
                AddSquareCaps(vh, start, end, direction, perpendicular, _lineWidth * 0.5f);
            }
        }

        private void AddRoundCaps(VertexHelper vh, Vector2 start, Vector2 end, Vector2 perpendicular, float radius)
        {
            int segments = 16;
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;

            int startCenterIndex = vh.currentVertCount;
            vertex.position = start;
            vh.AddVert(vertex);

            for (int i = 0; i <= segments; i++)
            {
                float angle = Mathf.PI * 0.5f + Mathf.PI * 2f * i / segments;
                Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                vertex.position = start + offset;
                vh.AddVert(vertex);
                vh.AddTriangle(startCenterIndex, startCenterIndex + i + 1, startCenterIndex + ((i + 1) % (segments + 1)) + 1);
            }

            int endCenterIndex = vh.currentVertCount;
            vertex.position = end;
            vh.AddVert(vertex);

            for (int i = 0; i <= segments; i++)
            {
                float angle = -Mathf.PI * 0.5f + Mathf.PI * 2f * i / segments;
                Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                vertex.position = end + offset;
                vh.AddVert(vertex);
                vh.AddTriangle(endCenterIndex, endCenterIndex + i + 1, endCenterIndex + ((i + 1) % (segments + 1)) + 1);
            }
        }

        private void AddSquareCaps(VertexHelper vh, Vector2 start, Vector2 end, Vector2 direction, Vector2 perpendicular, float halfWidth)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;

            Vector2 startCapOffset = direction * halfWidth;
            Vector2 endCapOffset = direction * halfWidth;

            int startIndex = vh.currentVertCount;
            vertex.position = start - perpendicular * halfWidth - startCapOffset;
            vh.AddVert(vertex);
            vertex.position = start + perpendicular * halfWidth - startCapOffset;
            vh.AddVert(vertex);
            vertex.position = start + perpendicular * halfWidth;
            vh.AddVert(vertex);
            vertex.position = start - perpendicular * halfWidth;
            vh.AddVert(vertex);

            vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
            vh.AddTriangle(startIndex, startIndex + 2, startIndex + 3);

            int endIndex = vh.currentVertCount;
            vertex.position = end - perpendicular * halfWidth;
            vh.AddVert(vertex);
            vertex.position = end + perpendicular * halfWidth;
            vh.AddVert(vertex);
            vertex.position = end + perpendicular * halfWidth + endCapOffset;
            vh.AddVert(vertex);
            vertex.position = end - perpendicular * halfWidth + endCapOffset;
            vh.AddVert(vertex);

            vh.AddTriangle(endIndex, endIndex + 1, endIndex + 2);
            vh.AddTriangle(endIndex, endIndex + 2, endIndex + 3);
        }

        private void Update()
        {
            if (!_enableDynamicUpdate) return;

            bool needsUpdate = false;

            if (_targetStartTransform != null)
            {
                if (_targetStartTransform.position != _cachedStartWorldPos)
                {
                    _cachedStartWorldPos = _targetStartTransform.position;
                    needsUpdate = true;
                }
            }

            if (_targetEndTransform != null)
            {
                if (_targetEndTransform.position != _cachedEndWorldPos)
                {
                    _cachedEndWorldPos = _targetEndTransform.position;
                    needsUpdate = true;
                }
            }

            if (needsUpdate)
            {
                SetVerticesDirty();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_enableHoverEffect) return;

            _isHovering = true;
            color = _hoverColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_enableHoverEffect) return;

            _isHovering = false;
            color = _originalColor;
        }

        // 重写射线检测，使其只在线条附近触发
        // 去掉 override，实现接口方法
        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            // 基础判定：如果鼠标都不在组件的 RectTransform 矩形区域内，直接返回 false
            // if (!RectTransformUtility.RectangleContainsScreenPoint(rectTransform, sp, eventCamera))
            //     return false;

            // 将屏幕坐标转换为局部坐标
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, sp, eventCamera, out Vector2 localPoint);

            // 获取实际的起点和终点
            Vector2 start = _startPoint;
            Vector2 end = _endPoint;

            if (_targetStartTransform != null)
                start = transform.InverseTransformPoint(_targetStartTransform.position);
            if (_targetEndTransform != null)
                end = transform.InverseTransformPoint(_targetEndTransform.position);

            // 计算点到线段的最短距离
            float distance = DistancePointToLineSegment(localPoint, start, end);

            // 加上一点额外的判定范围（线宽的一半 + 5个像素的防抖容错）
            return distance <= (_lineWidth * 0.5f + 5f);
        }

        // 辅助数学计算：点到线段的最短距离
        private float DistancePointToLineSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            Vector2 ap = point - a;

            float proj = Vector2.Dot(ap, ab);
            float abLenSq = ab.sqrMagnitude;

            if (abLenSq == 0f) return ap.magnitude; // 起点终点重合

            float t = Mathf.Clamp01(proj / abLenSq);
            Vector2 closestPoint = a + t * ab;

            return Vector2.Distance(point, closestPoint);
        }

        public void SetPointsFromWorld(Vector2 worldStart, Vector2 worldEnd)
        {
            _startPoint = transform.InverseTransformPoint(worldStart);
            _endPoint = transform.InverseTransformPoint(worldEnd);
            SetVerticesDirty();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _targetStartTransform = null;
            _targetEndTransform = null;
        }
    }
}
