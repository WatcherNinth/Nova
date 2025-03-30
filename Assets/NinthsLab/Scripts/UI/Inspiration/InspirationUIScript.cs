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
        /// <summary>
        /// 存储当前关卡的灵感数据
        /// </summary>
        private List<InspirationDataType> inspirationLevelData = new List<InspirationDataType>();
        /// <summary>
        /// 灵感层按钮列表，按层存储
        /// </summary>
        private List<List<GameObject>> inspirationButtons = new List<List<GameObject>>();

        void Awake()
        {
            this.CheckSerializedFields();
        }
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            ButtonText.SetActive(false);
            CombinationText.SetActive(false);
            QuestionText.transform.parent.gameObject.SetActive(false);
            button_Back.gameObject.SetActive(false);
            button_Confirm.gameObject.SetActive(false);
            clearCombinationText();
            button_Back.onClick.AddListener(OnBackClick);
            button_Confirm.onClick.AddListener(OnConfirmClick);
            foreach (Image image in Trans_Location.GetComponentsInChildren<Image>())
            {
                image.enabled = false;
            }
            //Test();
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void InitByList(List<InspirationDataType> inspirationData)
        {
            Debug.Log($"加载到{inspirationData.Count}个灵感");
            Debug.Log(JsonConvert.SerializeObject(inspirationData, Formatting.Indented));
            inspirationLevelData = inspirationData;
            loadRootInspirations();
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
            // add text to combination
            addTextToCombination(inspiration.Text);
            //添加新层，必须放在final inspiration check之前，因为返回的时候一定退一层
            inspirationButtons.Add(new List<GameObject>());
            // check if final inspiration
            if (inspiration.IsFinalInspiration())
            {
                //display question
                setQuestionText(inspiration.DeductionText);
                button_Confirm.gameObject.SetActive(true);
                return;
            }
            //添加新灵感按钮
            for (int i = 0; i < inspiration.NextInspirations.Count; i++)
            {
                //next inspiration generation
                addButtonToCurrentLayer(inspiration.NextInspirations[i], i, inspiration);
            }
            resortButtonIndexInLayer(getCurrentLayer());
            foreach (var newButton in inspirationButtons[getCurrentLayer()])
            {
                playButtonBackToSideAnimation(newButton);
            }
        }

        public void OnBackClick()
        {
            if (getCurrentLayer() == 0) return;
            hideQuestionText();
            button_Confirm.gameObject.SetActive(false);
            // remove currentlayer
            //TODO：这个脚本之后考虑用destroyLayer替代，现在没法让他在动画放完后只执行一次
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
            removeLastTextFromCombination();
            //restore last layer
            playButtonBackToSideAnimation(currentCenterInspiration);
            foreach (var otherbutton in inspirationButtons[getCurrentLayer()])
            {
                if (otherbutton.GetComponent<InspirationUIInspirationButtonScript>().isCentered) continue;
                playButtonFadeInAnimation(otherbutton);
            }
            currentCenterInspiration.GetComponent<InspirationUIInspirationButtonScript>().isCentered = false;

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
            DeductionManager.Instance.DiscoverDeduction(
                currentCenterInspiration.GetComponent<InspirationUIInspirationButtonScript>().inspirationData.DeductionID);
            //handle combination text
            resetInspirations();
        }

        GameObject createInspirationButton(InspirationDataType inspiration, Vector2 initPos, InspirationDataType parent = null)
        {
            if(inspiration.IsLocked)
            {
                return null;
            }
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

        GameObject addButtonToCurrentLayer(InspirationDataType inspiration, int index, InspirationDataType parentIns = null)
        {
            GameObject NewButton = createInspirationButton(inspiration, getAnchorLocationByIndex(0), parentIns);
            if (NewButton == null) return null;
            inspirationButtons[getCurrentLayer()].Add(NewButton);
            NewButton.GetComponent<InspirationUIInspirationButtonScript>().SideLocation = getAnchorLocationByIndex(index + 1);
            return NewButton;
        }

        #region Layer

        void loadRootInspirations()
        {
            inspirationButtons.Add(new List<GameObject>());
            for (int i = 0; i < inspirationLevelData.Count; i++)
            {
                var NewButton = addButtonToCurrentLayer(inspirationLevelData[i], i);
                playButtonBackToSideAnimation(NewButton);
            }
        }

        void resetInspirations()
        {
            Debug.Log("重置灵感");
            var currentlayer = getCurrentLayer();
            for (int i = currentlayer; i >= 0; i--)
            {
                destroyLayer(i); // 销毁指定层
            }
            inspirationButtons.Clear();
            loadRootInspirations(); // 重新加载根灵感
            resetQuestionText(); // 重置问题文本
            clearCombinationText(); // 清除组合文本
            currentCenterInspiration = null; // 重置当前中心灵感
            button_Back.gameObject.SetActive(false); // 隐藏返回按钮
            button_Confirm.gameObject.SetActive(false); // 隐藏确认按钮
        }

        int getCurrentLayer()
        {
            return inspirationButtons.Count - 1;
        }

        void destroyLayer(int layer)
        {
            Debug.Log($"Destroying layer {layer}"); // 添加日志以便调试
            if (layer < 0 || layer >= inspirationButtons.Count)
            {
                Debug.LogError("Invalid layer index");
                return;
            }
            foreach (var button in inspirationButtons[layer])
            {
                Destroy(button);
            }
            inspirationButtons.RemoveAt(layer);
        }

        void resortButtonIndexInLayer(int layerIndex)
        {
            Debug.Log($"Resorting buttons in layer {layerIndex}"); // 添加日志以便调试
            var layer = inspirationButtons[layerIndex];
            for (int i = 0; i < layer.Count; i++)
            {
                var newSideLocation = getAnchorLocationByIndex(i + 1);
                layer[i].GetComponent<InspirationUIInspirationButtonScript>().SideLocation = newSideLocation;
            }
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

        #endregion
        #region Combination
        void clearCombinationText()
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

        void removeLastTextFromCombination()
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
                clearCombinationText();
            }
        }
        #endregion
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

        void resetQuestionText()
        {
            QuestionText.GetComponent<TMP_Text>().text = ""; // 清空文本
            hideQuestionText(); // 隐藏问题文本框
        }
        #endregion
        #region InspirationAnimation
        void playButtonToCenterAnimation(GameObject button)
        {
            var sidepos = button.GetComponent<InspirationUIInspirationButtonScript>().SideLocation;
            button.GetComponent<RectTransform>().anchoredPosition = sidepos;
            button.GetComponent<Button>().enabled = false;
            button.GetComponent<RectTransform>().DOAnchorPos(getAnchorLocationByIndex(0), AnimationDuration);
        }

        void playButtonBackToSideAnimation(GameObject button)
        {
            var pos = button.GetComponent<InspirationUIInspirationButtonScript>().SideLocation;
            button.GetComponent<RectTransform>().anchoredPosition = getAnchorLocationByIndex(0);
            button.GetComponent<RectTransform>().DOAnchorPos(pos, AnimationDuration).OnComplete(() =>
            {
                button.GetComponent<Button>().enabled = true;
            });
            button.GetComponent<InspirationUIInspirationButtonScript>().isCentered = false;
        }
        void playButtonFadeOutAnimation(GameObject button, UnityAction<GameObject> callback)
        {
            button.GetComponent<CanvasGroup>().alpha = 1.0f;
            button.GetComponent<CanvasGroup>().interactable = false;
            button.GetComponent<CanvasGroup>().DOFade(0, AnimationDuration).OnComplete(() =>
            {
                if (callback == null) return;
                callback(button);
            });

        }
        void playButtonFadeInAnimation(GameObject button)
        {
            button.GetComponent<CanvasGroup>().alpha = 0.0f;
            button.GetComponent<CanvasGroup>().DOFade(1, AnimationDuration).OnComplete(() =>
            {
                button.GetComponent<CanvasGroup>().interactable = true;
            });
        }
        #endregion
    }
}