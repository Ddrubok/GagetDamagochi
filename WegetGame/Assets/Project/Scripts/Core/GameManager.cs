using UnityEngine;
using UnityEngine.UI;
using System;

public class GameManager : MonoBehaviour
{
    [Header("부서 연결")]
    public GeminiNetwork network; // 통신팀
    public WidgetBridge widget;   // 파견팀

    [Header("UI 연결")]
    public InputField chatInput;
    public Button btnSend;
    public Button btnFeed;
    public Text debugText;
    public Text statusText;

    // 데이터 클래스 (기존과 동일)
    [Serializable]
    public class GameData
    {
        public int level = 1;
        public int exp = 0;
        public int hunger = 10;
        public int loveScore = 0;
        public string evolutionStage = "Baby";
        public string lastLoginTime;
        public string lastMessage = "";
    }
    public GameData myData = new GameData();

    void Start()
    {
        network.InitKey(); // 통신팀에게 키 준비시키기
        LoadGameData();    // 데이터 불러오기

        if (btnSend) btnSend.onClick.AddListener(OnClick_Send);
        if (btnFeed) btnFeed.onClick.AddListener(OnClick_Feed);

        UpdateUI();
    }

    // VoiceManager가 이제 이 함수를 부릅니다
    public void OnReceiveVoice(string text)
    {
        if (!string.IsNullOrEmpty(text)) ProcessChat(text);
    }

    public void OnClick_Send()
    {
        if (chatInput != null && chatInput.text.Length > 0)
        {
            ProcessChat(chatInput.text);
            chatInput.text = "";
        }
    }

    // 채팅 처리의 핵심 로직
    void ProcessChat(string userMsg)
    {
        if (btnSend) btnSend.interactable = false;
        if (debugText) debugText.text = "생각 중... 🧅💭";

        // 프롬프트 조립
        string prompt = GetSystemPrompt();

        // ✅ 통신팀(Network)에게 일 시키기
        network.SendChat(prompt, userMsg,
            (reply) => { // 성공했을 때
                ApplyResponse(reply);
                if (btnSend) btnSend.interactable = true;
            },
            (error) => { // 실패했을 때
                if (debugText) debugText.text = (error == "429") ? "피곤해양 (잠시 대기)" : "통신 실패";
                if (btnSend) btnSend.interactable = true;
            }
        );
    }

    void ApplyResponse(string reply)
    {
        string state = "NORMAL";
        if (reply.Contains("{HAPPY}")) { state = "HAPPY"; myData.loveScore += 5; reply = reply.Replace("{HAPPY}", "").Trim(); }
        else if (reply.Contains("{SAD}")) { state = "SAD"; myData.loveScore -= 5; reply = reply.Replace("{SAD}", "").Trim(); }

        myData.exp += 5;
        myData.lastMessage = $"[{state}] {reply}";
        myData.loveScore = Mathf.Clamp(myData.loveScore, -100, 100);

        CheckLevelUp();
        SaveGameData();
        UpdateUI();

        // ✅ 파견팀(Widget)에게 위젯 업데이트 시키기
        widget.SendToWidget(state, reply, myData.loveScore);
    }

    public void OnClick_Feed()
    {
        if (myData.hunger >= 100) return;
        myData.hunger = Mathf.Min(myData.hunger + 30, 100);
        myData.exp += 10;
        myData.loveScore += 2;
        CheckLevelUp();
        SaveGameData();
        UpdateUI();
        widget.SendToWidget("HAPPY", "냠냠! 밥 맛있다양!", myData.loveScore);
    }

    // ----------------------
    // [헬퍼 함수들]
    // ----------------------
    string GetSystemPrompt()
    {
        string p = "너는 '양파 쿵야'야. 20자 이내 한국어로 대답해. 말끝에 '양!' 붙여. ";
        if (myData.hunger < 30) p += "배고파서 까칠해. ";
        if (myData.evolutionStage == "Devil") p += "건방진 반말을 써. ";
        else if (myData.evolutionStage == "Angel") p += "천사같이 상냥해. ";
        p += "칭찬은 {HAPPY}, 비난은 {SAD} 태그 붙여.";
        return p;
    }

    void CheckLevelUp()
    {
        if (myData.exp >= myData.level * 50)
        {
            myData.level++;
            myData.exp = 0;
            if (myData.level == 5)
            {
                if (myData.loveScore >= 30) myData.evolutionStage = "Angel";
                else if (myData.loveScore <= -30) myData.evolutionStage = "Devil";
                else myData.evolutionStage = "Adult";
            }
        }
    }

    void LoadGameData()
    {
        string json = PlayerPrefs.GetString("OnionData_V2", "");
        if (!string.IsNullOrEmpty(json))
        {
            myData = JsonUtility.FromJson<GameData>(json);
            if (long.TryParse(myData.lastLoginTime, out long lastTime))
            {
                int hungerDecrease = (int)(new TimeSpan(DateTime.Now.Ticks - lastTime).TotalMinutes / 10) * 5;
                if (hungerDecrease > 0) myData.hunger = Mathf.Max(0, myData.hunger - hungerDecrease);
            }
        }
        else
        {
            myData = new GameData();
            myData.lastLoginTime = DateTime.Now.Ticks.ToString();
        }
    }

    void SaveGameData()
    {
        myData.lastLoginTime = DateTime.Now.Ticks.ToString();
        PlayerPrefs.SetString("OnionData_V2", JsonUtility.ToJson(myData));
        PlayerPrefs.Save();
    }

    void UpdateUI()
    {
        if (statusText) statusText.text = $"Lv.{myData.level} ({myData.evolutionStage})\n배고픔: {myData.hunger}/100\n호감도: {myData.loveScore}";
        if (debugText) debugText.text = myData.lastMessage;
    }
}