using Nova;
using TMPro;
using UnityEngine;

public class DeductionUIManager : MonoBehaviour
{
    #region 单例模式

    // 静态实例
    private static DeductionUIManager _instance;

    // 公共访问点
    public static DeductionUIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // 尝试在场景中找到现有的实例
                _instance = FindAnyObjectByType<DeductionUIManager>();

                if (_instance == null)
                {
                    Debug.LogError("DeductionUIManager 实例未找到。请确保场景中存在 DeductionUIManager 的实例。");
                }
            }
            return _instance;
        }
    }

    void instanceFunctionInAwake()
    {
        if (_instance == null)
        {
            _instance = this;
            //DontDestroyOnLoad(gameObject); // 保持跨场景的持久性
        }
        else if (_instance != this)
        {
            Destroy(gameObject); // 如果已经有实例存在，销毁多余的实例
        }
    }
    #endregion
    [SerializeField]
    private TMP_InputField deductionInputField;
    [SerializeField]
    private InspirationUIScript inspirationUIManager;
    [SerializeField]
    private DeductionListUIScript deductionListUIScript;

    private bool isDeductionModeActive = false;
    void Awake()
    {
        instanceFunctionInAwake();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.CheckSerializedFields();
        deductionInputField.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void initialize()
    {
        
    }

    public void ActivateDeductionMode()
    {
        isDeductionModeActive = true;
        inspirationUIManager.InitByList(DeductionManager.Instance.currentLevel.RootInspirations);
        deductionInputField.gameObject.SetActive(true);
    }
    public void DeactivateDeductionMode()
    {
        isDeductionModeActive = false;
        deductionInputField.gameObject.SetActive(false);
    }

    public void DiscoverDeduction(Interrorgation_Deduction deductionData)
    {
        deductionListUIScript.AddDiscoveredDeduction(deductionData);
    }

    public void SelectDeduction(Interrorgation_Deduction deductionData)
    {
        Debug.Log("Selected Deduction: " + deductionData.DeductionText);
        // Handle deduction selection logic here
    }

}
