using UnityEngine;
using Interrorgation.MidLayer;
using TMPro;
using UnityEngine.UI;

public class PlayerInputFieldUIScript : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField inputField;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 监听输入框的提交事件
        inputField.onSubmit.AddListener(delegate { OnSubmitInput(); });
        GetComponentInChildren<Button>().onClick.AddListener(OnSubmitInput);
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
}
