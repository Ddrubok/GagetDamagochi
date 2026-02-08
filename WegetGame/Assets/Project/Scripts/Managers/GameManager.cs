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

    private float _timer = 0f;

    private CatData _catData
    {
        get
        {
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
            return (int)_catData.Hunger;
        }
        set
        {
            if (_catData == null) return;

            float clampedValue = Mathf.Clamp(value, 0, 100);
            _catData.Hunger = clampedValue;

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


    public event Action<long> OnGoldChanged;

    public long Gold
    {
        get { return Managers.Data.CurrentData.Gold; }
        set
        {
            Managers.Data.CurrentData.Gold = value;
            OnGoldChanged?.Invoke(value);
        }
    }


    public void EarnGoldAuto()
    {
        long baseAmount = CurrentGoldAmount;
        long finalAmount = (long)(baseAmount * GetLoveEfficiency());

        Gold += finalAmount;
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
            return (int)(100 * Mathf.Pow(1.5f, ClickLevel - 1));
        }
    }

    public void EarnGoldByClick()
    {
        int amount = GoldPerClick;
        Gold += amount;

        Debug.Log($"골드 획득! +{amount} (현재: {Gold})");
    }

    public bool TryUpgradeClick()
    {
        int cost = UpgradeCost;
        if (Gold >= cost)
        {
            Gold -= cost;
            ClickLevel++;
            Debug.Log($"레벨업 성공! Lv.{ClickLevel}");
            Save(); return true;
        }
        else
        {
            Debug.Log("골드가 부족합니다!");
            return false;
        }
    }

    public bool TryBuyTranslator()
    {
        int cost = 5000; if (HasTranslator) return false;
        if (Gold >= cost)
        {
            Gold -= cost;
            HasTranslator = true;
            Debug.Log("번역기 구매 완료! 이제 고양이 말이 들립니다.");
            Save();
            return true;
        }
        return false;
    }

    public string GetFinalMessage(string originalText)
    {
        if (HasTranslator)
        {
            return originalText;
        }

        return ConvertToMeow(originalText);
    }

    private string ConvertToMeow(string text)
    {
        string[] meowSounds = { "야옹", "냐옹", "미야옹~", "그릉...", "냥!", "아르릉" };

        int count = Mathf.Max(1, text.Length / 5);

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < count; i++)
        {
            sb.Append(meowSounds[UnityEngine.Random.Range(0, meowSounds.Length)]);
            sb.Append(" ");
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

        RefreshStats();
    }

    public void RefreshStats()
    {
        OnHungerChanged?.Invoke(Hunger);
        OnLoveScoreChanged?.Invoke(LoveScore);
    }

    public void InitCat()
    {
        if (MyCat != null)
        {
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

    /*
void CalculateOfflineProgress()
{
string lastTimeStr = _catData.LastExitTime;
    }
*/

    public void Save()
    {
        if (Widget != null)
            Widget.SendToWidget(CurrentState.ToString(), "잘 있었냥?", LoveScore);
        SaveExitTime();
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

# Economic Status
- Your current Love Efficiency: {GetLoveEfficiency() * 100}%
- If efficiency is over 100%, you are very helpful and bring more gold to the human.
- If efficiency is low, you are lazy and don't care about gold.

# Rules
1. Respond ONLY in **{language}**.
2. Start with {{HAPPY}} if praised, {{SAD}} if scolded.
3. Be short and concise.
4. Mention your helpfulness if Love Efficiency is high.

Make the human feel like they are talking to a real cat.
";
        return prompt;
    }

    public int CurrentGoldAmount
    {
        get { return Managers.Data.CurrentData.GoldAmountLevel; }
    }

    public float CurrentGoldInterval
    {
        get
        {
            float interval = 60.0f - (Managers.Data.CurrentData.GoldSpeedLevel - 1);
            return Mathf.Max(0.5f, interval);
        }
    }

    public long CostAmountUpgrade
    {
        get { return (long)(100 * Mathf.Pow(1.5f, Managers.Data.CurrentData.GoldAmountLevel - 1)); }
    }

    public long CostSpeedUpgrade
    {
        get { return (long)(300 * Mathf.Pow(1.6f, Managers.Data.CurrentData.GoldSpeedLevel - 1)); }
    }

    public float TimerRatio
    {
        get { return Mathf.Clamp01(_timer / CurrentGoldInterval); }
    }

    public void OnUpdate()
    {
        _timer += Time.deltaTime;

        if (_timer >= CurrentGoldInterval)
        {
            _timer = 0f; 
            EarnGoldAuto();
        }
    }

  


    public bool TryUpgradeAmount()
    {
        long cost = CostAmountUpgrade;
        if (Gold >= cost)
        {
            Gold -= cost;
            Managers.Data.CurrentData.GoldAmountLevel++;
            Save();
            return true;
        }
        return false;
    }

    public bool TryUpgradeSpeed()
    {
        long cost = CostSpeedUpgrade;
        if (Gold >= cost)
        {
            Gold -= cost;
            Managers.Data.CurrentData.GoldSpeedLevel++;
            Save();
            return true;
        }
        return false;
    }

    public void SaveExitTime()
    {
        Managers.Data.CurrentData.LastAppExitTime = DateTime.Now.ToString();
    }

    public long CalculateOfflineGold()
    {
        string lastTimeStr = Managers.Data.CurrentData.LastAppExitTime;
        if (string.IsNullOrEmpty(lastTimeStr)) return 0;

        if (DateTime.TryParse(lastTimeStr, out DateTime lastTime))
        {
            TimeSpan timeDiff = DateTime.Now - lastTime;
            double totalSeconds = timeDiff.TotalSeconds;

            if (totalSeconds < 10) return 0; // 최소 10초는 지나야 보상

            // 1. 최대 시간 제한 (8시간)
            double maxSeconds = 8 * 60 * 60;
            if (totalSeconds > maxSeconds) totalSeconds = maxSeconds;

            // 2. 수익 계산 (현재 업그레이드 수치 반영)
            float interval = CurrentGoldInterval; // 60s - (Level-1)
            long count = (long)(totalSeconds / interval);

            // 3. 효율 적용 (50%)
            long rawGold = count * CurrentGoldAmount;
            long finalGold = (long)(rawGold * GetLoveEfficiency());

            if (finalGold > 0)
            {
                Managers.Data.CurrentData.Gold += finalGold;
                // 보상을 받은 후 시간 데이터 초기화 (중복 수령 방지)
                Managers.Data.CurrentData.LastAppExitTime = DateTime.Now.ToString();
                Save();
            }

            return finalGold;
        }
        return 0;
    }

    public float GetLoveEfficiency()
    {
        // return 1.0f; 

        int score = LoveScore; 

        float efficiency = 0.5f + (score * 0.01f);

        return Mathf.Clamp(efficiency, 0.5f, 2.0f);
    }
}