using System;
using UnityEngine;
using static Define;

public class GameManager : MonoBehaviour
{
    public GeminiNetwork Network;
    public WidgetBridge Widget;
    public CatController MyCat;

    #region 다마고치 데이터
    public event Action<int> OnHungerChanged;
    private int _hunger = 50;
    public int Hunger
    {
        get { return _hunger; }
        set { _hunger = Mathf.Clamp(value, 0, 100); OnHungerChanged?.Invoke(_hunger); }
    }

    public event Action<int> OnLoveScoreChanged;
    private int _loveScore = 0;
    public int LoveScore
    {
        get { return _loveScore; }
        set { _loveScore = Mathf.Clamp(value, -100, 100); OnLoveScoreChanged?.Invoke(_loveScore); }
    }

    public string EvolutionStage = "Baby";
    public string LastLoginTime;
    #endregion

    public void Init()
    {

        Widget=gameObject.AddComponent<WidgetBridge>();
        Network= gameObject.AddComponent<GeminiNetwork>();
        Network.InitKey();

        Hunger = PlayerPrefs.GetInt("Hunger", 50);
        LoveScore = PlayerPrefs.GetInt("LoveScore", 0);
        EvolutionStage = PlayerPrefs.GetString("Stage", "Baby");
        LastLoginTime = PlayerPrefs.GetString("LastTime", DateTime.Now.Ticks.ToString());

        CalculateTimePassed();
    }

    void CalculateTimePassed()
    {
        if (long.TryParse(LastLoginTime, out long lastTicks))
        {
            TimeSpan passed = new TimeSpan(DateTime.Now.Ticks - lastTicks);
            int hungerDrop = (int)(passed.TotalMinutes / 10) * 5;
            if (hungerDrop > 0)
            {
                Hunger -= hungerDrop;
                Debug.Log($"지난 시간 동안 배가 {hungerDrop}만큼 고파졌어냥...");
            }
        }
    }

    public void Save()
    {
        PlayerPrefs.SetInt("Hunger", Hunger);
        PlayerPrefs.SetInt("LoveScore", LoveScore);
        PlayerPrefs.SetString("Stage", EvolutionStage);
        PlayerPrefs.SetString("LastTime", DateTime.Now.Ticks.ToString());
        PlayerPrefs.Save();
    }

    public void OnReceiveVoice(string text)
    {
        ProcessChat(text);
    }

    public void ProcessChat(string userMsg, Action<string> onComplete = null)
    {
        if (Network == null) { Debug.LogError("❌ Network가 연결되지 않았습니다! 인스펙터를 확인하세요."); return; }

        if (MyCat) MyCat.ChangeState(CatState.THINKING);

        string prompt = GetSystemPrompt();

        Network.SendChat(prompt, userMsg, (reply) =>
        {
            CatState reaction = CatState.TALKING;
            if (reply.Contains("{HAPPY}"))
            {
                reaction = CatState.HAPPY_PURR;
                LoveScore += 5;
                reply = reply.Replace("{HAPPY}", "").Trim();
            }
            else if (reply.Contains("{SAD}"))
            {
                reaction = CatState.ANGRY_HISS;
                LoveScore -= 5;
                reply = reply.Replace("{SAD}", "").Trim();
            }

            if (MyCat) MyCat.ChangeState(reaction);
            if (Widget) Widget.SendToWidget(reaction.ToString(), reply, LoveScore);

            Save();
            onComplete?.Invoke(reply);
        },
        (err) => { Debug.LogError("통신 에러: " + err); });
    }

    string GetSystemPrompt()
    {
        string p = "너는 귀여운 '고양이'야. 집사에게 20자 이내로 짧게 대답해. 말끝에 '냥'을 붙여. ";
        if (Hunger < 30) p += "배가 고파서 예민해. ";
        p += "칭찬은 {HAPPY}, 비난은 {SAD} 태그를 붙여.";
        return p;
    }
}