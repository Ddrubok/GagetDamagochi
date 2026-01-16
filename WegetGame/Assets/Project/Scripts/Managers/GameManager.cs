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

    // 편의를 위해 데이터 경로를 짧게 줄여주는 프로퍼티
    private CatData _catData
    {
        get
        {
            // 혹시 데이터가 로드 안 됐을 때 안전장치
            if (Managers.Data == null || Managers.Data.CurrentData == null) return null;
            return Managers.Data.CurrentData.MyCat;
        }
    }

    public event Action<int> OnHungerChanged;
    public int Hunger
    {
        get
        {
            if (_catData == null) return 50;
            return (int)_catData.Hunger; // float -> int 변환
        }
        set
        {
            if (_catData == null) return;

            float clampedValue = Mathf.Clamp(value, 0, 100);
            _catData.Hunger = clampedValue;

            // UI 업데이트는 int로 알려줌
            OnHungerChanged?.Invoke((int)clampedValue);
        }
    }

    public event Action<int> OnLoveScoreChanged;
    public int LoveScore
    {
        get
        {
            if (_catData == null) return 0;
            return _catData.LoveScore;
        }
        set
        {
            if (_catData == null) return;

            int clampedValue = Mathf.Clamp(value, -100, 100);
            _catData.LoveScore = clampedValue;
            OnLoveScoreChanged?.Invoke(clampedValue);
        }
    }

    public int Level
    {
        get { return _catData != null ? _catData.Level : 1; }
        set { if (_catData != null) _catData.Level = value; }
    }

    public string CatName
    {
        get { return _catData != null ? _catData.Name : "나비"; }
        set { if (_catData != null) _catData.Name = value; }
    }

    public CatBreed MyBreed
    {
        get { return _catData != null ? _catData.Breed : CatBreed.Cheese; }
        set { if (_catData != null) _catData.Breed = value; }
    }

    public CatPersonality MyPersonality
    {
        get { return _catData != null ? _catData.Personality : CatPersonality.Normal; }
        set { if (_catData != null) _catData.Personality = value; }
    }

    public string EvolutionStage
    {
        get { return _catData != null ? _catData.EvolutionStage : "Baby"; }
        set { if (_catData != null) _catData.EvolutionStage = value; }
    }

    public CatState CurrentState = CatState.Idle;


    public void Init()
    {
        GameObject go = Managers.Instance.gameObject;
        Widget = Util.GetOrAddComponent<WidgetBridge>(go);
        Network = Util.GetOrAddComponent<GeminiNetwork>(go);

        if (Network != null) Network.InitKey();

        // 오프라인 보상 계산은 DataManager에서 이미 Load 시점에 수행했으므로
        // 여기서는 변경된 값을 UI에 뿌려주기만 하면 됨
        RefreshStats();
    }

    // 초기값 UI 갱신용
    public void RefreshStats()
    {
        OnHungerChanged?.Invoke(Hunger);
        OnLoveScoreChanged?.Invoke(LoveScore);
    }

    public void InitCat()
    {
        if (MyCat != null)
        {
            // 데이터에 있는 외형으로 고양이 설정
            MyCat.SetCatVisual(MyBreed, MyPersonality);

            RefreshStats();

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

    // CalculateOfflineProgress는 DataManager.LoadGame() 쪽으로 로직을 옮겼으므로 삭제하거나
    // 만약 GameManager에서 꼭 해야 한다면 아래처럼 경로 수정:
    /*
    void CalculateOfflineProgress()
    {
        string lastTimeStr = _catData.LastExitTime;
        // ... (나머지 로직)
    }
    */

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