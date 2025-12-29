using UnityEngine;
using System.Collections.Generic;
using System.Text;
using FrontendEngine.Data; // 引用 NovaTransformData

namespace FrontendEngine.Logic
{
    // =========================================================
    // 1. 配置项定义
    // =========================================================
    [System.Serializable]
    public class StagePositionDefinition
    {
        [Tooltip("剧本中调用的键值，如 'left', 'center', 'pos_1'")]
        public string Key;

        [Tooltip("场景中的锚点物体。如果赋值，将自动读取其坐标(RectTransform或Transform)作为基准。")]
        public Transform TargetAnchor;

        [Header("Override Settings")]
        [Tooltip("勾选后，将使用下方的数值覆盖锚点的数值（或者在没有锚点时直接使用该数值）。")]
        public bool OverrideX; public float X;
        public bool OverrideY; public float Y;
        public bool OverrideZ; public float Z;
        
        public bool OverrideScale; public Vector3 Scale = Vector3.one;
        public bool OverrideAngle; public Vector3 Angle;
    }

    // =========================================================
    // 2. 核心管理器
    // =========================================================
    public class StageLayoutManager : MonoBehaviour, IStageLayoutProvider
    {
        [Header("Position Configs")]
        [SerializeField]
        private List<StagePositionDefinition> definitions = new List<StagePositionDefinition>();

        // 运行时查找表
        private Dictionary<string, StagePositionDefinition> _lookupTable;

        private void Awake()
        {
            InitializeLookup();
        }

        private void InitializeLookup()
        {
            _lookupTable = new Dictionary<string, StagePositionDefinition>();
            foreach (var def in definitions)
            {
                if (!string.IsNullOrEmpty(def.Key))
                {
                    if (!_lookupTable.ContainsKey(def.Key))
                    {
                        _lookupTable.Add(def.Key, def);
                    }
                    else
                    {
                        Debug.LogWarning($"[StageLayoutManager] 检测到重复的 Key: {def.Key}，后续定义将被忽略。");
                    }
                }
            }
        }

        // =========================================================
        // 接口实现：ResolvePosition
        // =========================================================
        public NovaTransformData ResolvePosition(string posRaw)
        {
            if (string.IsNullOrEmpty(posRaw)) return null;

            string cleanedKey = posRaw.Trim();

            // 1. 尝试作为 Key 在配置表中查找
            if (_lookupTable.TryGetValue(cleanedKey, out var def))
            {
                return ConvertDefinitionToData(def);
            }

            // 2. 尝试解析为 Lua Table 字符串 "{...}"
            if (cleanedKey.StartsWith("{"))
            {
                return ParseComplexTableString(cleanedKey);
            }

            Debug.LogWarning($"[StageLayoutManager] 未找到坐标定义，且无法解析: {posRaw}");
            return null;
        }

        // =========================================================
        // 内部逻辑：将配置转换为数据
        // =========================================================
        private NovaTransformData ConvertDefinitionToData(StagePositionDefinition def)
        {
            NovaTransformData result = new NovaTransformData();

            // --- A. 从锚点读取基准值 ---
            if (def.TargetAnchor != null)
            {
                // 优先读取 RectTransform (UI 坐标系)
                RectTransform rt = def.TargetAnchor.GetComponent<RectTransform>();
                if (rt != null)
                {
                    result.X = rt.anchoredPosition.x;
                    result.Y = rt.anchoredPosition.y;
                }
                else
                {
                    result.X = def.TargetAnchor.localPosition.x;
                    result.Y = def.TargetAnchor.localPosition.y;
                }

                result.Z = def.TargetAnchor.localPosition.z;
                result.Scale = def.TargetAnchor.localScale;
                result.Angle = def.TargetAnchor.localEulerAngles;
            }

            // --- B. 应用 Overrides (覆盖) ---
            if (def.OverrideX) result.X = def.X;
            if (def.OverrideY) result.Y = def.Y;
            if (def.OverrideZ) result.Z = def.Z;
            
            if (def.OverrideScale) result.Scale = def.Scale;
            if (def.OverrideAngle) result.Angle = def.Angle;

            return result;
        }

