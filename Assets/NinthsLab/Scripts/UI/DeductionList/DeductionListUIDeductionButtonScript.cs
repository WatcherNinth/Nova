using Nova;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeductionListUIDeductionButtonScript : MonoBehaviour
{
    public TMP_Text deductionNameText;
    public Interrorgation_Deduction DeductionData;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnButtonClick);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void InitButton(Interrorgation_Deduction deduction)
    {
        DeductionData = deduction;
        deductionNameText.text = deduction.DeductionText;
    }

    public void OnButtonClick()
    {
        DeductionUIManager.Instance.SelectDeduction(DeductionData);
    }
}
