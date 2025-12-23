namespace FrontendEngine
{
    public class InterrogationDialogueUI : DialogueUIBase
    {
        [SerializeField] private Animator bubbleAnimator; // 漫画气泡动画

        protected override void OnBeforeShowDialogue(DialogueEntry entry)
        {
            // 审讯模式：文字可能是红色的，且气泡会抖动
            bodyText.color = Color.red;
            
            if(bubbleAnimator) bubbleAnimator.SetTrigger("PopUp");
        }
    }
}