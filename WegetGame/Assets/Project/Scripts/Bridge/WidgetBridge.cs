using UnityEngine;

public class WidgetBridge : MonoBehaviour
{
    private const string PACKAGE_NAME = "com.ddrubok.wegetgame";

    // GameManager가 이 함수를 호출해서 위젯을 업데이트함
    public void SendToWidget(string state, string message, int score)
    {
        string jsonString = JsonUtility.ToJson(new WidgetData { state = state, message = message, score = score });

#if UNITY_ANDROID && !UNITY_EDITOR
        try {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject context = currentActivity.Call<AndroidJavaObject>("getApplicationContext"))
            using (AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent")) {
                intent.Call<AndroidJavaObject>("setClassName", PACKAGE_NAME, PACKAGE_NAME + ".TinyCapsuleWidget");
                intent.Call<AndroidJavaObject>("setAction", PACKAGE_NAME + ".ACTION_WIDGET_UPDATE");
                intent.Call<AndroidJavaObject>("putExtra", "EXTRA_DATA_JSON", jsonString);
                context.Call("sendBroadcast", intent);
            }
        } catch (System.Exception e) { Debug.LogError("위젯 전송 실패: " + e.Message); }
#else
        Debug.Log($"[위젯 전송 시뮬레이션] 상태:{state}, 메시지:{message}, 점수:{score}");
#endif
    }

    [System.Serializable] class WidgetData { public string state; public string message; public int score; }
}