using UnityEngine;
using UnityEngine.UI;

namespace Interrorgation.UI
{
    public class DeductionBoardUIScirpt : MonoBehaviour
    {

        [SerializeField] private GameObject deductionBoardPrefab;
        [SerializeField] private Button toggleButton;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            toggleButton.onClick.AddListener(OnDeducitonBoardToggleButtonClicked);
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void ShowDeductionBoard()
        {
            // 显示推理板 UI
            deductionBoardPrefab.gameObject.SetActive(true);
            // 这里可以根据实际情况实例化推理板预制件，或者激活已经存在的推理板 UI 对象
        }

        public void HideDeductionBoard()
        {
            // 隐藏推理板 UI
            deductionBoardPrefab.gameObject.SetActive(false);
            // 这里可以根据实际情况销毁推理板预制件，或者隐藏已经存在的推理板 UI 对象
        }

        public void OnDeducitonBoardToggleButtonClicked()
        {
            if (deductionBoardPrefab.gameObject.activeSelf)
            {
                HideDeductionBoard();
            }
            else
            {
                ShowDeductionBoard();
            }
        }
    }
}
