using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using DialogueSystem;
using Interrorgation.MidLayer;
using Interrorgation.UI.UIState;
using LogicEngine;
using LogicEngine.LevelLogic;
using Nova;
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
    [SerializeField] GameObject sideProvingPanel;
    [SerializeField] TMP_Text sideProvingNodeText;

    private List<string> _scopeCache;
    private string _currentProvingNodeId;

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
        DialogueEventDispatcher.OnDialogueSourceChanged += OnDialogueSourceChanged;
        DialogueEventDispatcher.OnDialogueBatchEnded += OnDialogueEnded;
    }

    protected override void UnsubscribeEvents()
    {
        UIEventDispatcher.OnScopeStackChanged -= UpdateScopeUI;
        DialogueEventDispatcher.OnDialogueSourceChanged -= OnDialogueSourceChanged;
        DialogueEventDispatcher.OnDialogueBatchEnded -= OnDialogueEnded;
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
        Debug.Log($"scopeUpdate: {scopeStack.Count}");
        if (scopeStack.Count == 0 && !DialogueEventDispatcher.GetIsInDialogue())
        {
            StateMachine.TryTransitionTo(ScopeState.Hidden);
            _scopeCache = scopeStack;
            return;
        }
        StateMachine.TryTransitionTo(ScopeState.Shown);

        clearScopeContainer();

        for (int i = 0; i < scopeStack.Count; i++)
        {
            string nodeId = scopeStack[i];
            addScopeElement(nodeId, IsCurrentProving: false);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(scopeContainer.GetComponent<RectTransform>());
        _scopeCache = scopeStack;
    }
    private void addScopeElement(string nodeId, bool IsCurrentProving = false)
    {
        // 先判断是否需要生成分隔符
        bool needSeparator;
        if (IsCurrentProving)
        {
            needSeparator = _scopeCache != null && _scopeCache.Count != 0;
        }
        else
        {
            needSeparator = _scopeCache != null && scopeContainer.childCount < _scopeCache.Count - 1;
        }

        if (needSeparator)
        {
            GameObject scopeSeparator = Instantiate(scopeSeparatorPrefab, scopeContainer);
            scopeSeparator.gameObject.SetActive(true);
        }

        TMP_Text text = Instantiate(scopeTextPrefab, scopeContainer);
        text.gameObject.SetActive(true);
        text.text = LevelGraphContext.CurrentGraph.nodeLookup[nodeId].Node.Basic.Description;
        if (IsCurrentProving)
        {
            setScopeElementAsProving(text.gameObject);
        }
    }

    private void RefreshUIWithProvingNode(string provingNodeId)
    {
        List<string> currentScope = _scopeCache;
        bool isScopeEmpty = currentScope == null || currentScope.Count == 0;

        // 1. 激活ScopeUI
        StateMachine.TryTransitionTo(ScopeState.Shown);
        scopeContainer.gameObject.SetActive(true);

        // 2. 隐藏sideProvingPanel
        if (sideProvingPanel != null)
        {
            sideProvingPanel.SetActive(false);
        }

        // 3. 根据scope情况判断显示逻辑
        if (isScopeEmpty)
        {
            // 情况A：scope是空的
            clearScopeContainer();
            addScopeElement(provingNodeId, IsCurrentProving: true);
        }
        else
        {
            string topNodeId = currentScope.Last();
            
            // 检查当前证明节点是否在栈顶节点的GeneratedDependencyNodes中和relativenodes（我们认为只要相关就可以显示）,要考虑栈顶就是当前证明节点的情况
            bool isInGeneratedDeps = false;
            if (LevelGraphContext.CurrentGraph.nodeLookup.TryGetValue(topNodeId, out var topNodeData))
            {
                isInGeneratedDeps = topNodeData.Node.Logic.GeneratedDependencyNodes.Contains(provingNodeId) ||
                topNodeData.Node.Logic.GeneratedRelativeNodes.Contains(provingNodeId);
            }
            bool isProvingScopeTop = topNodeId == provingNodeId;

            if (isProvingScopeTop)
            {
                // 情况Scope: 正在证明栈顶（scope连锁证明状态）
                setScopeElementAsProving(scopeContainer.GetChild(scopeContainer.childCount - 1).gameObject);
            }
            else if (isInGeneratedDeps)
            {
                // 情况B：在GeneratedDependencyNodes中，显示节点
                // 添加当前证明节点
                addScopeElement(provingNodeId, IsCurrentProving: true);
            }
            else
            {
                // 情况C：不在GeneratedDependencyNodes中，显示sideProvingPanel
                if (sideProvingNodeText != null &&
                    LevelGraphContext.CurrentGraph.nodeLookup.TryGetValue(provingNodeId, out var node))
                {
                    sideProvingPanel.SetActive(true);
                    sideProvingNodeText.text = node.Node.Basic.Description;
                    LayoutRebuilder.ForceRebuildLayoutImmediate(sideProvingPanel.GetComponent<RectTransform>());
                }
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(scopeContainer.GetComponent<RectTransform>());
    }

    private void OnDialogueSourceChanged(DialogueSource source)
    {
        if (source.OwnerType == DialogueOwnerType.Node && source.DialogueKey == "on_proven")
        {
            _currentProvingNodeId = source.OwnerId;
            DispatchOrBacklog(() => RefreshUIWithProvingNode(source.OwnerId), "");
        }
    }

    private void OnDialogueEnded()
    {
        // 清除当前证明节点
        _currentProvingNodeId = null;

        // 隐藏sideProvingPanel
        if (sideProvingPanel != null)
        {
            sideProvingPanel.SetActive(false);
        }

        // 恢复正常的scope显示
        List<string> currentScope = GameEventDispatcher.GetCurrentScopeStack();
        RefreshUI(_scopeCache);
    }

    private void clearScopeContainer()
    {
        foreach (Transform child in scopeContainer)
        {
            Destroy(child.gameObject);
        }
    }
    
    private void setScopeElementAsProving(GameObject element)
    {
        element.GetComponent<TMP_Text>().color = Color.yellow;
    }
}
