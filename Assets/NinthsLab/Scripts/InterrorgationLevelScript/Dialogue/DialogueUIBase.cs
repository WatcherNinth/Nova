using System.Collections;
using UnityEngine;
using TMPro;
using DialogueSystem;

namespace FrontendEngine
{
    public abstract class DialogueUIBase : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] protected TMP_Text nameText;
        [SerializeField] protected TMP_Text bodyText;
        
        [Header("Settings")]
        [SerializeField] protected float typeSpeed = 0.05f;

        protected bool isTyping = false;
        protected string fullTargetText = "";

        // =========================================================
        // [修复点 1] 定义虚方法 Awake
        // MonoBehaviour 的 Awake 默认不是 virtual 的，
        // 如果子类想用 override Awake()，父类必须显式定义它。
        // =========================================================
        protected virtual void Awake()
        {
            // 父类可能不需要在 Awake 做事，但必须留着这个坑给子类跳
        }

        protected virtual void OnEnable()
        {
            DialogueEventDispatcher.OnShowDialogue += OnShowDialogue;
        }

        protected virtual void OnDisable()
        {
            DialogueEventDispatcher.OnShowDialogue -= OnShowDialogue;
        }

        // 核心流程
        private void OnShowDialogue(DialogueEntry entry)
        {
            fullTargetText = entry.Content;
            
            // 调用子类的钩子
            OnBeforeDisplay(entry);

            if (nameText) nameText.text = entry.DisplayName;
            if (bodyText)
            {
                StopAllCoroutines();
                StartCoroutine(TypewriterRoutine(fullTargetText));
            }
        }

        public void OnClickContainer()
        {
            if (isTyping)
            {
                StopAllCoroutines();
                bodyText.text = fullTargetText;
                isTyping = false;
                OnTypingComplete();
            }
            else
            {
                DialogueEventDispatcher.DispatchRequestNext();
            }
        }

        private IEnumerator TypewriterRoutine(string text)
        {
            isTyping = true;
            bodyText.text = "";
            foreach (char c in text)
            {
                bodyText.text += c;
                yield return new WaitForSeconds(typeSpeed);
            }
            isTyping = false;
            OnTypingComplete();
        }

        // =========================================================
        // [修复点 2] 确保定义了这些虚方法，且签名与子类一致
        // =========================================================
        
        /// <summary>
        /// 在显示文本之前触发（用于设置颜色、头像、隐藏箭头等）
        /// </summary>
        protected virtual void OnBeforeDisplay(DialogueEntry entry) {}

        /// <summary>
        /// 打字机效果播放完毕后触发（用于显示箭头）
        /// </summary>
        protected virtual void OnTypingComplete() {}
    }
}