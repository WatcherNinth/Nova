using System.Collections.Generic;
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
        if (scopeStack.Count == 0 && !DialogueEventDispatcher.GetIsInDialogue())
        {
            StateMachine.TryTransitionTo(ScopeState.Hidden);
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
        TMP_Text text = Instantiate(scopeTextPrefab, scopeContainer);
        text.gameObject.SetActive(true);
        text.text = LevelGraphContext.CurrentGraph.nodeLookup[nodeId].Node.Basic.Description;

        // Forward检测：检查是否需要生成separator
        bool needSeparator = false;

        // 如果是当前正在证明的节点，且有scope缓存，则不需要separator（证明结束后会被移除）
        if (IsCurrentProving)
        {
            needSeparator = false;
        }
        else
        {
            // 检查后续是否还有元素
            // 通过计算当前scopeContainer的子元素数量和_scopeCache来判断
            int currentIndex = scopeContainer.childCount - 1;
            if (_scopeCache != null && currentIndex < _scopeCache.Count - 1)
            {
                needSeparator = true;
            }
        }

        if (needSeparator)
        {
            GameObject scopeSeparator = Instantiate(scopeSeparatorPrefab, scopeContainer);
            scopeSeparator.gameObject.SetActive(true);
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
            // 检查当前证明节点是否在栈顶节点的GeneratedDependencyNodes中和relativenodes（我们认为只要相关就可以显示）
            bool isInGeneratedDeps = false;
            if (LevelGraphContext.CurrentGraph.nodeLookup.TryGetValue(topNodeId, out var topNodeData))
            {
                isInGeneratedDeps = topNodeData.Node.Logic.GeneratedDependencyNodes.Contains(provingNodeId) ||
                topNodeData.Node.Logic.GeneratedRelativeNodes.Contains(provingNodeId);
            }

            if (isInGeneratedDeps)
            {
                // 情况B：在GeneratedDependencyNodes中，显示节点
                // 先显示scope里的所有元素
                for (int i = 0; i < currentScope.Count; i++)
                {
                    addScopeElement(currentScope[i], IsCurrentProving: false);
                }
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
        RefreshUI(currentScope);
    }
    
    private void clearScopeContainer()
    {
        foreach (Transform child in scopeContainer)
        {
            Destroy(child.gameObject);
        }
    }
}
