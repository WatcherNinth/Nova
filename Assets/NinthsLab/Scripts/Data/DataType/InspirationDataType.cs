using System;
using System.Collections.Generic;
using UnityEngine;
namespace Nova
{
    public class InspirationDataType
    {
        public string Text;
        public List<InspirationDataType> NextInspirations;
        public string QuestionText;
        public bool NeedUnlock = false;
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

            string[] lines = textAsset.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
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
            
            // 分割文本和问题
            string[] parts;
            if (line.Contains("："))
                parts = line.Split(new[] { '：' }, 2);
            else
                parts = line.Split(new[] { ':' }, 2);
            
            if (parts.Length >= 1)
            {
                string text = parts[0].Trim();
                // 检查文本中是否有*标记，表示需要解锁
                if (text.EndsWith("*"))
                {
                    inspiration.NeedUnlock = true;
                    text = text.Substring(0, text.Length - 1).Trim();
                }
                inspiration.Text = text;
            }
            
            if (parts.Length >= 2)
                inspiration.QuestionText = parts[1].Trim();
            
            return inspiration;
        }
    }
}
