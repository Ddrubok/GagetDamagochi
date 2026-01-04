using System;
using UnityEngine;
using static Define;

public class GameManager
{
    [Header("Game Settings")]
    public string language = "Korean";

    public GeminiNetwork Network;
    public WidgetBridge Widget;
    public CatController MyCat;


    public event Action<int> OnHungerChanged;
    public int Hunger
    {
        get { return Managers.Data.CurrentData.hunger; }
        set
        {
            int clampedValue = Mathf.Clamp(value, 0, 100);
            Managers.Data.CurrentData.hunger = clampedValue;
            OnHungerChanged?.Invoke(clampedValue);
        }
    }

    public event Action<int> OnLoveScoreChanged;
    public int LoveScore
    {
        get { return Managers.Data.CurrentData.loveScore; }
        set
        {
            int clampedValue = Mathf.Clamp(value, -100, 100);
            Managers.Data.CurrentData.loveScore = clampedValue;
            OnLoveScoreChanged?.Invoke(clampedValue);
        }
    }

    public int Level
    {
        get { return Managers.Data.CurrentData.level; }
        set { Managers.Data.CurrentData.level = value; }
    }

    public string CatName
    {
        get { return Managers.Data.CurrentData.catName; }
        set { Managers.Data.CurrentData.catName = value; }
    }

    public CatBreed MyBreed
    {
        get { return Managers.Data.CurrentData.myBreed; }
        set { Managers.Data.CurrentData.myBreed = value; }
    }

    public CatPersonality MyPersonality
    {
        get { return Managers.Data.CurrentData.myPersonality; }
        set { Managers.Data.CurrentData.myPersonality = value; }
    }

    public string EvolutionStage
    {
        get { return Managers.Data.CurrentData.evolutionStage; }
        set { Managers.Data.CurrentData.evolutionStage = value; }
    }

    public CatState CurrentState = CatState.Idle;


    public void Init()
    {
        Managers.Data.Init();
        GameObject go = Managers.Instance.gameObject;
        Widget = Util.GetOrAddComponent<WidgetBridge>(go);
        Network = Util.GetOrAddComponent<GeminiNetwork>(go);
        Network.InitKey();
        CalculateOfflineProgress();
    }

    public void InitCat()
    {
        if (MyCat != null)
        {
            MyCat.SetCatVisual(MyBreed, MyPersonality);

            OnHungerChanged?.Invoke(Hunger);
            OnLoveScoreChanged?.Invoke(LoveScore);

            Debug.Log($"고양이 로드 완료: {MyBreed} / {MyPersonality}");
        }
    }

    public void GachaCat()
    {
        Array breeds = Enum.GetValues(typeof(CatBreed));
        MyBreed = (CatBreed)breeds.GetValue(UnityEngine.Random.Range(0, breeds.Length));

        Array personalities = Enum.GetValues(typeof(CatPersonality));
        MyPersonality = (CatPersonality)personalities.GetValue(UnityEngine.Random.Range(0, personalities.Length));

        Debug.Log($"뽑기 완료! [{MyBreed}] / [{MyPersonality}]");

        Save();

        if (MyCat != null) MyCat.SetCatVisual(MyBreed, MyPersonality);
    }

    void CalculateOfflineProgress()
    {
        string lastTimeStr = Managers.Data.CurrentData.lastExitTime;
        if (string.IsNullOrEmpty(lastTimeStr)) return;

        DateTime lastTime = DateTime.Parse(lastTimeStr);
        TimeSpan timePassed = DateTime.Now - lastTime;

        int hungerDecrease = (int)(timePassed.TotalMinutes / 10);
        if (hungerDecrease > 0)
        {
            Hunger = Hunger - hungerDecrease;
            Debug.Log($"오프라인 보상: 배고픔 -{hungerDecrease}");
        }
    }

    public void Save()
    {
        if (Widget != null)
            Widget.SendToWidget(CurrentState.ToString(), "잘 있었냥?", LoveScore);

        Managers.Data.SaveGame();
    }

    public void OnReceiveVoice(string text)
    {
        if (MyCat) MyCat.ChangeState(CatState.Listening);
        ProcessChat(text);
    }

    public void ProcessChat(string userMsg, Action<string> onComplete = null)
    {
        if (Network == null) { Debug.LogError("GeminiNetwork 연결 안됨"); return; }

        if (MyCat) MyCat.ChangeState(CatState.Thinking);

        string prompt = GetSystemPrompt();

        Network.SendChat(prompt, userMsg, (reply) =>
        {
            CatState reaction = CatState.Talking;

            if (reply.Contains("{HAPPY}"))
            {
                reaction = CatState.Happy;
                LoveScore += 5;
                reply = reply.Replace("{HAPPY}", "").Trim();
            }
            else if (reply.Contains("{SAD}"))
            {
                reaction = CatState.Angry;
                LoveScore -= 5;
                reply = reply.Replace("{SAD}", "").Trim();
            }

            if (MyCat) MyCat.ChangeState(reaction);

            UI_Main ui = Managers.UI.GetSceneUI<UI_Main>();
            if (ui != null) ui.ShowBubble(reply, 3.0f);

            Save();
            onComplete?.Invoke(reply);
        },
        (err) =>
        {
            Debug.LogError($"통신 에러{err}");
            if (MyCat) MyCat.ChangeState(CatState.Idle);
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
Role-play strictly as a real cat.

# Identity
- Name: {CatName}
- Age: {Level} years old
- Personality: {personalityDesc} ({MyPersonality})

# Rules
1. Respond ONLY in **{language}**.
2. Start with {{HAPPY}} if praised, {{SAD}} if scolded.
3. Be short and concise.

Make the human feel like they are talking to a real cat.
";
        return prompt;
    }
}