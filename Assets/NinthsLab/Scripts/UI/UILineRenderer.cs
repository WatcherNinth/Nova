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

        [Header("Materials")]
        [SerializeField] private Material _normalMaterial;
        [SerializeField] private Material _highlightMaterial;

        private Material _normalMaterialInstance;
        private Material _highlightMaterialInstance;
        private float _lineLengthCache;

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

        public Material normalMaterial
        {
            get => _normalMaterial;
            set
            {
                _normalMaterial = value;
                if (_normalMaterial != null)
                {
                    if (_normalMaterialInstance != null)
                        Destroy(_normalMaterialInstance);
                    _normalMaterialInstance = new Material(_normalMaterial);
                }
                else
                {
                    _normalMaterialInstance = null;
                }
                // 仅在非悬停状态同步应用到渲染
                if (!_isHovering)
                    material = _normalMaterialInstance ?? _normalMaterial;
            }
        }

        public Material highlightMaterial
        {
            get => _highlightMaterial;
            set
            {
                _highlightMaterial = value;
                if (_highlightMaterial != null)
                {
                    if (_highlightMaterialInstance != null)
                        Destroy(_highlightMaterialInstance);
                    _highlightMaterialInstance = new Material(_highlightMaterial);
                }
                else
                {
                    _highlightMaterialInstance = null;
                }
                // 仅在悬停状态同步应用到渲染
                if (_isHovering)
                    material = _highlightMaterialInstance ?? _highlightMaterial;
            }
        }

        public float lineLength => _lineLengthCache;

        protected override void Awake()
        {
            base.Awake();
            _originalColor = color;

            if (_normalMaterial != null)
                _normalMaterialInstance = new Material(_normalMaterial);
            if (_highlightMaterial != null)
                _highlightMaterialInstance = new Material(_highlightMaterial);

            material = _normalMaterialInstance ?? _normalMaterial;

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

            _lineLengthCache = Vector2.Distance(start, end);

            Vector2 v1 = start + halfWidth;
            Vector2 v2 = end + halfWidth;
            Vector2 v3 = end - halfWidth;
            Vector2 v4 = start - halfWidth;

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;

            vertex.position = v1;
            vertex.uv0 = new Vector2(0, 1);
            vh.AddVert(vertex);

            vertex.position = v2;
            vertex.uv0 = new Vector2(1, 1);
            vh.AddVert(vertex);

            vertex.position = v3;
            vertex.uv0 = new Vector2(1, 0);
            vh.AddVert(vertex);

            vertex.position = v4;
            vertex.uv0 = new Vector2(0, 0);
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
            vertex.uv0 = new Vector2(0, 0.5f);
            vh.AddVert(vertex);

            for (int i = 0; i <= segments; i++)
            {
                float angle = Mathf.PI * 0.5f + Mathf.PI * 2f * i / segments;
                Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                vertex.position = start + offset;
                vertex.uv0 = new Vector2(0, 0.5f + 0.5f * Mathf.Sin(angle));
                vh.AddVert(vertex);
                vh.AddTriangle(startCenterIndex, startCenterIndex + i + 1, startCenterIndex + ((i + 1) % (segments + 1)) + 1);
            }

            int endCenterIndex = vh.currentVertCount;
            vertex.position = end;
            vertex.uv0 = new Vector2(1, 0.5f);
            vh.AddVert(vertex);

            for (int i = 0; i <= segments; i++)
            {
                float angle = -Mathf.PI * 0.5f + Mathf.PI * 2f * i / segments;
                Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                vertex.position = end + offset;
                vertex.uv0 = new Vector2(1, 0.5f + 0.5f * Mathf.Sin(angle));
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
            vertex.uv0 = new Vector2(0, 0);
            vh.AddVert(vertex);
            vertex.position = start + perpendicular * halfWidth - startCapOffset;
            vertex.uv0 = new Vector2(0, 1);
            vh.AddVert(vertex);
            vertex.position = start + perpendicular * halfWidth;
            vertex.uv0 = new Vector2(0, 1);
            vh.AddVert(vertex);
            vertex.position = start - perpendicular * halfWidth;
            vertex.uv0 = new Vector2(0, 0);
            vh.AddVert(vertex);

            vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
            vh.AddTriangle(startIndex, startIndex + 2, startIndex + 3);

            int endIndex = vh.currentVertCount;
            vertex.position = end - perpendicular * halfWidth;
            vertex.uv0 = new Vector2(1, 0);
            vh.AddVert(vertex);
            vertex.position = end + perpendicular * halfWidth;
            vertex.uv0 = new Vector2(1, 1);
            vh.AddVert(vertex);
            vertex.position = end + perpendicular * halfWidth + endCapOffset;
            vertex.uv0 = new Vector2(1, 1);
            vh.AddVert(vertex);
            vertex.position = end - perpendicular * halfWidth + endCapOffset;
            vertex.uv0 = new Vector2(1, 0);
            vh.AddVert(vertex);

            vh.AddTriangle(endIndex, endIndex + 1, endIndex + 2);
            vh.AddTriangle(endIndex, endIndex + 2, endIndex + 3);
        }

        private float _cachedAnimTime;

        private void Update()
        {
            if (_enableDynamicUpdate)
            {
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

            if (_isHovering && _highlightMaterialInstance != null)
            {
                _cachedAnimTime = Time.unscaledTime % 1000f;
                // 先写到实例上，保持同步
                _highlightMaterialInstance.SetFloat("_AnimationTime", _cachedAnimTime);
                // 触发 Canvas 重建，让 GetModifiedMaterial 传播到渲染材质
                SetMaterialDirty();
            }
        }

        /// <summary>
        /// MaskableGraphic 在 Mask/ScrollRect 下会通过 StencilMaterial.Add()
        /// 创建缓存材质副本进行渲染。必须重写此方法将动画参数传播到实际渲染的材质上。
        /// </summary>
        public override Material GetModifiedMaterial(Material baseMaterial)
        {
            Material result = base.GetModifiedMaterial(baseMaterial);
            if (result != null)
            {
                result.SetFloat("_LineLength", _lineLengthCache);
                if (_isHovering)
                {
                    result.SetFloat("_AnimationTime", _cachedAnimTime);
                }
            }
            return result;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_enableHoverEffect) return;

            _isHovering = true;
            material = _highlightMaterialInstance ?? _highlightMaterial;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_enableHoverEffect) return;

            _isHovering = false;
            material = _normalMaterialInstance ?? _normalMaterial;
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

            if (_normalMaterialInstance != null)
                Destroy(_normalMaterialInstance);
            if (_highlightMaterialInstance != null)
                Destroy(_highlightMaterialInstance);
        }
    }
}
