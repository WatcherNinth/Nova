using System.Collections.Generic;
using Interrorgation.MidLayer;
using LogicEngine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class ScopeUIScript : MonoBehaviour
{
    [SerializeField] TMP_Text scopeTextPrefab;
    [SerializeField] Transform scopeContainer;
    [SerializeField] GameObject scopeSeparatorPrefab;

    private List<string> _scopeCache;

    private void Awake()
    {
        _scopeCache = new List<string>();
        UIEventDispatcher.OnScopeStackChanged += UpdateScopeUI;
    }

    private void OnDestroy()
    {
        UIEventDispatcher.OnScopeStackChanged -= UpdateScopeUI;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _scopeCache = new List<string>();
        RefreshUI(_scopeCache);
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void UpdateScopeUI(List<string> scopeStack, string actionId)
    {
        RefreshUI(scopeStack);
        UIEventDispatcher.DispatchActionCompleted(actionId);
    }

    private void RefreshUI(List<string> scopeStack)
    {
        if (scopeStack.Count == 0)
        {
            scopeContainer.gameObject.SetActive(false);
            return;
        }
        scopeContainer.gameObject.SetActive(true);
        foreach (Transform child in scopeContainer)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < scopeStack.Count; i++)
        {
            string nodeId = scopeStack[i];
            TMP_Text text = Instantiate(scopeTextPrefab, scopeContainer);
            text.gameObject.SetActive(true);
            text.text = LevelGraphContext.CurrentGraph.nodeLookup[nodeId].Node.Basic.Description;
            if (i < scopeStack.Count - 1)
            {
                GameObject scopeSeparator = Instantiate(scopeSeparatorPrefab, scopeContainer);
                scopeSeparator.gameObject.SetActive(true);
            }
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(scopeContainer.GetComponent<RectTransform>());
        _scopeCache = scopeStack;
    }
}
