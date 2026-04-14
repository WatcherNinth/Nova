using UnityEngine;
using Interrorgation.MidLayer;
using TMPro;

public class PlayerInputFieldUIScript : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField inputField;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

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
