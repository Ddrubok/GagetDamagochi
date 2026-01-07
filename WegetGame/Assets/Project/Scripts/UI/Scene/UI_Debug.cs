using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_Debug : UI_Popup
{
    enum Buttons
    {
        BtnClose,       // 닫기
        BtnReset,       // 데이터 초기화
        BtnHungerFull,  // 배고픔 100 (밥주기 귀찮을 때)
        BtnHungerZero,  // 배고픔 0 (아픈 상태 테스트)
        BtnLoveUp,      // 애정도 +10
        BtnLoveDown,    // 애정도 -10
        BtnSave         // 강제 저장
    }

    enum Texts
    {
        TextInfo, // 현재 수치 표시용
        TextVoiceLog   //음성 상태 로그 표시용
    }

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        Bind<Button>(typeof(Buttons));
        Bind<TextMeshProUGUI>(typeof(Texts));

        GetButton((int)Buttons.BtnClose).gameObject.BindEvent((evt) => OnClose());
        GetButton((int)Buttons.BtnReset).gameObject.BindEvent((evt) => OnResetData());
        GetButton((int)Buttons.BtnHungerFull).gameObject.BindEvent((evt) => ChangeHunger(100));
        GetButton((int)Buttons.BtnHungerZero).gameObject.BindEvent((evt) => ChangeHunger(0));
        GetButton((int)Buttons.BtnLoveUp).gameObject.BindEvent((evt) => ChangeLove(10));
        GetButton((int)Buttons.BtnLoveDown).gameObject.BindEvent((evt) => ChangeLove(-10));
        GetButton((int)Buttons.BtnSave).gameObject.BindEvent((evt) => OnSave());
        RefreshInfo();

        return true;
    }

    void Update()
    {
        // 켜져 있는 동안 실시간 정보 갱신
        RefreshInfo();
    }

    void RefreshInfo()
    {
        GetTextMesh((int)Texts.TextInfo).text =
            $"[DEBUG MODE]\n" +
            $"Hunger: {Managers.Game.Hunger}\n" +
            $"Love: {Managers.Game.LoveScore}\n" +
            $"State: {Managers.Game.CurrentState}\n" +
            $"Cat: {Managers.Game.MyBreed}/{Managers.Game.MyPersonality}";

        VoiceManager voice = Managers.Instance.GetComponent<VoiceManager>();
        string voiceMsg = (voice != null) ? voice.LastLog : "VoiceManager Not Found";

        GetTextMesh((int)Texts.TextVoiceLog).text = $"[VOICE LOG]\n<color=yellow>{voiceMsg}</color>";
    }

    // --- 기능 구현 ---

    void OnClose()
    {
        ClosePopupUI();
    }

    void OnResetData()
    {
        Managers.Data.ResetData();

        // 매니저 값도 갱신해주기 위해 InitCat 등을 재호출하거나 값을 덮어씀
        Managers.Game.Hunger = 50;
        Managers.Game.LoveScore = 0;

        // 고양이 외형 재적용 (혹시 초기화되니까)
        if (Managers.Game.MyCat != null)
            Managers.Game.MyCat.SetCatVisual(Managers.Game.MyBreed, Managers.Game.MyPersonality);

        Debug.Log("데이터 리셋 완료!");
    }

    void ChangeHunger(int value)
    {
        // 0이면 0으로, 100이면 100으로 설정 (테스트 목적)
        Managers.Game.Hunger = value;
    }

    void ChangeLove(int amount)
    {
        Managers.Game.LoveScore += amount;
    }

    void OnSave()
    {
        Managers.Game.Save();
    }
}