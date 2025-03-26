using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeductionListUIScript : MonoBehaviour
{
    public GameObject DeductionButtonPrefab;
    public Transform ListTransfrom;
    List<GameObject> discoveredDeductions = new List<GameObject>();

    void Awake()
    {
        this.CheckSerializedFields();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DeductionButtonPrefab.SetActive(false);
        ListTransfrom.DestroyAllChildrens();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void AddDiscoveredDeduction(Interrorgation_Deduction deduction)
    {
        var deductionButton = Instantiate(DeductionButtonPrefab, ListTransfrom);
        deductionButton.SetActive(true);
        deductionButton.GetComponent<DeductionListUIDeductionButtonScript>().InitButton(deduction);
        discoveredDeductions.Add(deductionButton);
    }

    public void AddDiscoveredDeductionList(List<Interrorgation_Deduction> deductionList)
    {
        foreach (var deduction in deductionList)
        {
            AddDiscoveredDeduction(deduction);
        }
    }
}
