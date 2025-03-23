using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.Events;

public class InspirationUIInspirationLayerScript : MonoBehaviour
{
    float AnimationDuration = 0.5f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    Vector2 getAnchorLocationByIndex(int index)
    {
        string locationName = index.ToString();
        Transform locationMarker = transform.Find(locationName);

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
    }
    void playButtonFadeOutAnimation(Transform trans_Loc, UnityAction<Transform> callback)
    {
        trans_Loc.GetComponent<CanvasGroup>().DOFade(0, AnimationDuration).OnComplete(() =>
        {
            callback(trans_Loc);
        });

    }
    void playButtonFadeInAnimation(Transform trans_Loc)
    {
        trans_Loc.gameObject.SetActive(true);
        trans_Loc.GetComponent<CanvasGroup>().DOFade(1, AnimationDuration);
    }
    #endregion
}
