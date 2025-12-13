using UnityEngine;

public class WidgetManager : MonoBehaviour
{
    private const string PACKAGE_NAME = "com.ddrubok.wegetgame";

    public void UpdateWidget(string message)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        using (AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext"))
        {
            try
            {
                // JSON 데이터 생성
                using (AndroidJavaObject jsonObject = new AndroidJavaObject("org.json.JSONObject"))
                {
                    jsonObject.Call<AndroidJavaObject>("put", "state", message);
                    
                    // Intent 생성 및 전송
                    using (AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent"))
                    {
                        intent.Call<AndroidJavaObject>("setAction", PACKAGE_NAME + ".ACTION_WIDGET_UPDATE");
                        intent.Call<AndroidJavaObject>("putExtra", "EXTRA_DATA_JSON", jsonObject.Call<string>("toString"));
                        context.Call("sendBroadcast", intent);
                    }
                }
                Debug.Log("위젯 업데이트 성공: " + message);
            }
            catch (System.Exception e)
            {
                Debug.LogError("위젯 업데이트 실패: " + e.Message);
            }
        }
#else
        Debug.Log("에디터에서는 위젯을 업데이트할 수 없습니다: " + message);
#endif
    }

    void Start()
    {
        // 테스트
        UpdateWidget("게임 시작!");
    }
}