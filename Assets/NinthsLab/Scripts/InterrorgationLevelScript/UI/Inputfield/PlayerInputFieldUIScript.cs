using UnityEngine;
using Interrorgation.MidLayer;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class PlayerInputFieldUIScript : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField inputField;

    [SerializeField]
    private Button submitButton;

    void OnEnable()
    {
        UIEventDispatcher.OnShowDialogues += HandleShowDialogues;
        DialogueSystem.DialogueEventDispatcher.OnDialogueBatchEnded += HandleDialogueEnd;
    }

    void OnDisable()
    {
        UIEventDispatcher.OnShowDialogues -= HandleShowDialogues;
        DialogueSystem.DialogueEventDispatcher.OnDialogueBatchEnded -= HandleDialogueEnd;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 监听输入框的提交事件
        inputField.onSubmit.AddListener(delegate { OnSubmitInput(); });
        submitButton.onClick.AddListener(OnSubmitInput);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnSubmitInput()
    {
        string playerInput = inputField.text;
        if (!string.IsNullOrEmpty(playerInput))
        {
            // 向外部发送事件，告知玩家提交了输入
            UIEventDispatcher.DispatchPlayerSubmitInput(playerInput);
            inputField.text = ""; // 提交后清空输入框
        }
    }

    void HandleShowDialogues(List<string> dialogues)
    {
        // 当显示对话时，隐藏输入框
        inputField.gameObject.SetActive(false);
        submitButton.gameObject.SetActive(false);
    }

    void HandleDialogueEnd()
    {
        // 当对话批次结束时，显示输入框
        inputField.gameObject.SetActive(true);
        submitButton.gameObject.SetActive(true);
    }
}
