using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using System;


public class ApiHandler : MonoBehaviour
{
    public string apiUrl = "http://127.0.0.1:8000/search";
    public void Search(string query, Action<ApiResponse, string> callback)
    {
        StartCoroutine(CallApi(query, callback));
    }

    private IEnumerator CallApi(string query, Action<ApiResponse, string> callback)
    {
        var requestData = new SearchRequest { query = query };
        string jsonData = JsonConvert.SerializeObject(requestData);
        byte[] postData = Encoding.UTF8.GetBytes(jsonData);

        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST")
        {
            uploadHandler = new UploadHandlerRaw(postData),
            downloadHandler = new DownloadHandlerBuffer()
        };
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            try
            {
                var response = JsonConvert.DeserializeObject<ApiResponse>(
                    request.downloadHandler.text);
                callback?.Invoke(response, null);
            }
            catch (JsonException e)
            {
                callback?.Invoke(null, $"JSON解析错误: {e.Message}");
            }
        }
        else
        {
            callback?.Invoke(null, $"请求错误: {request.error}");
        }
    }
}

// API接口响应数据结构（根据实际接口结构调整）
[System.Serializable]
public class ApiResponse
{
    public string id;
    public string title;
    public string content_snippet;
    public float? score; // 改为可空类型
    public string message;
}

// 请求数据结构
[System.Serializable]
public class SearchRequest
{
    public string query;
}