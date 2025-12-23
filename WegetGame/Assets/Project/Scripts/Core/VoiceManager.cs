using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Android;
using TMPro; // 안드로이드 권한용

public class VoiceManager : MonoBehaviour
{
    [Header("UI 연결 (인스펙터에서 드래그)")]
    public TextMeshProUGUI debugText;       // 디버그용 텍스트
    public Button btnMic;        // 마이크 버튼

    // 게임 매니저 (자동으로 찾음)
    public GameManager gameManager;

    private AndroidJavaObject speechRecognizer;
    private AndroidJavaObject recognizerIntent;
    private bool isListening = false;

    void Start()
    {
        // ✅ 매니저 연결 (Managers.Game이 싱글톤으로 존재하므로 안전함)
        if (gameManager == null)
        {
            gameManager = Managers.Game;
        }

        UpdateDebug("앱 시작: 권한 체크 중...");

        // 1. 마이크 권한 요청
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }

        // 2. 안드로이드 음성 인식기 준비
        if (Application.platform == RuntimePlatform.Android)
        {
            InitializeSpeechRecognizer();
        }
        else
        {
            UpdateDebug("PC/에디터에서는 마이크 안 됨 (폰에서만 가능)");
        }

        // 3. 버튼 리스너 연결
        if (btnMic != null)
        {
            btnMic.onClick.RemoveAllListeners(); // 중복 방지
            btnMic.onClick.AddListener(ToggleListening);
        }
    }

    void InitializeSpeechRecognizer()
    {
        RunOnUIThread(() => {
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

                    UpdateDebug("음성 인식 준비 완료");
                }
            }
            catch (System.Exception e) { UpdateDebug("초기화 에러: " + e.Message); }
        });
    }

    public void ToggleListening()
    {
        if (isListening) StopListening();
        else StartListening();
    }

    void StartListening()
    {
        if (speechRecognizer != null)
        {
            RunOnUIThread(() => {
                speechRecognizer.Call("startListening", recognizerIntent);
                UpdateDebug("듣는 중... 말씀하세요! 🎤");
            });
            isListening = true;
        }
    }

    void StopListening()
    {
        if (speechRecognizer != null)
        {
            RunOnUIThread(() => {
                speechRecognizer.Call("stopListening");
                UpdateDebug("듣기 중지");
            });
            isListening = false;
        }
    }

    public void OnResult(string result)
    {
        isListening = false;
        UpdateDebug("인식: " + result);

        // ✅ 매니저에게 텍스트 전달 -> Gemini와 대화 시작!
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

    void UpdateDebug(string msg)
    {
        if (debugText) debugText.text = msg;
        Debug.Log("[Voice] " + msg);
    }

    void RunOnUIThread(System.Action action)
    {
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        activity.Call("runOnUiThread", new AndroidJavaRunnable(action));
    }

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
}