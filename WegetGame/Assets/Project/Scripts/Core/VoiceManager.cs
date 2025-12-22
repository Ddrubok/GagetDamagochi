using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Android;

public class VoiceManager : MonoBehaviour
{
    public Text debugText;       // 상태 표시용
    public Button btnMic;        // 마이크 버튼
    public GameManager gameManager;

    private AndroidJavaObject speechRecognizer;
    private AndroidJavaObject recognizerIntent;
    private bool isListening = false;

    void Start()
    {
        if(gameManager ==null)
        {
            gameManager = Managers.Game;
        }
        UpdateDebug("앱 시작: 권한 체크 중...");

        // 1. 마이크 권한 요청
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
            UpdateDebug("권한 요청 팝업 띄움");
        }
        else
        {
            UpdateDebug("권한 이미 허용됨 OK");
        }

        // 2. 안드로이드 음성 인식기 준비
        if (Application.platform == RuntimePlatform.Android)
        {
            InitializeSpeechRecognizer();
        }
        else
        {
            UpdateDebug("PC에서는 마이크 안 됨 (폰에서만 가능)");
        }

        // 3. 버튼 연결
        if (btnMic != null)
        {
            btnMic.onClick.AddListener(ToggleListening);
            UpdateDebug("마이크 버튼 연결 완료");
        }
        else
        {
            UpdateDebug("🚨 경고: 마이크 버튼이 연결 안 됨!");
        }
    }

    void InitializeSpeechRecognizer()
    {
        // 안드로이드 UI 스레드에서 생성해야 안전합니다.
        RunOnUIThread(() => {
            try
            {
                AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext");

                AndroidJavaClass speechClass = new AndroidJavaClass("android.speech.SpeechRecognizer");

                // 여기서 인식기를 만듭니다.
                speechRecognizer = speechClass.CallStatic<AndroidJavaObject>("createSpeechRecognizer", context);

                if (speechRecognizer != null)
                {
                    speechRecognizer.Call("setRecognitionListener", new RecognitionListenerProxy(this));

                    string ACTION_RECOGNIZE_SPEECH = "android.speech.action.RECOGNIZE_SPEECH";
                    recognizerIntent = new AndroidJavaObject("android.content.Intent", ACTION_RECOGNIZE_SPEECH);
                    recognizerIntent.Call<AndroidJavaObject>("putExtra", "android.speech.extra.LANGUAGE_MODEL", "free_form");
                    recognizerIntent.Call<AndroidJavaObject>("putExtra", "android.speech.extra.LANGUAGE", "ko-KR");

                    UpdateDebug("음성 인식기 준비 완료 (Ready)");
                }
                else
                {
                    // 만약 여전히 null이면, 폰에 'Google' 앱이 없거나 비활성화된 상태입니다.
                    UpdateDebug("🚨 인식기 생성 실패: Google 앱이 설치되어 있나요?");
                }
            }
            catch (System.Exception e)
            {
                UpdateDebug("🚨 초기화 에러: " + e.Message);
            }
        });
    }

    public void ToggleListening()
    {
        UpdateDebug("버튼 눌림!"); // 버튼 반응 확인용

        if (isListening) StopListening();
        else StartListening();
    }

    void StartListening()
    {
        if (speechRecognizer != null)
        {
            RunOnUIThread(() => {
                try
                {
                    speechRecognizer.Call("startListening", recognizerIntent);
                    UpdateDebug("듣는 중... 말씀하세요! 🎤");
                }
                catch (System.Exception e)
                {
                    UpdateDebug("시작 에러: " + e.Message);
                }
            });
            isListening = true;
        }
        else
        {
            UpdateDebug("오류: 인식기가 없습니다.");
        }
    }

    void StopListening()
    {
        if (speechRecognizer != null)
        {
            RunOnUIThread(() => {
                try
                {
                    speechRecognizer.Call("stopListening");
                    UpdateDebug("듣기 중지");
                }
                catch (System.Exception e)
                {
                    UpdateDebug("중지 에러: " + e.Message);
                }
            });
            isListening = false;
        }
    }

    public void OnResult(string result)
    {
        isListening = false;
        UpdateDebug("인식 성공: " + result);

        if (gameManager) gameManager.OnReceiveVoice(result);
        else UpdateDebug("경고: Bridge 연결 안 됨");
    }

    public void OnError(int error)
    {
        isListening = false;
        string msg = "에러 코드: " + error;

        // 자주 발생하는 에러 코드 해석
        if (error == 7) msg += " (인식 결과 없음)";
        else if (error == 6) msg += " (음성 입력 없음)";
        else if (error == 9) msg += " (권한 부족)";

        UpdateDebug(msg);
    }

    void UpdateDebug(string msg)
    {
        if (debugText) debugText.text = msg;
        // 로그캣에서도 볼 수 있게
        Debug.Log("[VoiceManager] " + msg);
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