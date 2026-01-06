using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Android; // 안드로이드 권한용
using TMPro; // TextMeshPro 사용
using System.Runtime.InteropServices; // iOS 플러그인용

public class VoiceManager : MonoBehaviour
{
    [Header("UI 연결")]
    public TextMeshProUGUI debugText;
    public Button btnMic;
    public GameManager gameManager;

    private bool isListening = false;

    // =========================================================
    // 📱 1. 안드로이드 전용 변수 & 함수
    // =========================================================
#if UNITY_ANDROID
    private AndroidJavaObject speechRecognizer;
    private AndroidJavaObject recognizerIntent;

    void InitializeAndroid() // 이름을 이걸로 통일했습니다!
    {
        RunOnUIThread(() =>
        {
            try
            {
                AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext");
                AndroidJavaClass speechClass = new AndroidJavaClass("android.speech.SpeechRecognizer");

                speechRecognizer = speechClass.CallStatic<AndroidJavaObject>("createSpeechRecognizer", context);

                if (speechRecognizer != null)
                {
                    speechRecognizer.Call("setRecognitionListener", new RecognitionListenerProxy(this));

                    string ACTION_RECOGNIZE_SPEECH = "android.speech.action.RECOGNIZE_SPEECH";
                    recognizerIntent = new AndroidJavaObject("android.content.Intent", ACTION_RECOGNIZE_SPEECH);
                    recognizerIntent.Call<AndroidJavaObject>("putExtra", "android.speech.extra.LANGUAGE_MODEL", "free_form");
                    recognizerIntent.Call<AndroidJavaObject>("putExtra", "android.speech.extra.LANGUAGE", "ko-KR");

                    UpdateDebug("안드로이드 음성 인식 준비 완료");
                }
            }
            catch (System.Exception e) { UpdateDebug("초기화 에러: " + e.Message); }
        });
    }

    void StartListeningAndroid()
    {
        if (speechRecognizer != null)
        {
            RunOnUIThread(() =>
            {
                speechRecognizer.Call("startListening", recognizerIntent);
                UpdateDebug("듣는 중... 말씀하세요! 🎤");
            });
            isListening = true;
        }
    }

    void StopListeningAndroid()
    {
        if (speechRecognizer != null)
        {
            RunOnUIThread(() =>
            {
                speechRecognizer.Call("stopListening");
                UpdateDebug("듣기 중지");
            });
            isListening = false;
        }
    }

    // 안드로이드 콜백용 프록시 클래스
    class RecognitionListenerProxy : AndroidJavaProxy
    {
        private VoiceManager manager;
        public RecognitionListenerProxy(VoiceManager manager) : base("android.speech.RecognitionListener") { this.manager = manager; }
        public void onResults(AndroidJavaObject bundle)
        {
            AndroidJavaObject matches = bundle.Call<AndroidJavaObject>("getStringArrayList", "results_recognition");
            string result = matches != null ? matches.Call<string>("get", 0) : "";
            manager.OnResult(result);
        }
        public void onError(int error) { manager.OnError(error); }
        public void onReadyForSpeech(AndroidJavaObject param) { }
        public void onBeginningOfSpeech() { }
        public void onRmsChanged(float rmsdB) { }
        public void onBufferReceived(byte[] buffer) { }
        public void onEndOfSpeech() { }
        public void onPartialResults(AndroidJavaObject partialResults) { }
        public void onEvent(int eventType, AndroidJavaObject param) { }
    }
#endif

    // =========================================================
    // 🍎 2. iOS 전용 변수 & 함수 (네이티브 플러그인 연결)
    // =========================================================
#if UNITY_IOS
    [DllImport("__Internal")]
    private static extern void _StartListening();
    [DllImport("__Internal")]
    private static extern void _StopListening();
    
    // iOS에서 초기화는 보통 StartListening 시점에 하거나 필요시 _Init() 함수를 추가합니다.
#endif

    public void IOS_OnError(string error)
    {
        isListening = false;
        UpdateDebug("에러(iOS): " + error);
    }

    // =========================================================
    // 🎮 3. 공통 로직 (Start, 버튼 이벤트 등)
    // =========================================================
    void Start()
    {
        if (gameManager == null) gameManager = Managers.Game;

        UpdateDebug("앱 시작: 마이크 준비 중...");

        // 권한 요청 (안드로이드)
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            Permission.RequestUserPermission(Permission.Microphone);

        InitializeAndroid(); //이제 이 함수가 존재하므로 에러가 나지 않습니다.
#endif

        // 버튼 연결
        if (btnMic != null)
        {
            btnMic.onClick.RemoveAllListeners();
            btnMic.onClick.AddListener(ToggleListening);
        }
    }

    public void ToggleListening()
    {
        if (isListening) StopListening();
        else StartListening();
    }

    public void StartListening()
    {
#if UNITY_ANDROID
        StartListeningAndroid();
#elif UNITY_IOS
        if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            _StartListening(); 
            isListening = true;
            UpdateDebug("듣는 중... (iOS)");
        }
#else
        UpdateDebug("PC에서는 마이크를 사용할 수 없습니다.");
#endif
    }

    void StopListening()
    {
#if UNITY_ANDROID
        StopListeningAndroid();
#elif UNITY_IOS
        if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            _StopListening(); // iOS 플러그인 호출
            isListening = false;
            UpdateDebug("iOS 듣기 중지");
        }
#endif
    }

    // 결과 처리 (안드로이드/iOS 공통)
    public void OnResult(string result)
    {
        isListening = false;
        UpdateDebug("인식 결과: " + result);

        if (gameManager != null) gameManager.OnReceiveVoice(result);
    }

    public void OnError(int error)
    {
        isListening = false;
        string msg = "에러: " + error;
        if (error == 7) msg = "인식된 내용이 없어요.";
        else if (error == 6) msg = "말씀이 없으셨어요.";
        UpdateDebug(msg);
    }

    // iOS에서 유니티로 메시지 보낼 때 사용 (UnitySendMessage)
    public void IOS_OnResult(string result)
    {
        isListening = false;
        UpdateDebug("인식(iOS): " + result);
        if (gameManager != null) gameManager.OnReceiveVoice(result);
    }

    // 디버그 및 스레드 유틸
    void UpdateDebug(string msg)
    {
        if (debugText) debugText.text = msg;
        Debug.Log("[Voice] " + msg);
    }

    void RunOnUIThread(System.Action action)
    {
#if UNITY_ANDROID
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        activity.Call("runOnUiThread", new AndroidJavaRunnable(action));
#endif
    }
}