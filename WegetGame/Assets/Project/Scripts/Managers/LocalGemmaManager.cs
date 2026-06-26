using System;
using System.Collections.Concurrent;
using UnityEngine;

public class LocalGemmaManager : MonoBehaviour
{
    private AndroidJavaObject _nativeBridge;

    // UI 업데이트를 위한 메인 쓰레드 큐
    private ConcurrentQueue<Action> _mainThreadActions = new ConcurrentQueue<Action>();

    // Java의 LlmListener를 C#에서 구현
    class LlmCallbackProxy : AndroidJavaProxy
    {
        private LocalGemmaManager _manager;
        private Action<string> _onPartial;
        private Action<string> _onComplete;
        private Action<string> _onError;

        public LlmCallbackProxy(LocalGemmaManager manager, Action<string> onPartial, Action<string> onComplete, Action<string> onError)
            : base("com.ddrubok.wegetgame.bridge.MediaPipeLlmBridge$LlmListener")
        {
            _manager = manager;
            _onPartial = onPartial;
            _onComplete = onComplete;
            _onError = onError;
        }

        public void onPartialResult(string partialText)
        {
            _manager.QueueOnMainThread(() => _onPartial?.Invoke(partialText));
        }

        public void onComplete(string resultText)
        {
            _manager.QueueOnMainThread(() => _onComplete?.Invoke(resultText));
        }

        public void onError(string errorMsg)
        {
            _manager.QueueOnMainThread(() => _onError?.Invoke(errorMsg));
        }
    }

    void Update()
    {
        // 매 프레임마다 큐에 쌓인 UI 업데이트 작업을 실행 (안전함 보장)
        while (_mainThreadActions.TryDequeue(out Action action))
        {
            action?.Invoke();
        }
    }

    public void QueueOnMainThread(Action action)
    {
        _mainThreadActions.Enqueue(action);
    }

    public void InitializeLocalLLM(Action onSuccess, Action<string> onError)
    {
        if (Application.platform != RuntimePlatform.Android) return;

        try
        {
            _nativeBridge = new AndroidJavaObject("com.ddrubok.wegetgame.bridge.GemmaLlmBridge");

            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                LlmCallbackProxy proxy = new LlmCallbackProxy(this, null,
                    (msg) => onSuccess?.Invoke(),
                    (err) => onError?.Invoke(err));

                _nativeBridge.Call("initModel", currentActivity, proxy);
            }
        }
        catch (Exception e) { onError?.Invoke(e.Message); }
    }

    public void SendChatToGemma(string prompt, Action<string> onPartial, Action<string> onComplete, Action<string> onError)
    {
        if (_nativeBridge == null) return;

        LlmCallbackProxy proxy = new LlmCallbackProxy(this, onPartial, onComplete, onError);
        _nativeBridge.Call("generateAsync", prompt, proxy);
    }

    void OnDestroy()
    {
        _nativeBridge?.Call("closeModel");
    }
}