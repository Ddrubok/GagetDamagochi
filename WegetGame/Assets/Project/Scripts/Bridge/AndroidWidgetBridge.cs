using UnityEngine;
using UnityEngine.UI;

public class AndroidWidgetBridge : MonoBehaviour
{
    public Text debugText; // 화면 디버깅용 (선택)

    // 양파의 사랑 지수 (-100 ~ 100)
    private int loveScore = 0;

    // [버튼 1] "사랑해" 버튼에 연결
    public void OnClick_Praise()
    {
        loveScore += 10;
        if (loveScore > 100) loveScore = 100;
        SendOnionState();
    }

    // [버튼 2] "미워해" 버튼에 연결
    public void OnClick_Scold()
    {
        loveScore -= 10;
        if (loveScore < -100) loveScore = -100;
        SendOnionState();
    }

    private void SendOnionState()
    {
        string state = "NORMAL";
        string message = "양파는 평범해.";

        // 점수에 따라 상태 결정
        if (loveScore >= 30)
        {
            state = "HAPPY";
            message = $"행복한 양파 (Lv.{loveScore})";
        }
        else if (loveScore <= -30)
        {
            state = "SAD";
            message = $"상처받은 양파 (Lv.{loveScore})";
        }

        UpdateWidget(state, message, loveScore);
    }

    public void UpdateWidget(string state, string message, int score)
    {
        // JSON 데이터 생성
        string jsonString = JsonUtility.ToJson(new WidgetData { state = state, message = message, score = score });

        if (debugText != null) debugText.text = $"전송: {message}";

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject context = currentActivity.Call<AndroidJavaObject>("getApplicationContext"))
            using (AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent"))
            {
                string packageName = "com.ddrubok.wegetgame";
                // 명시적 인텐트 (아까 성공했던 방식)
                intent.Call<AndroidJavaObject>("setClassName", packageName, packageName + ".TinyCapsuleWidget");
                intent.Call<AndroidJavaObject>("setAction", packageName + ".ACTION_WIDGET_UPDATE");
                intent.Call<AndroidJavaObject>("putExtra", "EXTRA_DATA_JSON", jsonString);
                
                context.Call("sendBroadcast", intent);
            }
        }
        catch (System.Exception e) { if(debugText) debugText.text = "Error: " + e.Message; }
#else
        Debug.Log($"[Editor] {jsonString}");
#endif
    }

    [System.Serializable]
    class WidgetData
    {
        public string state;
        public string message;
        public int score;
    }
}