        // =========================================================
        // 核心算法：嵌套 Table 字符串解析
        // 格式: {x, y, scale, z, angle}
        // scale/angle 可以是单值，也可以是 {x,y,z}
        // =========================================================
        private NovaTransformData ParseComplexTableString(string raw)
        {
            // 1. 去除最外层的大括号和空白
            string content = raw.Trim();
            if (content.StartsWith("{") && content.EndsWith("}"))
            {
                content = content.Substring(1, content.Length - 2);
            }

            // 2. 智能分割 (处理嵌套逗号)
            List<string> parts = SplitTopLevelParams(content);
            var data = new NovaTransformData();

            // 3. 按索引映射
            // Index 0: X
            data.X = ParseFloatSafe(GetPart(parts, 0));
            
            // Index 1: Y
            data.Y = ParseFloatSafe(GetPart(parts, 1));
            
            // Index 2: Scale (特殊: 可能是单值或Table)
            string scaleRaw = GetPart(parts, 2);
            if (!string.IsNullOrEmpty(scaleRaw))
            {
                // 如果是单值，返回 (v,v,v)；如果是Table，返回 (x,y,z)
                // 默认 Scale 为 1
                data.Scale = ParseVector3Safe(scaleRaw, Vector3.one, isScalarUniform: true);
            }

            // Index 3: Z
            data.Z = ParseFloatSafe(GetPart(parts, 3));

            // Index 4: Angle (特殊: 可能是单值或Table)
            string angleRaw = GetPart(parts, 4);
            if (!string.IsNullOrEmpty(angleRaw))
            {
                // 如果是单值，视为 Z 轴旋转 (v=0,0,val)
                // 默认 Angle 为 0
                data.Angle = ParseVector3Safe(angleRaw, Vector3.zero, isScalarUniform: false);
            }

            return data;
        }

        /// <summary>
        /// 智能分割参数，忽略嵌套大括号内的逗号
        /// </summary>
        private List<string> SplitTopLevelParams(string input)
        {
            List<string> result = new List<string>();
            int depth = 0;
            StringBuilder currentChunk = new StringBuilder();

            foreach (char c in input)
            {
                if (c == '{') depth++;
                else if (c == '}') depth--;

                // 只有当深度为0时，逗号才算作分隔符
                if (c == ',' && depth == 0)
                {
                    result.Add(currentChunk.ToString().Trim());
                    currentChunk.Clear();
                }
                else
                {
                    currentChunk.Append(c);
                }
            }
            if (currentChunk.Length > 0) result.Add(currentChunk.ToString().Trim());

            return result;
        }

        private string GetPart(List<string> parts, int index)
        {
            if (index >= 0 && index < parts.Count) return parts[index];
            return null;
        }

        private float? ParseFloatSafe(string raw)
        {
            if (string.IsNullOrEmpty(raw) || raw == "nil") return null;
            if (float.TryParse(raw, out float v)) return v;
            return null;
        }

        /// <summary>
        /// 解析 Vector3，支持 "{x,y,z}" 或 "scalar"
        /// </summary>
        /// <param name="isScalarUniform">如果为true，单值输入 "2" 会变成 (2,2,2)；如果为false，单值输入 "90" 会变成 (0,0,90) [用于2D旋转]</param>
        private Vector3? ParseVector3Safe(string raw, Vector3 defaultIfEmpty, bool isScalarUniform)
        {
            if (string.IsNullOrEmpty(raw) || raw == "nil") return null;

            // 情况 A: 嵌套结构 "{x, y, z}"
            if (raw.StartsWith("{"))
            {
                string inner = raw.Trim().Trim('{', '}');
                string[] nums = inner.Split(',');
                
                float x = nums.Length > 0 && float.TryParse(nums[0], out float vx) ? vx : defaultIfEmpty.x;
                float y = nums.Length > 1 && float.TryParse(nums[1], out float vy) ? vy : defaultIfEmpty.y;
                float z = nums.Length > 2 && float.TryParse(nums[2], out float vz) ? vz : defaultIfEmpty.z;
                
                return new Vector3(x, y, z);
            }

            // 情况 B: 单一数值 "1.5"
            if (float.TryParse(raw, out float scalar))
            {
                if (isScalarUniform)
                {
                    // 用于 Scale: 统一缩放
                    return new Vector3(scalar, scalar, scalar); // 假设 Z 也缩放，或者设为 1
                }
                else
                {
                    // 用于 Angle: Z轴旋转 (2D 常用)
                    return new Vector3(0, 0, scalar);
                }
            }

            return null;
        }
    }
}