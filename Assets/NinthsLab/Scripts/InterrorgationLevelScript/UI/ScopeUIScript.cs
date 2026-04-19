using System.Collections.Generic;
using DialogueSystem;
using Interrorgation.MidLayer;
using Interrorgation.UI.UIState;
using LogicEngine;
using LogicEngine.LevelLogic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScopeUIScript : UIStateController<ScopeUIScript.ScopeState>
{
    public enum ScopeState
    {
        Hidden,
        Shown
    }

    [SerializeField] TMP_Text scopeTextPrefab;
    [SerializeField] Transform scopeContainer;
    [SerializeField] GameObject scopeSeparatorPrefab;

    private List<string> _scopeCache;

    protected override ScopeState InitialState => ScopeState.Hidden;

    protected override Dictionary<ScopeState, List<ScopeState>> DefineTransitions()
    {
        return new Dictionary<ScopeState, List<ScopeState>>
        {
            { ScopeState.Hidden, new List<ScopeState> { ScopeState.Shown } },
            { ScopeState.Shown,  new List<ScopeState> { ScopeState.Hidden } },
        };
    }


    /// <summary>
    /// Scope显示永远视觉活跃
    /// </summary>
    /// <param name="state"></param>
    /// <returns></returns>
    protected override bool CheckIsVisualActive(ScopeState state) => true;

    protected override void SubscribeEvents()
    {
        UIEventDispatcher.OnScopeStackChanged += UpdateScopeUI;
        UIEventDispatcher.OnNodeStatusChanged += GetCurrentProvingNode;
    }

    protected override void UnsubscribeEvents()
    {
        UIEventDispatcher.OnScopeStackChanged -= UpdateScopeUI;
        UIEventDispatcher.OnNodeStatusChanged -= GetCurrentProvingNode;
    }

    protected override void Awake()
    {
        base.Awake();
        _scopeCache = new List<string>();
    }

    private void Start()
    {
        _scopeCache = new List<string>();
    }

    public override void OnStateEnter(ScopeState state)
    {
        switch (state)
        {
            case ScopeState.Hidden:
                scopeContainer.gameObject.SetActive(false);
                break;
            case ScopeState.Shown:
                scopeContainer.gameObject.SetActive(true);
                break;
        }
    }

    private void UpdateScopeUI(List<string> scopeStack, string actionId)
    {
        DispatchOrBacklog(() => RefreshUI(scopeStack), actionId);
    }

    private void RefreshUI(List<string> scopeStack)
    {
        if (scopeStack.Count == 0 && !DialogueEventDispatcher.GetIsInDialogue())
        {
            StateMachine.TryTransitionTo(ScopeState.Hidden);
            return;
        }
        StateMachine.TryTransitionTo(ScopeState.Shown);

        foreach (Transform child in scopeContainer)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < scopeStack.Count; i++)
        {
            string nodeId = scopeStack[i];
            addScopeElement(nodeId, i < scopeStack.Count - 1);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(scopeContainer.GetComponent<RectTransform>());
        _scopeCache = scopeStack;
    }
    private void addScopeElement(string nodeId, bool needSeparatorBehind, bool IsCurrentProving = false)
    {
        TMP_Text text = Instantiate(scopeTextPrefab, scopeContainer);
        text.gameObject.SetActive(true);
        text.text = LevelGraphContext.CurrentGraph.nodeLookup[nodeId].Node.Basic.Description;
        if (needSeparatorBehind)
        {
            GameObject scopeSeparator = Instantiate(scopeSeparatorPrefab, scopeContainer);
            scopeSeparator.gameObject.SetActive(true);
        }
    }

    public void GetCurrentProvingNode(RuntimeNodeData nodeData, string actionId)
    {
        
    }
}
