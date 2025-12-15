using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Threading.Tasks;
using Unity.InferenceEngine.Samples.Chat; // [중요] LlavaRunner가 있는 네임스페이스

public class AndroidWidgetBridge : MonoBehaviour
{
    [Header("UI 연결")]
    public Text debugText;
    public Button btnPraise;
    public Button btnScold;

    [Header("설정")]
    private const string PACKAGE_NAME = "com.ddrubok.wegetgame"; // 패키지명 확인!
    private int loveScore = 0;

    // [핵심] Sentis AI 실행기
    private LlavaRunner m_LlavaRunner;
    private Texture2D m_DummyImage; // 모델이 이미지를 요구하므로 가짜 이미지 사용

    async void Start()
    {
        // 1. AI 모델 로딩 (비동기)
        if (debugText) debugText.text = "AI 뇌를 깨우는 중...";

        // 약간의 딜레이 후 초기화 (UI 끊김 방지)
        await Task.Delay(100);

        try
        {
            m_LlavaRunner = new LlavaRunner(lazyInit: false);
            m_DummyImage = new Texture2D(2, 2); // 빈 이미지 생성
            if (debugText) debugText.text = "양파가 깨어났습니다! (준비 완료)";
        }
        catch (System.Exception e)
        {
            if (debugText) debugText.text = $"모델 로딩 실패: {e.Message}\n파일 위치를 확인하세요.";
            Debug.LogError(e);
        }
    }

    // [버튼 1] 칭찬하기 -> AI에게 "사랑해" 전송
    public async void OnClick_Praise()
    {
        await ChatWithOnion("주인님: 우리 양파 정말 착하고 귀여워! 사랑해!", 10);
    }

    // [버튼 2] 혼내기 -> AI에게 "미워" 전송
    public async void OnClick_Scold()
    {
        await ChatWithOnion("주인님: 야! 너 왜 말을 안 들어! 저리 가!", -10);
    }

    // [핵심 로직] AI와 대화하고 위젯 업데이트
    private async Task ChatWithOnion(string userMessage, int scoreChange)
    {
        if (m_LlavaRunner == null) return;

        // 버튼 잠시 비활성화 (중복 클릭 방지)
        btnPraise.interactable = false;
        btnScold.interactable = false;
        if (debugText) debugText.text = "양파가 생각하는 중... 🧅💭";

        string fullResponse = "";

        try
        {
            // 2. AI에게 질문 던지기 (이미지는 더미, 텍스트는 유저 메시지)
            // LlavaRunner가 System Prompt(양파 설정)를 자동으로 적용해 줍니다.
            var tokenStream = m_LlavaRunner.GetPredictionTokenAsync(m_DummyImage, userMessage);

            // 3. 답변 한 글자씩 받기 (스트리밍)
            await foreach (var token in tokenStream)
            {
                string word = m_LlavaRunner.Config.Tokenizer.Decode(new[] { token });
                fullResponse += word;
                // (선택) 화면에 실시간으로 답변이 써지는 효과를 줄 수 있음
                if (debugText) debugText.text = fullResponse;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"AI 생성 중 오류: {e.Message}");
            fullResponse = "으앙 머리가 아파양... (오류)";
        }

        // 4. 감정 태그 분석 ({HAPPY}, {SAD} 찾기)
        string state = "NORMAL";

        if (fullResponse.Contains("{HAPPY}"))
        {
            state = "HAPPY";
            loveScore += 10; // 점수 증가
            fullResponse = fullResponse.Replace("{HAPPY}", "").Trim(); // 태그는 안 보이게 삭제
        }
        else if (fullResponse.Contains("{SAD}"))
        {
            state = "SAD";
            loveScore -= 10; // 점수 감소
            fullResponse = fullResponse.Replace("{SAD}", "").Trim();
        }
        else
        {
            // 태그가 없으면 점수 변화에 따라 자동 결정
            loveScore += scoreChange;
            if (loveScore >= 30) state = "HAPPY";
            if (loveScore <= -30) state = "SAD";
        }

        // 점수 범위 제한 (-100 ~ 100)
        loveScore = Mathf.Clamp(loveScore, -100, 100);

        // 5. 위젯으로 최종 결과 전송
        UpdateWidget(state, fullResponse, loveScore);

        // 버튼 다시 활성화
        btnPraise.interactable = true;
        btnScold.interactable = true;
    }

    public void UpdateWidget(string state, string message, int score)
    {
        // JSON 포장 및 안드로이드 브로드캐스트 전송 (기존과 동일)
        string jsonString = JsonUtility.ToJson(new WidgetData { state = state, message = message, score = score });

        if (debugText) debugText.text = $"[전송 완료]\n{message}";

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
        Debug.Log($"[Editor] 위젯 전송 시뮬레이션: {jsonString}");
#endif
    }

    void OnDestroy()
    {
        // 메모리 누수 방지를 위해 꼭 정리해야 함
        m_LlavaRunner?.Dispose();
    }

    [System.Serializable]
    class WidgetData { public string state; public string message; public int score; }
}