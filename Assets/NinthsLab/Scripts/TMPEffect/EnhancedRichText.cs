using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;
using System;
using System.Text;

/// <summary>
/// 为 TextMeshPro 添加增强富文本效果的组件
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class EnhancedRichText : MonoBehaviour
{
    protected TMP_Text textComponent;
    protected string originalText;
    protected bool needsRefresh = false;
    protected bool isProcessingText = false; // 添加处理标志
    
    // 抖动效果的配置
    [Header("抖动效果设置")]
    [Tooltip("默认抖动强度")]
    [SerializeField] protected float defaultShakeIntensity = 3f;
    [Tooltip("默认抖动频率")]
    [SerializeField] protected float defaultShakeFrequency = 20f;
    protected float defaultShakeSpeed = 10f;
    [Tooltip("使用顿帧抖动效果")]
    [SerializeField] protected bool useStepShake = true;
    [Header("调试设置")]
    [Tooltip("启用调试日志")]
    [SerializeField] protected bool enableDebugLogs = false;
    
    // 存储需要应用特殊效果的字符范围
    protected List<EffectInfo> activeEffects = new List<EffectInfo>();
    
    // 缓存原始顶点位置
    protected Dictionary<int, Vector3[]> originalVertices = new Dictionary<int, Vector3[]>();
    
    protected virtual void Awake()
    {
        textComponent = GetComponent<TMP_Text>();
    }
    
    protected virtual void OnEnable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        originalText = textComponent.text;
        ProcessText();
    }
    
    protected virtual void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
    }
    
    protected virtual void OnTextChanged(UnityEngine.Object obj)
    {
        if (isProcessingText) return; // 如果正在处理文本，跳过事件
        
        if (obj == textComponent && textComponent.text != originalText)
        {
            originalText = textComponent.text;
            needsRefresh = true;
        }
    }
    
    protected virtual void Update()
    {
        if (needsRefresh)
        {
            ProcessText();
            needsRefresh = false;
        }
        
        // 只在帧率合理范围内应用效果，减少内存操作
        if (textComponent.enabled && textComponent.textInfo.characterCount > 0 && activeEffects.Count > 0)
        {
            // 可以考虑降低更新频率，例如每2-3帧更新一次
            if (Time.frameCount % 2 == 0) // 每2帧更新一次
            {
                ApplyEffects();
            }
        }
    }
    
    /// <summary>
    /// 处理文本，查找并提取自定义标签
    /// </summary>
    protected virtual void ProcessText()
    {
        if (textComponent == null)
            return;
            
        isProcessingText = true; // 设置处理标志
        activeEffects.Clear();
        originalVertices.Clear();
        
        string processedText = originalText;
        
        try
        {
            // 处理抖动标签
            processedText = ProcessShakeTag(processedText);
            
            // 这里可以添加更多标签的处理
            // processedText = ProcessCustomTag(processedText);
            
            // 更新文本（不包含自定义标签）
            textComponent.text = processedText;
            
            // 强制更新网格以便在下一帧应用效果
            textComponent.ForceMeshUpdate();
            
            // 立即应用一次效果，以确保效果在第一帧就可见
            if (activeEffects.Count > 0 && textComponent.enabled && textComponent.textInfo.characterCount > 0)
            {
                ApplyEffects();
            }
        }
        finally
        {
            isProcessingText = false; // 确保标志被重置
        }
    }
    
    /// <summary>
    /// 处理抖动标签 <shake=强度,频率>文本</shake>
    /// </summary>
    protected virtual string ProcessShakeTag(string text)
    {
        // 匹配 <shake> 或 <shake=数值> 或 <shake=数值,数值> 标签
        string pattern = @"<shake(=(\d+(\.\d+)?)(,(\d+(\.\d+)?))?)?>([^<]*)</shake>";
        
        StringBuilder processedText = new StringBuilder(text);
        List<TagInfo> tags = new List<TagInfo>();
        
        // 查找所有匹配项，但要从后向前处理，以避免索引变化问题
        MatchCollection matches = Regex.Matches(text, pattern);
        for (int m = matches.Count - 1; m >= 0; m--)
        {
            Match match = matches[m];
            
            if (enableDebugLogs)
                Debug.Log("找到标签: " + match.Groups[0].Value);
                
            string fullTag = match.Groups[0].Value;
            string content = match.Groups[7].Value;
            float intensity = defaultShakeIntensity;
            float frequency = defaultShakeFrequency;
            
            // 解析强度参数（如果存在）
            if (match.Groups[2].Success)
            {
                float.TryParse(match.Groups[2].Value, out intensity);
                intensity = Mathf.Max(0.1f, intensity); // 确保强度有最小值
            }
            
            // 解析频率参数（如果存在）
            if (match.Groups[5].Success)
            {
                float.TryParse(match.Groups[5].Value, out frequency);
                frequency = Mathf.Max(0.1f, frequency); // 确保频率有最小值
            }
            
            // 记录标签信息
            TagInfo tagInfo = new TagInfo
            {
                fullTag = fullTag,
                content = content,
                startIndex = match.Index,
                parameters = new Dictionary<string, float>
                {
                    { "intensity", intensity },
                    { "speed", defaultShakeSpeed },
                    { "frequency", frequency }
                }
            };
            
            tags.Add(tagInfo);
            
            // 移除标签，保留内容（从后向前处理避免索引变化）
            processedText.Remove(match.Index, fullTag.Length);
            processedText.Insert(match.Index, content);
        }
        
        // 处理所有找到的标签
        foreach (TagInfo tag in tags)
        {
            // 添加到活动效果列表
            activeEffects.Add(new EffectInfo
            {
                effectType = EffectType.Shake,
                startIndex = tag.startIndex,
                length = tag.content.Length,
                parameters = tag.parameters
            });
        }
        
        return processedText.ToString();
    }
    
    /// <summary>
    /// 应用所有活动效果
    /// </summary>
    protected virtual void ApplyEffects()
    {
        if (activeEffects.Count == 0)
            return;
            
        // 缓存和重置原始顶点位置
        CacheOriginalVertices();
        
        // 应用每个效果
        foreach (EffectInfo effect in activeEffects)
        {
            switch (effect.effectType)
            {
                case EffectType.Shake:
                    ApplyShakeEffect(effect);
                    break;
                    
                // 在这里添加更多效果类型的处理
            }
        }
        
        // 更新顶点数据以应用变更
        bool meshModified = false;
        for (int i = 0; i < textComponent.textInfo.meshInfo.Length; i++)
        {
            if (textComponent.textInfo.meshInfo[i].mesh != null)
            {
                textComponent.textInfo.meshInfo[i].mesh.vertices = textComponent.textInfo.meshInfo[i].vertices;
                meshModified = true;
            }
        }
        
        // 只在确实修改了网格时进行更新
        if (meshModified)
        {
            textComponent.UpdateVertexData();
        }
    }
    
    /// <summary>
    /// 缓存原始顶点位置
    /// </summary>
    protected virtual void CacheOriginalVertices()
    {
        // 只在第一次或需要时缓存
        if (originalVertices.Count == 0)
        {
            for (int i = 0; i < textComponent.textInfo.meshInfo.Length; i++)
            {
                Vector3[] vertices = textComponent.textInfo.meshInfo[i].vertices;
                if (vertices != null && vertices.Length > 0)
                {
                    Vector3[] copy = new Vector3[vertices.Length];
                    Array.Copy(vertices, copy, vertices.Length);
                    originalVertices[i] = copy;
                }
            }
        }
        else
        {
            // 恢复原始位置
            for (int i = 0; i < textComponent.textInfo.meshInfo.Length; i++)
            {
                if (originalVertices.TryGetValue(i, out Vector3[] original))
                {
                    Vector3[] vertices = textComponent.textInfo.meshInfo[i].vertices;
                    if (vertices != null && vertices.Length == original.Length)
                    {
                        Array.Copy(original, vertices, original.Length);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 应用抖动效果
    /// </summary>
    protected virtual void ApplyShakeEffect(EffectInfo effect)
    {
        float intensity = effect.parameters["intensity"];
        float speed = effect.parameters["speed"];
        float frequency = effect.parameters["frequency"];
        
        int endIndex = effect.startIndex + effect.length;
        
        // 对每个受影响的字符应用抖动
        for (int i = effect.startIndex; i < endIndex; i++)
        {
            // 确保索引有效
            if (i >= textComponent.textInfo.characterCount)
                break;
                
            TMP_CharacterInfo charInfo = textComponent.textInfo.characterInfo[i];
            
            // 跳过不可见字符
            if (!charInfo.isVisible)
                continue;
                
            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;
            
            // 获取字符的顶点数组
            Vector3[] vertices = textComponent.textInfo.meshInfo[materialIndex].vertices;
            
            // 使用顿帧抖动效果
            Vector3 offset;
            
            if (useStepShake)
            {
                // 量化时间，创建顿帧效果
                float timeStep = Mathf.Floor(Time.time * frequency) / frequency;
                
                // 基于字符位置和量化时间生成"随机"偏移
                float charSeed = i * 0.1731f; // 使用质数来增加随机感
                float xSeed = (timeStep * 3.7123f + charSeed) * 93.41f;
                float ySeed = (timeStep * 2.3721f + charSeed) * 87.29f;
                
                // 生成-1到1之间的值
                float xRandom = Mathf.PerlinNoise(xSeed, 0) * 2 - 1;
                float yRandom = Mathf.PerlinNoise(0, ySeed) * 2 - 1;
                
                // 应用抖动强度
                offset = new Vector3(
                    xRandom * intensity,
                    yRandom * intensity,
                    0
                );
            }
            else
            {
                // 原来的平滑抖动效果（用作备选）
                float timeValue = Time.time * speed;
                float xOffset = Mathf.Sin(timeValue + i * 0.95f) * 0.7f * intensity;
                xOffset += Mathf.Sin(timeValue * 1.3f + i * 0.1f) * 0.3f * intensity;
                
                float yOffset = Mathf.Cos(timeValue * 0.9f + i * 0.5f) * 0.6f * intensity;
                yOffset += Mathf.Sin(timeValue * 2.1f + i * 0.7f) * 0.4f * intensity;
                
                offset = new Vector3(
                    xOffset,
                    yOffset,
                    0
                );
            }
            
            // 应用偏移到字符的所有四个顶点
            vertices[vertexIndex + 0] += offset;
            vertices[vertexIndex + 1] += offset;
            vertices[vertexIndex + 2] += offset;
            vertices[vertexIndex + 3] += offset;
        }
    }
    
    /// <summary>
    /// 刷新文本处理
    /// </summary>
    public virtual void RefreshText()
    {
        needsRefresh = true;
    }
    
    /// <summary>
    /// 设置新文本
    /// </summary>
    public virtual void SetText(string text)
    {
        originalText = text;
        needsRefresh = true;
    }
    
    /// <summary>
    /// 效果类型枚举
    /// </summary>
    protected enum EffectType
    {
        Shake,
        // 可在此添加更多效果类型
    }
    
    /// <summary>
    /// 标签信息类
    /// </summary>
    protected class TagInfo
    {
        public string fullTag;       // 完整标签
        public string content;       // 标签内容
        public int startIndex;       // 在原始文本中的起始位置
        public Dictionary<string, float> parameters;  // 标签参数
    }
    
    /// <summary>
    /// 效果信息类
    /// </summary>
    protected class EffectInfo
    {
        public EffectType effectType;  // 效果类型
        public int startIndex;         // 起始字符索引
        public int length;             // 长度
        public Dictionary<string, float> parameters;  // 效果参数
    }

    /// <summary>
    /// 释放组件资源
    /// </summary>
    protected virtual void OnDestroy()
    {
        // 清理缓存的顶点数据
        originalVertices.Clear();
        activeEffects.Clear();
    }
}