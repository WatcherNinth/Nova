using UnityEngine;
using TMPro;
using System.Collections;
using DialogueSystem;

namespace FrontendEngine
{
    public abstract class DialogueUIBase : MonoBehaviour
    {
        [Header("Base References")]
        [SerializeField] protected TMP_Text nameText;
        [SerializeField] protected TMP_Text bodyText;
        [SerializeField] protected float typeSpeed = 0.05f;

        protected bool isTyping = false;
        protected string currentFullText = "";

        // --- 1. 统一的生命周期 ---
        protected virtual void OnEnable()
        {
            DialogueEventDispatcher.OnShowDialogue += HandleShowDialogue;
        }

        protected virtual void OnDisable()
        {
            DialogueEventDispatcher.OnShowDialogue -= HandleShowDialogue;
        }

        // --- 2. 核心逻辑 (不要在子类重写这个，除非特殊情况) ---
        private void HandleShowDialogue(DialogueEntry entry)
        {
            currentFullText = entry.Content;
            
            // 调用子类的视觉设置（比如换皮肤、变颜色）
            OnBeforeShowDialogue(entry);

            // 设置名字
            if(nameText) nameText.text = entry.DisplayName;
            
            // 开始打字
            if(bodyText) 
            {
                StopAllCoroutines();
                StartCoroutine(TypewriterRoutine(currentFullText));
            }
        }

        // --- 3. 抽象方法 (子类必须实现或重写的视觉逻辑) ---
        
        // 子类可以在这里播放弹窗动画、切换背景板等
        protected virtual void OnBeforeShowDialogue(DialogueEntry entry) {}

        // 子类可以重写打字机结束后的表现（比如显示一个小箭头）
        protected virtual void OnTypingComplete() {}

        // --- 4. 点击处理 (供 UI 按钮调用) ---
        public void OnClickContainer()
        {
            if (isTyping)
            {
                // 瞬间显示全
                StopAllCoroutines();
                bodyText.text = currentFullText;
                isTyping = false;
                OnTypingComplete();
            }
            else
            {
                // 请求下一句
                DialogueEventDispatcher.DispatchRequestNext();
            }
        }

        // 打字机协程 (基类处理)
        private IEnumerator TypewriterRoutine(string text)
        {
            isTyping = true;
            bodyText.text = "";
            foreach (char letter in text.ToCharArray())
            {
                bodyText.text += letter;
                yield return new WaitForSeconds(typeSpeed);
            }
            isTyping = false;
            OnTypingComplete();
        }
    }
}