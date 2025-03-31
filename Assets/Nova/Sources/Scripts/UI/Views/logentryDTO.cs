using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nova
{
    // ------------------------------
    // DTO：用于导出 DialogueDisplayData 的详细数据
    // ------------------------------
    [Serializable]
    public class DialogueDisplayDataDTO
    {
        public Dictionary<string, string> displayNames;
        public Dictionary<string, string> dialogues;

        public DialogueDisplayDataDTO(DialogueDisplayData data)
        {
            if (data != null)
            {
                displayNames = ConvertDict(data.displayNames);
                dialogues = ConvertDict(data.dialogues);
            }
            else
            {
                displayNames = new Dictionary<string, string>();
                dialogues = new Dictionary<string, string>();
            }
        }

        private Dictionary<string, string> ConvertDict(Dictionary<SystemLanguage, string> dict)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach (var kvp in dict)
            {
                // 将 SystemLanguage 转换为字符串作为 key
                result[kvp.Key.ToString()] = kvp.Value;
            }
            return result;
        }
    }

    // ------------------------------
    // DTO：用于导出 ReachedDialogueData 的详细数据
    // ------------------------------
    [Serializable]
    public class ReachedDialogueDataDTO
    {
        public string nodeName;
        public int dialogueIndex;
        public Dictionary<string, VoiceEntry> voices; // 如果 VoiceEntry 复杂，可进一步转换
        public bool needInterpolate;
        public ulong textHash;

        public ReachedDialogueDataDTO(ReachedDialogueData data)
        {
            if (data != null)
            {
                nodeName = data.nodeName;
                dialogueIndex = data.dialogueIndex;
                voices = new Dictionary<string, VoiceEntry>();
                if (data.voices != null)
                {
                    foreach (var kvp in data.voices)
                    {
                        // 这里直接赋值，如果需要转换 VoiceEntry，请自行修改
                        voices[kvp.Key] = kvp.Value;
                    }
                }
                needInterpolate = data.needInterpolate;
                textHash = data.textHash;
            }
        }
    }

    // ------------------------------
    // DTO：用于导出 LogEntry 的数据
    // ------------------------------
    [Serializable]
    public class LogEntryDTO
    {
        public float height;
        public float prefixHeight;
        public long nodeOffset;
        public long checkpointOffset;
        public string branchChoice; // 新增字段，用于记录分支选择文本
        public ReachedDialogueDataDTO reachedDialogueData;
        public DialogueDisplayDataDTO displayData;

        public LogEntryDTO(LogEntry entry)
        {
            height = entry.height;
            prefixHeight = entry.prefixHeight;
            nodeOffset = entry.nodeOffset;
            checkpointOffset = entry.checkpointOffset;
            branchChoice = entry.branchChoice; // 同步赋值分支选择文本
            reachedDialogueData = entry.dialogueData != null ? new ReachedDialogueDataDTO(entry.dialogueData) : null;
            displayData = entry.displayData != null ? new DialogueDisplayDataDTO(entry.displayData) : null;
        }
    }

    // ------------------------------
    // LogEntry 数据类
    // ------------------------------
    public class LogEntry
    {
        public float height;
        public float prefixHeight;
        public readonly long nodeOffset;
        public readonly long checkpointOffset;
        public readonly ReachedDialogueData dialogueData;
        public readonly DialogueDisplayData displayData;

        // 新增字段：分支选择文本。如果为 null 表示非分支记录
        public string branchChoice;

        public LogEntry(float height, float prefixHeight, long nodeOffset, long checkpointOffset,
            ReachedDialogueData dialogueData, DialogueDisplayData displayData)
        {
            this.height = height;
            this.prefixHeight = prefixHeight;
            this.nodeOffset = nodeOffset;
            this.checkpointOffset = checkpointOffset;
            this.dialogueData = dialogueData;
            this.displayData = displayData;
            this.branchChoice = null;
        }
    }
}
