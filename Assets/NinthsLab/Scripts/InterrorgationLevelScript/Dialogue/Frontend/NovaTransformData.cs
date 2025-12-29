using UnityEngine;
using System;

namespace FrontendEngine.Data
{
    /// <summary>
    /// 运行时传输用的坐标数据包
    /// 对应 NovaScript 中的 {x, y, scale, z, angle} 结构
    /// 字段为 null 表示“保持不变”或“未定义”
    /// </summary>
    [Serializable]
    public class NovaTransformData
    {
        // 位置分量
        public float? X;
        public float? Y;
        public float? Z;
        
        // 缩放 (支持统一缩放或分量缩放)
        // 存储为 Vector3?，解析器会将单数值转换为 Vector3(v,v,v)
        public Vector3? Scale; 
        
        // 旋转 (支持 Z轴旋转或 3D 旋转)
        // 存储为 Vector3?，解析器会将单数值转换为 Vector3(0,0,v)
        public Vector3? Angle; 

        // =========================================================
        // 构造函数
        // =========================================================

        /// <summary>
        /// 默认构造
        /// </summary>
        public NovaTransformData() { }

        /// <summary>
        /// 拷贝构造函数
        /// </summary>
        public NovaTransformData(NovaTransformData other)
        {
            if (other == null) return;
            X = other.X; 
            Y = other.Y; 
            Z = other.Z;
            Scale = other.Scale; 
            Angle = other.Angle;
        }

        // =========================================================
        // 逻辑方法
        // =========================================================

        /// <summary>
        /// 合并逻辑：将 other 中的非空值覆盖到当前对象上
        /// (用于实现 StagePositionDefinition 中的 Override 逻辑)
        /// </summary>
        /// <param name="other">拥有更高优先级的数据</param>
        public void OverrideWith(NovaTransformData other)
        {
            if (other == null) return;

            if (other.X.HasValue) X = other.X;
            if (other.Y.HasValue) Y = other.Y;
            if (other.Z.HasValue) Z = other.Z;
            
            if (other.Scale.HasValue) Scale = other.Scale;
            if (other.Angle.HasValue) Angle = other.Angle;
        }
        
        public override string ToString()
        {
            string s_x = X.HasValue ? X.Value.ToString("F1") : "_";
            string s_y = Y.HasValue ? Y.Value.ToString("F1") : "_";
            string s_z = Z.HasValue ? Z.Value.ToString("F1") : "_";
            string s_scale = Scale.HasValue ? Scale.Value.ToString() : "_";
            string s_angle = Angle.HasValue ? Angle.Value.ToString() : "_";

            return $"Pos:({s_x}, {s_y}, {s_z}) Scale:{s_scale} Angle:{s_angle}";
        }
    }
}