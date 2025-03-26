using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class TransformExtensions
{
    /// <summary>
    /// 安全销毁所有子物体（支持编辑模式和运行时）
    /// </summary>
    /// <param name="parent">父物体Transform</param>
    /// <param name="immediate">是否立即销毁（默认为自动判断）</param>
    public static void DestroyAllChildrens(this Transform parent, bool immediate = false)
    {
        // 使用逆序循环避免修改集合时的索引问题
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            var child = parent.GetChild(i);
            
#if UNITY_EDITOR
            if (!Application.isPlaying || immediate)
            {
                // 编辑模式下或强制立即销毁时使用DestroyImmediate
                Object.DestroyImmediate(child.gameObject);
            }
            else
            {
                Object.Destroy(child.gameObject);
            }
#else
            // 正式构建中只需使用普通Destroy
            Object.Destroy(child.gameObject);
#endif
        }
    }
}