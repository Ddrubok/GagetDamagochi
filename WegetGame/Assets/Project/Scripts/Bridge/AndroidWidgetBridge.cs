using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class AndroidWidgetBridge : MonoBehaviour
{
    private string apiKey = "";

    [Header("UI 연결")]
    public Text debugText;
    public Button btnPraise;
    public Button btnScold;

    private const string PACKAGE_NAME = "com.ddrubok.wegetgame";
    private int loveScore = 0;

    // 목록에 있던 'gemini-flash-latest' 사용 (가장 안정적이고 무료 할당량이 많음)
    private const string API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent";

    // 양파 성격 설정
    private string systemPrompt =
        "너는 스마트폰에 사는 귀여운 '양파 쿵야'야. " +
        "한국어로 20자 이내로 짧고 재치 있게 대답해. " +
        "말끝마다 '양!'을 붙여. " +
        "칭찬은 {HAPPY}, 비난은 {SAD} 태그를 앞에 붙여.";

    public void OnClick_Praise() { StartCoroutine(ChatWithGemini("우리 양파 최고야! 사랑해!")); }
    public void OnClick_Scold() { StartCoroutine(ChatWithGemini("야! 썩은 양파! 저리 가!")); }

    IEnumerator ChatWithGemini(string userMessage)
    {
        // 버튼 잠금
        if (btnPraise) btnPraise.interactable = false;
        if (btnScold) btnScold.interactable = false;

        if (debugText) debugText.text = "양파(Exp)가 생각 중... 🧅💭";

        if(apiKey== "")
        {
            LoadApiKey();
        }

        // URL 조립
        string finalUrl = $"{API_URL}?key={apiKey.Trim()}";

        // JSON 데이터
        string jsonBody = "{ \"contents\": [{ \"parts\": [{ \"text\": \"" + systemPrompt + "\\n\\nUser: " + userMessage + "\" }] }] }";

        using (UnityWebRequest request = new UnityWebRequest(finalUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            // SSL 우회 (필수)
            request.certificateHandler = new BypassCertificate();

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                // 429 에러가 뜨면 잠시 기다리라는 안내 표시
                if (request.responseCode == 429)
                {
                    if (debugText) debugText.text = "양파가 너무 바빠요 (30초 후 다시!)";
                    Debug.LogError("API 요청 한도 초과 (429). 잠시 대기 후 시도하세요.");
                }
                else
                {
                    string errorMsg = $"오류: {request.downloadHandler.text}";
                    Debug.LogError(errorMsg);
                    if (debugText) debugText.text = "통신 실패 (콘솔 확인)";
                }
            }
            else
            {
                // 성공!
                ParseAndApplyResponse(request.downloadHandler.text);
            }
        }

        // 버튼 해제
        if (btnPraise) btnPraise.interactable = true;
        if (btnScold) btnScold.interactable = true;
    }

    void LoadApiKey()
    {
        // 1. Resources 폴더의 'GeminiKey.txt' 파일을 읽어옵니다.
        TextAsset keyFile = Resources.Load<TextAsset>("GeminiKey");

        if (keyFile != null)
        {
            apiKey = keyFile.text.Trim();
            Debug.Log("🔑 API 키 로드 성공!");
        }
        else
        {
            Debug.LogError("🚨 'Assets/Resources/GeminiKey.txt' 파일을 찾을 수 없습니다!");
            // (파일을 안 만들었을 때 경고)
        }
    }

    // 결과 해석 및 위젯 업데이트
    void ParseAndApplyResponse(string json)
    {
        string reply = "알 수 없음";

        int start = json.IndexOf("\"text\": \"");
        if (start != -1)
        {
            start += 9;
            int end = json.IndexOf("\"", start);
            if (end > start)
            {
                reply = json.Substring(start, end - start).Replace("\\n", "\n").Replace("\\\"", "\"");
            }
        }

        // 감정 분석
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

        loveScore = Mathf.Clamp(loveScore, -100, 100);

        // 화면과 위젯에 반영
        UpdateWidget(state, reply, loveScore);
    }

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
        catch (System.Exception e) { if(debugText) debugText.text = "위젯 전송 실패: " + e.Message; }
#else
        Debug.Log($"[위젯 전송] {message} (점수: {score})");
#endif
    }

    class BypassCertificate : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData) => true;
    }

    [System.Serializable]
    class WidgetData { public string state; public string message; public int score; }
}