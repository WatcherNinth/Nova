using System.Collections.Generic;
using Interrorgation.MidLayer;
using Interrorgation.UI.UIState;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInputFieldUIScript : UIStateController<PlayerInputFieldUIScript.InputFieldState>
{
    public enum InputFieldState
    {
        Hidden,
        Active
    }

    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button submitButton;

    protected override InputFieldState InitialState => InputFieldState.Active;

    protected override Dictionary<InputFieldState, List<InputFieldState>> DefineTransitions()
    {
        return new Dictionary<InputFieldState, List<InputFieldState>>
        {
            { InputFieldState.Hidden, new List<InputFieldState> { InputFieldState.Active } },
            { InputFieldState.Active, new List<InputFieldState> { InputFieldState.Hidden } },
        };
    }

    protected override bool CheckIsVisualActive(InputFieldState state) => state == InputFieldState.Active;

    protected override void SubscribeEvents()
    {
        UIEventDispatcher.OnShowDialogues += HandleShowDialogues;
        DialogueSystem.DialogueEventDispatcher.OnDialogueBatchEnded += HandleDialogueEnd;
    }

    protected override void UnsubscribeEvents()
    {
        UIEventDispatcher.OnShowDialogues -= HandleShowDialogues;
        DialogueSystem.DialogueEventDispatcher.OnDialogueBatchEnded -= HandleDialogueEnd;
    }

    void Start()
    {
        inputField.onSubmit.AddListener(delegate { OnSubmitInput(); });
        submitButton.onClick.AddListener(OnSubmitInput);
    }

    public override void OnStateEnter(InputFieldState state)
    {
        bool visible = state == InputFieldState.Active;
        inputField.gameObject.SetActive(visible);
        submitButton.gameObject.SetActive(visible);
    }

    private void HandleShowDialogues(List<string> dialogues)
    {
        StateMachine.TryTransitionTo(InputFieldState.Hidden);
    }

    private void HandleDialogueEnd()
    {
        StateMachine.TryTransitionTo(InputFieldState.Active);
    }

    public void OnSubmitInput()
    {
        string playerInput = inputField.text;
        if (!string.IsNullOrEmpty(playerInput))
        {
            UIEventDispatcher.DispatchPlayerSubmitInput(playerInput);
            inputField.text = "";
        }
    }
}
