using System.Collections.Generic;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Nova
{
    public class InspirationUIScript : MonoBehaviour
    {
        [SerializeField]
        private GameObject ButtonText;
        [SerializeField]
        private GameObject CombinationText;
        [SerializeField]
        private GameObject QuestionText;
        [SerializeField]
        private Transform Trans_Combination;
        [SerializeField]
        private Transform Trans_Location;
        private List<InspirationDataType> inspirations;
        private InspirationDataType currentParent;
        private Vector2 lastInspirationLocation;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            ButtonText.SetActive(false);
            CombinationText.SetActive(false);
            QuestionText.transform.parent.gameObject.SetActive(false);
            clearCombination();
            foreach (Image image in Trans_Location.GetComponentsInChildren<Image>())
            {
                image.enabled = false;
            }
            Test();
        }

        // Update is called once per frame
        void Update()
        {

        }

        void Test()
        {
            // 假设文件路径为 "Assets/Resources/InterrorgationLevels/DemoLevel/Inspiration_Test"
            InitByList(InspirationDataType.LoadFromFile("InterrorgationLevels/DemoLevel/Inspiration_Test"));
            Debug.Log($"加载到{inspirations.Count}个灵感");
            Debug.Log(JsonConvert.SerializeObject(inspirations, Formatting.Indented));
        }

        public void InitByList(List<InspirationDataType> inspirationData)
        {
            inspirations = inspirationData;
            InitRootInspirations();
        }

        void InitRootInspirations()
        {
            for (int i = 0; i < inspirations.Count; i++)
            {
                GameObject button = createInspirationButton(inspirations[i]);
                button.GetComponent<RectTransform>().anchoredPosition = getAnchorLocationByIndex(i + 1);
            }
        }
        public void OnInspirationClick(InspirationDataType inspiration, GameObject button)
        {
            // move to center by dotween
            button.GetComponent<RectTransform>().DOAnchorPos(getAnchorLocationByIndex(0), 0.5f);
            if (inspiration.IsFinalInspiration())
            {
                return;
            }
            addTextToCombination(inspiration.Text);
        }

        GameObject createInspirationButton(InspirationDataType inspiration)
        {
            var button = Instantiate(ButtonText, Trans_Location);
            button.SetActive(true);
            button.GetComponent<TMP_Text>().text = inspiration.Text;
            button.GetComponent<Button>().onClick.AddListener(() => OnInspirationClick(inspiration, button));
            return button;
        }

        Vector2 getAnchorLocationByIndex(int index)
        {
            string locationName = index.ToString();
            Transform locationMarker = Trans_Location.Find(locationName);

            if (locationMarker != null)
            {
                // 保持按钮在当前层级，只使用位置标记的位置信息
                RectTransform markerRect = locationMarker.GetComponent<RectTransform>();
                return markerRect.anchoredPosition;
            }
            else
            {
                Debug.LogWarning($"找不到位置标记：{locationName}");
                // 可以设置一个默认位置或者使用其他处理方式
                return Vector2.zero;
            }
        }

        void clearCombination()
        {
            for (int i = 0; i < Trans_Combination.childCount; i++)
            {

                Destroy(Trans_Combination.GetChild(i).gameObject);
            }
        }

        void addTextToCombination(string text)
        {
            if (Trans_Combination.childCount > 0)
            {
                Instantiate(CombinationText, Trans_Combination);
                CombinationText.GetComponent<TMP_Text>().text = "X";
            }
            Instantiate(CombinationText, Trans_Combination);
            CombinationText.GetComponent<TMP_Text>().text = text;
        }

        void removeTextFromCombination()
        {
            var count = Trans_Combination.childCount;
            if (count == 0)
            {
                return;
            }
            if (count > 3)
            {
                Destroy(Trans_Combination.GetChild(count - 1).gameObject);
                Destroy(Trans_Combination.GetChild(count - 2).gameObject);
            }
            else
            {
                clearCombination();
            }
        }
    }
}