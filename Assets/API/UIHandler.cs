using UnityEngine;
using UnityEngine.UI;

public class UIHandler : MonoBehaviour
{
    [Header("UI Components")]
    public InputField inputField;
    public Text outputText;

    [Header("Dependencies")]
    [SerializeField] private ApiHandler apiHandler;

    private void Start()
    {
        if (apiHandler == null)
            apiHandler = gameObject.GetComponent<ApiHandler>();
    }

    public void OnSearchButtonClick()
    {
        string userInput = inputField.text;
        if (string.IsNullOrEmpty(userInput))
        {
            outputText.text = "请输入查询内容。";
            return;
        }

        apiHandler.Search(userInput, HandleApiResponse);
    }

    private void HandleApiResponse(ApiResponse response, string error)
    {
        if (!string.IsNullOrEmpty(error))
        {
            outputText.text = error;
            return;
        }

        string scoreDisplay = response.score.HasValue ? 
                            response.score.Value.ToString("F2") : 
                            "N/A";

        string result = $"ID: {response.id}\n" +
                    $"Title: {response.title}\n" +
                    $"Score: {scoreDisplay}\n" +
                    $"Snippet: {response.content_snippet}\n" +
                    $"Message: {response.message}";
        
        outputText.text = result;
    }

}