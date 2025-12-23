// namespace FrontendEngine
// {
//     public class StandardDialogueUI : DialogueUIBase
//     {
//         [SerializeField] private GameObject nextArrowIcon;

//         protected override void OnBeforeShowDialogue(DialogueEntry entry)
//         {
//             // 普通模式：隐藏等待箭头
//             if(nextArrowIcon) nextArrowIcon.SetActive(false);
            
//             // 可以在这里设置普通模式的字体颜色
//             bodyText.color = Color.white;
//         }

//         protected override void OnTypingComplete()
//         {
//             // 打字结束，显示箭头
//             if(nextArrowIcon) nextArrowIcon.SetActive(true);
//         }
//     }
// }