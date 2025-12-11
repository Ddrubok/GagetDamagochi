using UnityEngine;
using System.IO;

public class AndroidWidgetBridge : MonoBehaviour
{
    // 싱글톤 패턴 적용
    public static AndroidWidgetBridge Instance;

    // 앞서 설정한 Package Name과 정확히 일치해야 함
    private const string PACKAGE_NAME = "com.ddrubok.wegetgame";
    private const string FILE_NAME = "widget_data.json";

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 유지
    }

    // 테스트용: 게임 시작 시 데이터 한번 보내보기
    private void Start()
    {
        UpdateWidgetState("IDLE", 80);
    }

    public void UpdateWidgetState(string state, int hunger)
    {
        // 1. 데이터 객체 생성
        CapsuleData data = new CapsuleData();
        data.state = state;
        data.hunger = hunger;
        data.lastInteractionTime = System.DateTimeOffset.Now.ToUnixTimeSeconds();

        // 2. JSON 변환
        string json = JsonUtility.ToJson(data);

        // 3. 파일로 저장 & 위젯 갱신 요청
        SendToAndroid(json);
    }

    private void SendToAndroid(string jsonString)
    {
        // 유니티 에디터에서는 안드로이드 기능이 동작하지 않으므로 예외 처리
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            // A. 안드로이드 내부 저장소에 파일 쓰기
            // Unity의 persistentDataPath를 사용하되, 나중에 Native 쪽에서 읽을 수 있게 경로 설계 필요
            // 여기서는 가장 단순하게 Unity Activity를 통해 Intent로 데이터를 실어 보냅니다.
            // (파일 공유 방식은 권한 문제가 복잡하므로, 초기 단계에선 Intent 방식 추천)

            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject context = currentActivity.Call<AndroidJavaObject>("getApplicationContext"))
            {
                // B. 방송(Broadcast) 보내기
                // "야! TinyCapsule 데이터 갱신됐어!"라고 안드로이드 시스템에 소리침
                using (AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent"))
                {
                    // 이 Action 문자열은 AndroidManifest.xml에 등록될 예정
                    intent.Call<AndroidJavaObject>("setAction", PACKAGE_NAME + ".ACTION_WIDGET_UPDATE");
                    
                    // JSON 데이터를 Intent라는 봉투에 넣음
                    intent.Call<AndroidJavaObject>("putExtra", "EXTRA_DATA_JSON", jsonString);
                    
                    // 방송 송출
                    context.Call("sendBroadcast", intent);
                }
            }
            
            Debug.Log($"[Bridge] Sent Broadcast: {jsonString}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Bridge] Error: {e.Message}");
        }
#else
        Debug.Log($"[Editor Mock] Android Broadcast Sent: {jsonString}");
#endif
    }
}