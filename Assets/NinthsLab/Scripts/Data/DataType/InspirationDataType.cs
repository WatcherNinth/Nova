using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InspirationDataType
{
    public string Text;
    public List<InspirationDataType> NextInspirations;
    public string DeductionID;
    public string DeductionText;
    public bool NeedUnlock = false;
    public string UnlockFlag = "";
    public bool IsLocked = false;

    public bool IsFinalInspiration()
    {
        return NextInspirations.Count == 0;
    }

    /// <summary>
    /// 从Resources文件夹中加载灵感数据文件
    /// </summary>
    /// <param name="subPath">资源的路径（不含扩展名）</param>
    /// <returns>顶级灵感数据列表</returns>
    public static List<InspirationDataType> LoadFromFile(string subPath)
    {
        // 从Resources加载文本资源
        TextAsset textAsset = Resources.Load<TextAsset>(subPath);
        if (textAsset == null)
        {
            Debug.LogError($"加载灵感文件失败：{subPath}");
            return new List<InspirationDataType>();
        }
        return ParseInspirations(textAsset.text);   
    }
    /// <summary>
    /// 解析灵感数据文本
    /// </summary>
    /// <param name="text">灵感数据文本内容</param>
    /// <returns>顶级灵感数据列表</returns>
    public static List<InspirationDataType> ParseInspirations(string text)
    {
        string[] lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        List<InspirationDataType> rootInspirations = new List<InspirationDataType>();

        // 使用字典跟踪每个缩进级别当前的灵感对象
        Dictionary<int, InspirationDataType> levelToInspiration = new Dictionary<int, InspirationDataType>();

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // 计算当前行的缩进级别
            int currentLevel = CountTabIndents(line);
            string trimmedLine = line.Trim();

            // 解析文本行
            InspirationDataType inspiration = ParseInspirationLine(trimmedLine);

            if (currentLevel == 0)
            {
                // 顶级灵感
                rootInspirations.Add(inspiration);
                levelToInspiration.Clear();
                levelToInspiration[0] = inspiration;
            }
            else
            {
                // 子级灵感，寻找父级
                int parentLevel = currentLevel - 1;
                if (levelToInspiration.TryGetValue(parentLevel, out InspirationDataType parent))
                {
                    if (parent.NextInspirations == null)
                        parent.NextInspirations = new List<InspirationDataType>();

                    parent.NextInspirations.Add(inspiration);
                }
                else
                {
                    Debug.LogWarning($"检测到不一致的缩进：{line}");
                    rootInspirations.Add(inspiration);
                }

                // 更新当前级别的灵感
                levelToInspiration[currentLevel] = inspiration;

                // 清除更深层次的灵感记录
                List<int> deeperLevels = new List<int>();
                foreach (int level in levelToInspiration.Keys)
                {
                    if (level > currentLevel)
                        deeperLevels.Add(level);
                }

                foreach (int level in deeperLevels)
                    levelToInspiration.Remove(level);
            }
        }

        return rootInspirations;
    }

    // 计算Tab缩进数量
    private static int CountTabIndents(string line)
    {
        int indentCount = 0;
        foreach (char c in line)
        {
            if (c == '\t')
                indentCount++;
            else
                break;
        }
        return indentCount;
    }

    // 解析一行文本为InspirationDataType对象
    private static InspirationDataType ParseInspirationLine(string line)
    {
        InspirationDataType inspiration = new InspirationDataType
        {
            NextInspirations = new List<InspirationDataType>()
        };

        // 检查是否需要解锁
        if (line.Contains("*"))
        {
            int unlockFlagStartIndex = line.IndexOf('[');
            int unlockFlagEndIndex = line.IndexOf(']', unlockFlagStartIndex + 1);
            if (unlockFlagStartIndex >= 0 && unlockFlagEndIndex >= 0)
            {
                inspiration.NeedUnlock = true;
                inspiration.IsLocked = true;
                inspiration.UnlockFlag = line.Substring(unlockFlagStartIndex + 1, unlockFlagEndIndex - unlockFlagStartIndex - 1).Trim();
                line = line.Substring(0, unlockFlagStartIndex).Trim() + line.Substring(unlockFlagEndIndex + 1).Trim();
            }
        }

        // 分割文本和问题
        int colonIndexFullWidth = line.IndexOf('：');
        int colonIndexHalfWidth = line.IndexOf(':');
        int colonIndex = colonIndexFullWidth >= 0 && colonIndexHalfWidth >= 0
            ? Math.Min(colonIndexFullWidth, colonIndexHalfWidth)
            : (colonIndexFullWidth >= 0 ? colonIndexFullWidth : colonIndexHalfWidth);

        if (colonIndex >= 0)
        {
            string text = line.Substring(0, colonIndex).Trim();

            // Remove '*' from the end of the text if present
            if (text.EndsWith("*"))
            {
                text = text.Substring(0, text.Length - 1).Trim();
            }

            // 查找并提取方括号中的内容作为 DeductionID
            int deductionIDStartIndex = line.IndexOf('[', colonIndex);
            int deductionIDEndIndex = line.IndexOf(']', deductionIDStartIndex + 1);
            if (deductionIDStartIndex >= 0 && deductionIDEndIndex >= 0)
            {
                inspiration.DeductionID = line.Substring(deductionIDStartIndex + 1, deductionIDEndIndex - deductionIDStartIndex - 1).Trim();
                // 检查 DeductionText 是否存在
                if (deductionIDEndIndex < line.Length - 1)
                {
                    inspiration.DeductionText = line.Substring(deductionIDEndIndex + 1).Trim();
                }
                else
                {
                    inspiration.DeductionText = string.Empty; // DeductionText 缺省
                }
            }
            else
            {
                inspiration.DeductionText = line.Substring(colonIndex + 1).Trim();
            }

            inspiration.Text = text;
        }
        else
        {
            // Remove '*' from the text if present
            if (line.EndsWith("*"))
            {
                line = line.Substring(0, line.Length - 1).Trim();
            }

            inspiration.Text = line.Trim();
        }

        return inspiration;
    }
}