using UnityEngine;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class AllowNullAttribute : System.Attribute { }

public static class UnityObjectExtensions
{
    /// <summary>
    /// Unity全版本兼容的变量空值检查方法
    /// </summary>
    public static void CheckNull<T>(
        this T obj,
        string variableName = "未知变量",
        [System.Runtime.CompilerServices.CallerMemberName] string callerName = "",
        [System.Runtime.CompilerServices.CallerFilePath] string scriptPath = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0
    ) where T : UnityEngine.Object
    {
        if (obj != null) return;

        if (variableName == "未知变量")
        {
#if UNITY_EDITOR
            variableName = TryGetVariableNameFromSource(scriptPath, lineNumber);
#endif
        }

        // 获取当前堆栈帧信息
        var stackTrace = new StackTrace(true);
        var declaringType = stackTrace.GetFrame(1)?.GetMethod()?.DeclaringType;

        // 生成类型信息
        string typeName = typeof(T).Name;
        string scriptName = System.IO.Path.GetFileNameWithoutExtension(scriptPath);

        // 构建详情报告
        string errorDetails = $"变量检查失败于：{callerName} 方法\n" +
                            $"所属脚本：{scriptName}\n" +
                            $"组件路径：{GetHierarchyPath(obj)}";

        string errorMsg = $"[Null检查] <b>{variableName}</b> 未正确初始化！\n\n" +
                        $"• 变量类型：{typeName}\n" +
                        $"• 位置：{scriptName}：第 {lineNumber} 行 \n\n" +
                        $"<i>{errorDetails}</i>";

        UnityEngine.Debug.LogError(errorMsg, obj);

#if UNITY_EDITOR
        // 编辑器模式下的增强功能
        if (obj is Component c)
        {
            EditorGUIUtility.PingObject(c.gameObject);
        }
#endif
    }

    // 辅助方法：获取对象层级路径（保护处理空引用）
    private static string GetHierarchyPath(Object obj)
    {
        try
        {
            return obj switch
            {
                GameObject go => GetFullPath(go.transform),
                Component c => GetFullPath(c.transform),
                _ => obj?.name ?? "[未被挂载到场景]"
            };
        }
        catch
        {
            return "[引用已被销毁]";
        }
    }

    private static string GetFullPath(Transform current)
    {
        var path = new System.Text.StringBuilder();
        while (current != null)
        {
            if (path.Length > 0) path.Insert(0, '/');
            path.Insert(0, current.name);
            current = current.parent;
        }
        return path.ToString();
    }

    /// <summary>
    /// 自动检测某个组件中所有序列化字段的空引用
    /// </summary>
    public static void AutoCheckSerializedFields(this MonoBehaviour script)
    {
        var fields = script.GetType()
            .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(f => f.GetCustomAttributes(typeof(SerializeField), true).Length > 0
            && typeof(UnityEngine.Object).IsAssignableFrom(f.FieldType));

        foreach (var field in fields)
        {
            CheckFieldWithNullValidation(script, field);
        }
    }

    private static void CheckFieldWithNullValidation(MonoBehaviour script, FieldInfo field)
    {
        var value = field.GetValue(script) as UnityEngine.Object;
        
        if (value == null)
        {
            string scriptName = script.GetType().Name;
            string originMessage = $"未初始化的序列化字段 <b>{field.Name}</b>\n" +
                                  $"组件类型：<color=#00FF00>{scriptName}</color>\n" +
                                  $"对象路径：{GetHierarchyPath(script.transform)}";
            
            // 构造标准null检查调用
            value.CheckNull(
                variableName: field.Name,
                callerName: "AutoCheckSerializedFields",
                scriptPath: GetScriptPath(script),
                lineNumber: 0
            );
        }
    }

    /// <summary>
    /// 新增：在Awake中调用的序列化字段批量检查方法（Editor专用）
    /// </summary>
    public static void CheckSerializedFields(this MonoBehaviour script)
    {
#if UNITY_EDITOR
        var targetType = script.GetType();
        
        // 反射获取所有序列化字段（含私有）
        var fields = targetType.GetFields(
            System.Reflection.BindingFlags.Public | 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance);

        foreach (var field in fields)
        {
            // 跳过没有SerializeField的字段
            if (!field.IsDefined(typeof(SerializeField), true)) continue;

            // 跳过非UnityObject类型的字段    
            if (!typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType)) continue;

            // 跳过标记AllowNull特性的字段
            if (field.IsDefined(typeof(AllowNullAttribute), true)) continue;

            // 执行空值检查
            var value = field.GetValue(script) as UnityEngine.Object;
            value.CheckNull(
                variableName: field.Name,
                callerName: "Awake",
                scriptPath: GetScriptPath(script),
                lineNumber: 0
            );
        }
#endif
    }

    #if UNITY_EDITOR
    private static string TryGetVariableNameFromSource(string scriptPath, int lineNumber)
    {
        try 
        {
            // 源码预处理：适应不同平台路径格式
            var normalizedPath = scriptPath.Replace("\\", "/").Replace(Application.dataPath, "Assets");
            
            // 通过Unity API获取源码文本
            var scriptLines = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(normalizedPath)?.text.Split('\n');
            if (scriptLines == null || lineNumber > scriptLines.Length) return "自动解析失败";

            // 获取目标行代码（行号从1开始，索引从0开始）
            var targetLine = scriptLines[lineNumber - 1].Trim();
            
            // 正则解析方法调用（示例模式："obj.CheckNull()" 或 "obj.CheckNull() // 注释"）
            var regex = new System.Text.RegularExpressions.Regex(
                @"(?<variable>\w+)\.CheckNull\s*\(([^)]*)\)");
            var match = regex.Match(targetLine);
            
            return match.Success ? match.Groups["variable"].Value : "自动解析失败";
        }
        catch 
        {
            return "自动解析异常";
        }
    }
#endif

    private static string GetScriptPath(MonoBehaviour script)
    {
        #if UNITY_EDITOR
        var monoScript = MonoScript.FromMonoBehaviour(script);
        return AssetDatabase.GetAssetPath(monoScript);
        #else
        return "Unknown";
        #endif
    }

    // 获取完整层级路径（复用之前的实现）
    private static string GetHierarchyPath(Transform current)
    {
        var path = new System.Text.StringBuilder();
        while (current != null)
        {
            if (path.Length > 0) path.Insert(0, '/');
            path.Insert(0, current.name);
            current = current.parent;
        }
        return path.ToString();
    }
}
