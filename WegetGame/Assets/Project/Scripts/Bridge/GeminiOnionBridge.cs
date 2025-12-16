using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

[System.Serializable]
public class GeminiRequest
{
    public Content[] contents;
    public GenerationConfig generationConfig;
}

[System.Serializable]
public class Content
{
    public string role;
    public Part[] parts;
}

[System.Serializable]
public class Part
{
    public string text;
}

[System.Serializable]
public class GenerationConfig
{
    public int maxOutputTokens = 100; // 답변 길이 제한
    public float temperature = 0.9f;  // 창의성 (0~1)
}

public class GeminiOnionBridge : MonoBehaviour
{
    [Header("설정")]
    public string apiKey = "AIzaSyA1_9XQmMOnKDl80RrdCZxQuzPsB1-vjUk"; // 아까 복사한 키!
    private string apiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent";

    [Header("UI 연결")]
    public Text debugText;
    public Button btnPraise;
    public Button btnScold;

    private int loveScore = 0;
    private const string PACKAGE_NAME = "com.ddrubok.wegetgame";

    // 양파의 성격 설정 (여기를 바꾸면 말투가 바뀝니다!)
    private string systemPrompt =
        "너는 사용자의 스마트폰 바탕화면에 사는 귀여운 '양파 쿵야' 같은 캐릭터야. " +
        "사용자가 하는 말에 대해 한국어로 짧고(20자 이내) 재치 있게 대답해. " +
        "말끝마다 '양!'을 붙여. (예: 고마워양!, 배고파양!) " +
        "사용자가 칭찬하면 문장 맨 앞에 {HAPPY}를 붙이고, " +
        "사용자가 욕하거나 혼내면 문장 맨 앞에 {SAD}를 붙여. " +
        "그 외 일상적인 말은 태그 없이 대답해.";

    // 버튼 1: 칭찬하기
    public void OnClick_Praise()
    {
        StartCoroutine(ChatWithGemini("우리 양파 정말 착하고 귀엽네! 사랑해!"));
    }

    // 버튼 2: 혼내기
    public void OnClick_Scold()
    {
        StartCoroutine(ChatWithGemini("야 이 바보야! 왜 말을 안 들어! 못생겨가지고."));
    }

    // Gemini API와 통신하는 핵심 함수
    IEnumerator ChatWithGemini(string userMessage)
    {
        btnPraise.interactable = false;
        btnScold.interactable = false;
        if (debugText) debugText.text = "양파가 구글 서버에 물어보는 중... 🧅📡";

        // JSON 데이터 생성 (약간 복잡해 보이지만 그냥 복사하면 됩니다)
        string jsonBody = $@"
        {{
            ""contents"": [
                {{
                    ""role"": ""user"",
                    ""parts"": [{{ ""text"": ""{systemPrompt}\n\n사용자: {userMessage}"" }}]
                }}
            ],
            ""generationConfig"": {{
                ""maxOutputTokens"": 100,
                ""temperature"": 1.0
            }}
        }}";

        using (UnityWebRequest request = new UnityWebRequest($"{apiUrl}?key={apiKey}", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                if (debugText) debugText.text = "통신 오류: " + request.error;
            }
            else
            {
                // 응답 받기
                string jsonResponse = request.downloadHandler.text;
                ParseAndApplyResponse(jsonResponse);
            }
        }

        btnPraise.interactable = true;
        btnScold.interactable = true;
    }

    void ParseAndApplyResponse(string json)
    {
        // JSON 파싱 (간단하게 처리)
        // 실제로는 더 정교한 JSON 파서가 좋지만, 여기선 문자열 검색으로 해결합니다.
        string reply = "알 수 없음";

        // Gemini 응답 구조에서 텍스트 추출
        int startIndex = json.IndexOf("\"text\": \"");
        if (startIndex != -1)
        {
            startIndex += 9;
            int endIndex = json.IndexOf("\"", startIndex);
            reply = json.Substring(startIndex, endIndex - startIndex);
            // 줄바꿈 문자(\n) 처리
            reply = reply.Replace("\\n", "\n").Replace("\\\"", "\"");
        }

        // 감정 분석 태그 처리
        string state = "NORMAL";
        if (reply.Contains("{HAPPY}"))
        {
            state = "HAPPY";
            loveScore += 10;
            reply = reply.Replace("{HAPPY}", "").Trim();
        }
        else if (reply.Contains("{SAD}"))
        {
            state = "SAD";
            loveScore -= 10;
            reply = reply.Replace("{SAD}", "").Trim();
        }

        // 점수 제한
        loveScore = Mathf.Clamp(loveScore, -100, 100);

        // 위젯 업데이트 전송
        UpdateWidget(state, reply, loveScore);
    }

    // (기존과 동일) 위젯 전송 함수
    public void UpdateWidget(string state, string message, int score)
    {
        string jsonString = JsonUtility.ToJson(new WidgetData { state = state, message = message, score = score });
        if (debugText) debugText.text = message;

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject context = currentActivity.Call<AndroidJavaObject>("getApplicationContext"))
            using (AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent"))
            {
                intent.Call<AndroidJavaObject>("setClassName", PACKAGE_NAME, PACKAGE_NAME + ".TinyCapsuleWidget");
                intent.Call<AndroidJavaObject>("setAction", PACKAGE_NAME + ".ACTION_WIDGET_UPDATE");
                intent.Call<AndroidJavaObject>("putExtra", "EXTRA_DATA_JSON", jsonString);
                context.Call("sendBroadcast", intent);
            }
        }
        catch (System.Exception e) { if(debugText) debugText.text = "Error: " + e.Message; }
#endif
    }

    [System.Serializable]
    class WidgetData { public string state; public string message; public int score; }
}