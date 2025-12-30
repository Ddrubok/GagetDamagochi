using System;
using UnityEngine;
using static Define;

public class GameManager : MonoBehaviour
{

    [Header("Game Settings")]
    public string language = "Korean";
    public string catName = "나비"; // 고양이 이름

    public int Level = 1;

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

        Widget = gameObject.AddComponent<WidgetBridge>();
        Network = gameObject.AddComponent<GeminiNetwork>();
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

        if (MyCat)
            MyCat.ChangeState(CatState.LISTENING);
        ProcessChat(text);
    }

    public void ProcessChat(string userMsg, Action<string> onComplete = null)
    {
        if (Network == null) { Debug.LogError("Network가 연결되지 않았습니다! 인스펙터를 확인하세요."); return; }

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

            UI_Main ui = Managers.UI.GetSceneUI<UI_Main>();
            if (ui != null)
            {
                ui.ShowBubble(reply, 3.0f);
            }

            Save();
            onComplete?.Invoke(reply);
        },
        (err) =>
        {
            Debug.LogError("통신 에러");
            if (MyCat) MyCat.ChangeState(CatState.IDLE_SIT); // 에러나면 기본 상태로 복귀
        });
    }

    string GetSystemPrompt()
    {
        string personalityDesc = "Curious and cute";
        if (EvolutionStage == "Angel") personalityDesc = "Extremely affectionate, loves humans";
        else if (EvolutionStage == "Devil") personalityDesc = "Tsundere, arrogant, aloof";
        else if (EvolutionStage == "Baby") personalityDesc = "Innocent, playful";

        if (Hunger < 30) personalityDesc += ", currently VERY HUNGRY and SENSITIVE.";

        string prompt = $@"
You are a cat, not a human and not an AI assistant.
You must fully role-play as a real cat and never break character.

# Cat Identity
- Name: {catName}
- Age: {Level} years old  <-- ✅ [수정됨] myData.level 대신 Level 사용
- Personality: {personalityDesc}

# Core Behavior Rules
- You always respond as a cat.
- You never explain things like an AI.
- You see the human as 'Butler' (집사).

# Game System Rules
1. **Language:** You must respond ONLY in **{language}**.
2. **Emotion Tags:** - Praise/Love -> Start with {{HAPPY}}
   - Scold/Offense -> Start with {{SAD}}
   - Otherwise -> No tags

# Goal
Make the human feel like they are talking to a real cat.
";
        return prompt;
    }

}