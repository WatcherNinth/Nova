using System.Collections.Generic;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.Events;

namespace Nova
{
    public class InspirationUIScript : MonoBehaviour
    {
        [Header("动画数据")]
        [SerializeField]
        private float AnimationDuration = 0.5f;
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
        [SerializeField]
        private Button button_Back;
        [SerializeField]
        private Button button_Confirm;
        private GameObject currentCenterInspiration = null;
        private List<List<GameObject>> inspirationButtons = new List<List<GameObject>>();
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            ButtonText.SetActive(false);
            CombinationText.SetActive(false);
            QuestionText.transform.parent.gameObject.SetActive(false);
            button_Back.gameObject.SetActive(false);
            button_Confirm.gameObject.SetActive(false);
            clearCombination();
            button_Back.onClick.AddListener(OnBackClick);
            button_Confirm.onClick.AddListener(OnConfirmClick);
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
        }

        public void InitByList(List<InspirationDataType> inspirationData)
        {
            Debug.Log($"加载到{inspirationData.Count}个灵感");
            Debug.Log(JsonConvert.SerializeObject(inspirationData, Formatting.Indented));
            //createLocationTransform();
            inspirationButtons.Add(new List<GameObject>());
            for (int i = 0; i < inspirationData.Count; i++)
            {
                GameObject button = createInspirationButton(inspirationData[i], getAnchorLocationByIndex(i + 1));
                inspirationButtons[getCurrentLayer()].Add(button);
            }
        }
        public void OnInspirationClick(InspirationDataType inspiration, GameObject button)
        {
            button_Back.gameObject.SetActive(true);
            // remove other buttons
            foreach (var otherbutton in inspirationButtons[getCurrentLayer()])
            {
                if (otherbutton == button) continue;
                playButtonFadeOutAnimation(otherbutton, null);
            }
            if (currentCenterInspiration != null)
            {
                playButtonFadeOutAnimation(currentCenterInspiration, null);
            }
            //createLocationTransform();
            //button.transform.SetParent(getLastLocationTransform());
            // move to center by dotween
            playButtonToCenterAnimation(button);
            button.GetComponent<InspirationUIInspirationButtonScript>().isCentered = true;
            currentCenterInspiration = button;
            //add new layer
            inspirationButtons.Add(new List<GameObject>());
            addTextToCombination(inspiration.Text);
            if (inspiration.IsFinalInspiration())
            {
                //display question
                setQuestionText(inspiration.DeductionText);
                button_Confirm.gameObject.SetActive(true);
                return;
            }
            for (int i = 0; i < inspiration.NextInspirations.Count; i++)
            {
                //next inspiration generation
                //TODO: checkunlockstatus
                GameObject nextButton = createInspirationButton(inspiration.NextInspirations[i], getAnchorLocationByIndex(0), inspiration);
                inspirationButtons[getCurrentLayer()].Add(nextButton);
                nextButton.GetComponent<InspirationUIInspirationButtonScript>().SideLocation = getAnchorLocationByIndex(i + 1);
                playButtonBackToSideAnimation(nextButton);
            }
        }

        public void OnBackClick()
        {
            if (getCurrentLayer() == 0) return;
            hideQuestionText();
            button_Confirm.gameObject.SetActive(false);
            // remove currentlayer
            foreach (var button in inspirationButtons[getCurrentLayer()])
            {
                playButtonToCenterAnimation(button);
                playButtonFadeOutAnimation(button, (button) =>
                {
                    Destroy(button);
                });
            }
            inspirationButtons.RemoveAt(getCurrentLayer());
            //handle conbination text
            removeTextFromCombination();
            //restore last layer
            playButtonBackToSideAnimation(currentCenterInspiration);
            foreach (var otherbutton in inspirationButtons[getCurrentLayer()])
            {
                if (otherbutton.GetComponent<InspirationUIInspirationButtonScript>().isCentered) continue;
                playButtonFadeInAnimation(otherbutton);
            }

            if (getCurrentLayer() == 0)
            {
                button_Back.gameObject.SetActive(false);
                currentCenterInspiration = null;
            }
            else
            {
                //get center from last layer and display it
                currentCenterInspiration = inspirationButtons[getCurrentLayer() - 1].Find(x => x.GetComponent<InspirationUIInspirationButtonScript>().isCentered);
                playButtonFadeInAnimation(currentCenterInspiration);
            }
        }
        public void OnConfirmClick()
        {
            
        }

