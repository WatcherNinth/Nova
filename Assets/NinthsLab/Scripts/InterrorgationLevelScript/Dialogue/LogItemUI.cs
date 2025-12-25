using UnityEngine;
using TMPro;
using DialogueSystem; // 引用数据结构

namespace FrontendEngine
{
    public class LogItemUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text contentText;

        public void Setup(DialogueEntry entry)
        {
            // 设置名字
            // 如果是旁白(DisplayName为空)，可以隐藏名字框或显示特定文本
            if (string.IsNullOrEmpty(entry.DisplayName))
            {
                nameText.text = ""; 
                // 此时你可能想调整布局，这里简单处理
            }
            else
            {
                nameText.text = entry.DisplayName;
            }

            // 设置内容
            contentText.text = entry.Content;

            // TODO: 如果 DialogueEntry 里包含语音 AudioClip，可以在这里添加播放按钮
        }
    }
}