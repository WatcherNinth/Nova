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
    private GameObject DeductionUIPanel;
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
        HideDeductionUI(); // 确保在游戏开始时隐藏UI面板

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
    }
    public void DeactivateDeductionMode()
    {
        isDeductionModeActive = false;
        HideDeductionUI();
    }

    public void DiscoverDeduction(Interrorgation_Deduction deductionData)
    {
        deductionListUIScript.AddDiscoveredDeduction(deductionData);
    }

    public void SelectDeduction(Interrorgation_Deduction deductionData)
    {
        Debug.Log("Selected Deduction: " + deductionData.DeductionText);
        DeductionManager.Instance.SubmitDeduction(deductionData);
    }

    public void ShowDeductionUI()
    {
        DeductionUIPanel.SetActive(true);
    }

    public void HideDeductionUI()
    {
        DeductionUIPanel.SetActive(false);
    }

    public void Debug_GetAllDeduction()
    {
        DeductionManager.Instance.Debug_GetAllDeduction();
    }
}
