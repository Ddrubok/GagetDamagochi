using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class GeminiNetwork : MonoBehaviour
{
    private const string API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemma-3-27b-it:generateContent";
    private string apiKey = "";

    // GameManager가 이 함수를 호출해서 대화를 시작함
    public void SendChat(string prompt, string userMessage, System.Action<string> onComplete, System.Action<string> onFail)
    {
        StartCoroutine(RoutineChat(prompt, userMessage, onComplete, onFail));
    }

    // 초기화 (키 로드)
    public void InitKey()
    {
        TextAsset keyFile = Resources.Load<TextAsset>("GeminiKey");
        if (keyFile != null) apiKey = keyFile.text.Trim();
    }

    IEnumerator RoutineChat(string prompt, string msg, System.Action<string> onComplete, System.Action<string> onFail)
    {
        string finalUrl = $"{API_URL}?key={apiKey}";
        GeminiRequest requestData = new GeminiRequest();
        requestData.contents = new Content[] { new Content { parts = new Part[] { new Part { text = prompt + "\n\nUser: " + msg } } } };
        string jsonBody = JsonUtility.ToJson(requestData);

        using (UnityWebRequest request = new UnityWebRequest(finalUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.certificateHandler = new BypassCertificate();

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onFail?.Invoke(request.responseCode == 429 ? "429" : request.error);
            }
            else
            {
                // 파싱 로직도 여기서 처리해서 깔끔한 텍스트만 돌려줌
                string json = request.downloadHandler.text;
                string reply = "해석 실패";
                int start = json.IndexOf("\"text\": \"");
                if (start != -1)
                {
                    start += 9;
                    int end = json.IndexOf("\"", start);
                    if (end > start) reply = json.Substring(start, end - start).Replace("\\n", "\n").Replace("\\\"", "\"");
                }
                onComplete?.Invoke(reply);
            }
        }
    }

    class BypassCertificate : CertificateHandler { protected override bool ValidateCertificate(byte[] certificateData) => true; }
    [System.Serializable] public class GeminiRequest { public Content[] contents; }
    [System.Serializable] public class Content { public Part[] parts; }
    [System.Serializable] public class Part { public string text; }
}