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

    public int ClickLevel
    {
        get { return Managers.Data.CurrentData.ClickLevel; }
        set { Managers.Data.CurrentData.ClickLevel = value; }
    }

    public bool HasTranslator
    {
        get { return Managers.Data.CurrentData.HasTranslator; }
        set { Managers.Data.CurrentData.HasTranslator = value; }
    }

    public int GoldPerClick
    {
        get
        {
            return 10 * ClickLevel;
        }
    }

    public int UpgradeCost
    {
        get
        {
            // 예: 기본 100원에서 시작, 레벨마다 1.5배씩 비싸짐
            return (int)(100 * Mathf.Pow(1.5f, ClickLevel - 1));
        }
    }

    // 터치했을 때 돈 벌기 (CatController에서 호출)
    public void EarnGoldByClick()
    {
        int amount = GoldPerClick;
        Managers.Data.CurrentData.Gold += amount;

        Debug.Log($"골드 획득! +{amount} (현재: {Managers.Data.CurrentData.Gold})");
    }

    public bool TryUpgradeClick()
    {
        int cost = UpgradeCost;
        if (Managers.Data.CurrentData.Gold >= cost)
        {
            Managers.Data.CurrentData.Gold -= cost;
            ClickLevel++;
            Debug.Log($"레벨업 성공! Lv.{ClickLevel}");
            Save(); // 저장
            return true;
        }
        else
        {
            Debug.Log("골드가 부족합니다!");
            return false;
        }
    }

    // 번역기 구매 시도
    public bool TryBuyTranslator()
    {
        int cost = 5000; // 번역기는 비쌈
        if (HasTranslator) return false; // 이미 샀음

        if (Managers.Data.CurrentData.Gold >= cost)
        {
            Managers.Data.CurrentData.Gold -= cost;
            HasTranslator = true;
            Debug.Log("번역기 구매 완료! 이제 고양이 말이 들립니다.");
            Save();
            return true;
        }
        return false;
    }

    // =========================================================
    // ★ 2. 야옹어 변환기 (번역기 로직)
    // =========================================================
    public string GetFinalMessage(string originalText)
    {
        // 1. 번역기가 있으면 원래 한국어 출력
        if (HasTranslator)
        {
            return originalText;
        }

        // 2. 번역기가 없으면 "야옹"으로 변조
        // AI가 긴 말을 했다면 야옹도 길게, 짧으면 짧게
        return ConvertToMeow(originalText);
    }

    private string ConvertToMeow(string text)
    {
        string[] meowSounds = { "야옹", "냐옹", "미야옹~", "그릉...", "냥!", "아르릉" };

        // 문장 길이에 따라 야옹 횟수 결정 (대략 5글자당 1번 야옹)
        int count = Mathf.Max(1, text.Length / 5);

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < count; i++)
        {
            sb.Append(meowSounds[UnityEngine.Random.Range(0, meowSounds.Length)]);
            sb.Append(" "); // 띄어쓰기

            // 가끔 느낌표나 물음표 붙이기
            if (UnityEngine.Random.value > 0.7f) sb.Append("? ");
            else if (UnityEngine.Random.value > 0.8f) sb.Append("! ");
        }

        return sb.ToString().Trim();
    }

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

            //UI_Main ui = Managers.UI.GetSceneUI<UI_Main>();
            //if (ui != null) ui.ShowBubble(reply, 3.0f);

            //if (MyCat != null)
            //{
            //    MyCat.ShowBubble(reply); // 고양이 스크립트에 만든 함수 호출
            //}

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