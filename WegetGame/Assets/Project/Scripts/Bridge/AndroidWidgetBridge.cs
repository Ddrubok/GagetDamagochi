using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class AndroidWidgetBridge : MonoBehaviour
{
    [Header("UI 연결")]
    public InputField chatInput; // 채팅 입력창
    public Button btnSend;       // 전송 버튼
    public Text debugText;       // 대화 내용 표시

    private string apiKey = "";
    private const string PACKAGE_NAME = "com.ddrubok.wegetgame";
    private const string API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent";

    private int loveScore = 0;
    private string mSaveText = ""; // 마지막 대화 저장 변수

    // 양파 성격 프롬프트
    private string systemPrompt =
        "너는 스마트폰 바탕화면에 사는 '양파 쿵야'야. " +
        "사용자의 말을 듣고 감정을 판단해서 대답해. " +
        "1. 칭찬이나 애정표현이면 문장 맨 앞에 {HAPPY}를 붙여. " +
        "2. 욕설, 비난, 혼내는 말이면 문장 맨 앞에 {SAD}를 붙여. " +
        "3. 그 외의 일상적인 대화나 질문이면 태그를 붙이지 마. " +
        "답변은 한국어로 20자 이내로 짧고 재치 있게, 말끝마다 '양!'을 붙여.";

    void Start()
    {
        LoadApiKey();
        LoadGameData(); // 저장된 데이터 불러오기

        if (btnSend) btnSend.onClick.AddListener(OnClick_Send);
    }

    // VoiceManager에서 부르는 함수
    public void SendMessageToGemini(string text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            StartCoroutine(ChatWithGemini(text));
        }
    }

    // 전송 버튼 클릭 시
    public void OnClick_Send()
    {
        if (chatInput != null && chatInput.text.Length > 0)
        {
            StartCoroutine(ChatWithGemini(chatInput.text));
            chatInput.text = "";
        }
    }

    IEnumerator ChatWithGemini(string userMessage)
    {
        if (btnSend) btnSend.interactable = false;
        if (debugText) debugText.text = "양파가 눈치를 보는 중... 🧅👀";

        string finalUrl = $"{API_URL}?key={apiKey}";

        // JSON 안전 포장
        GeminiRequest requestData = new GeminiRequest();
        requestData.contents = new Content[]
        {
            new Content { parts = new Part[] { new Part { text = systemPrompt + "\n\nUser: " + userMessage } } }
        };
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
                string errorMsg = (request.responseCode == 429) ? "양파가 피곤하대요 (1분 휴식)" : "통신 실패";
                if (debugText) debugText.text = errorMsg;
            }
            else
            {
                ParseAndApplyResponse(request.downloadHandler.text);
            }
        }

        if (btnSend) btnSend.interactable = true;
    }

    void ParseAndApplyResponse(string json)
    {
        string reply = "알 수 없음";

        // JSON 파싱 (text 추출)
        int start = json.IndexOf("\"text\": \"");
        if (start != -1)
        {
            start += 9;
            int end = json.IndexOf("\"", start);
            if (end > start) reply = json.Substring(start, end - start).Replace("\\n", "\n").Replace("\\\"", "\"");
        }

        // 감정 분석 로직
        string state = "NORMAL";
        if (reply.Contains("{HAPPY}"))
        {
            state = "HAPPY";
            loveScore += 5;
            reply = reply.Replace("{HAPPY}", "").Trim();
        }
        else if (reply.Contains("{SAD}"))
        {
            state = "SAD";
            loveScore -= 5;
            reply = reply.Replace("{SAD}", "").Trim();
        }

        loveScore = Mathf.Clamp(loveScore, -100, 100);

        // ✅ [수정 포인트] 데이터를 여기서 확실하게 갱신하고 저장합니다.
        // 화면(UpdateWidget)과 데이터(SaveGameData)를 분리했습니다.

        // 1. 표시할 텍스트 만들기
        mSaveText = $"[{state}] {reply}\n(호감도: {loveScore})";

        // 2. 위젯으로 보내기
        UpdateWidget(state, reply, loveScore);

        // 3. 저장하기 (mSaveText가 이미 갱신되었으므로 안전함)
        SaveGameData();
    }

    void LoadApiKey()
    {
        TextAsset keyFile = Resources.Load<TextAsset>("GeminiKey");
        if (keyFile != null) apiKey = keyFile.text.Trim();
    }

    void LoadGameData()
    {
        loveScore = PlayerPrefs.GetInt("OnionScore", 0);
        mSaveText = PlayerPrefs.GetString("OnionString", ""); // 저장된 대화 불러오기

        // 불러온 내용이 있으면 화면에 보여주기
        if (debugText && !string.IsNullOrEmpty(mSaveText))
            debugText.text = mSaveText;
    }

    void SaveGameData()
    {
        PlayerPrefs.SetInt("OnionScore", loveScore);
        PlayerPrefs.SetString("OnionString", mSaveText); // 마지막 대화 저장
        PlayerPrefs.Save();
    }

    public void UpdateWidget(string state, string message, int score)
    {
        string jsonString = JsonUtility.ToJson(new WidgetData { state = state, message = message, score = score });

        // 화면 갱신
        if (debugText) debugText.text = mSaveText;

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
        catch (System.Exception e) {}
#endif
    }

    // JSON 클래스들 (맨 아래 유지)
    class BypassCertificate : CertificateHandler { protected override bool ValidateCertificate(byte[] certificateData) => true; }
    [System.Serializable] class WidgetData { public string state; public string message; public int score; }
    [System.Serializable] public class GeminiRequest { public Content[] contents; }
    [System.Serializable] public class Content { public Part[] parts; }
    [System.Serializable] public class Part { public string text; }
}