        GameObject createInspirationButton(InspirationDataType inspiration, Vector2 initPos, InspirationDataType parent = null)
        {
            var button = Instantiate(ButtonText, Trans_Location);
            button.SetActive(true);
            button.GetComponent<TMP_Text>().text = inspiration.Text;
            button.GetComponent<Button>().onClick.AddListener(() => OnInspirationClick(inspiration, button));
            button.GetComponent<InspirationUIInspirationButtonScript>().inspirationData = inspiration;
            button.GetComponent<InspirationUIInspirationButtonScript>().parentInspirationData = parent;
            button.GetComponent<RectTransform>().anchoredPosition = initPos;
            if (parent == null)
            {
                // 根节点
                button.GetComponent<InspirationUIInspirationButtonScript>().SideLocation = initPos;
            }
            return button;
        }

        #region Question
        void setQuestionText(string question)
        {
            QuestionText.GetComponent<TMP_Text>().text = question;
            QuestionText.transform.parent.gameObject.SetActive(true);
        }
        void hideQuestionText()
        {
            QuestionText.transform.parent.gameObject.SetActive(false);
        }
        #endregion

        #region Location
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
        int getCurrentLayer()
        {
            return inspirationButtons.Count - 1;
        }
        #endregion
        #region Combination
        void clearCombination()
        {
            for (int i = 0; i < Trans_Combination.childCount; i++)
            {
                Destroy(Trans_Combination.GetChild(i).gameObject);
            }
        }

        void addTextToCombination(string text)
        {
            CombinationText.SetActive(true);
            if (Trans_Combination.childCount > 0)
            {
                CombinationText.GetComponent<TMP_Text>().text = "X";
                Instantiate(CombinationText, Trans_Combination);
            }
            CombinationText.GetComponent<TMP_Text>().text = text;
            Instantiate(CombinationText, Trans_Combination);
            CombinationText.SetActive(false);
            LayoutRebuilder.ForceRebuildLayoutImmediate(Trans_Combination.GetComponent<RectTransform>());
        }

        void removeTextFromCombination()
        {
            var count = Trans_Combination.childCount;
            if (count == 0)
            {
                return;
            }
            if (count >= 3)
            {
                Destroy(Trans_Combination.GetChild(count - 1).gameObject);
                Destroy(Trans_Combination.GetChild(count - 2).gameObject);
            }
            else
            {
                clearCombination();
            }
        }
        #endregion
        #region InspirationAnimation
        void playButtonToCenterAnimation(GameObject button)
        {
            button.GetComponent<Button>().enabled = false;
            button.GetComponent<RectTransform>().DOAnchorPos(getAnchorLocationByIndex(0), AnimationDuration);
        }

        void playButtonBackToSideAnimation(GameObject button)
        {
            var pos = button.GetComponent<InspirationUIInspirationButtonScript>().SideLocation;
            button.GetComponent<RectTransform>().DOAnchorPos(pos, AnimationDuration).OnComplete(() =>
            {
                button.GetComponent<Button>().enabled = true;
            });
            button.GetComponent<InspirationUIInspirationButtonScript>().isCentered = false;
        }
        void playButtonFadeOutAnimation(GameObject button, UnityAction<GameObject> callback)
        {
            button.GetComponent<CanvasGroup>().interactable = false;
            button.GetComponent<CanvasGroup>().DOFade(0, AnimationDuration).OnComplete(() =>
            {
                if (callback == null) return;
                callback(button);
            });

        }
        void playButtonFadeInAnimation(GameObject button)
        {
            button.GetComponent<CanvasGroup>().DOFade(1, AnimationDuration).OnComplete(() =>
            {
                button.GetComponent<CanvasGroup>().interactable = true;
            });
        }
        #endregion
    }
}