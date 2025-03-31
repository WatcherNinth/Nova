using Nova;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeductionListUIDeductionButtonScript : MonoBehaviour
{
    enum e_DeductionType
    {
        Topic,
        Proof,
        Unassigned,
    }
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
        var text = deduction.DeductionText;
        //临时代码，之后要改
        if (getDeductionType(deduction) == e_DeductionType.Topic)
        {
            text = "论点：" + text;
        }
        else
        {
            text = "论据：" + text;
        }
        deductionNameText.text = text;
    }

    public void OnButtonClick()
    {
        DeductionUIManager.Instance.SelectDeduction(DeductionData);
    }

    e_DeductionType getDeductionType(Interrorgation_Deduction deduction)
    {
        switch (deduction)
        {
            case Interrorgation_Topic:
                return e_DeductionType.Topic;
            case Interrorgation_Proof:
                return e_DeductionType.Proof;
            default:
                return e_DeductionType.Unassigned; // 或者抛出异常
        }
    }
}
