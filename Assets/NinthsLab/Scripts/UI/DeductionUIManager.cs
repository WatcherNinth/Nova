using TMPro;
using UnityEngine;

public class DeductionUIManager : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField deductionInputField;

    private bool isDeductionModeActive = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        initialize();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void initialize()
    {
        deductionInputField.gameObject.SetActive(false);
    }

    public void ActivateDeductionMode()
    {
        isDeductionModeActive = true;
        deductionInputField.gameObject.SetActive(true);
    }
    public void DeactivateDeductionMode()
    {
        isDeductionModeActive = false;
        deductionInputField.gameObject.SetActive(false);
    }
}
