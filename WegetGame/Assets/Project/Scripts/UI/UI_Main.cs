using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
public class UI_Main : UI_Scene
{

    [SerializeField] private Slider _hungerSlider;
    [SerializeField] private Text _goldText;

    enum Sliders
    {
        HungerSlider
    }
    enum GameObjects
    {
        ImgBubble
    }
    enum Buttons
    {
        Feed,
        Send,
        BtnDebug,
        BtnShop
    }

    enum Texts
    {
        OnionDebug,
        Status,
        VoiceDebug,
        TextBubble
    }

    enum InputFields
    {
        Input
    }

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        Bind<GameObject>(typeof(GameObjects));
        Bind<Button>(typeof(Buttons));
        Bind<TextMeshProUGUI>(typeof(Texts));
        Bind<TMP_InputField>(typeof(InputFields));
        Bind<Slider>(typeof(Sliders));
        GetButton((int)Buttons.BtnDebug).gameObject.BindEvent(OnClickDebug);
        GetButton((int)Buttons.BtnShop).gameObject.BindEvent(OnClickShop);
        _hungerSlider = GetSlider((int)Sliders.HungerSlider);
        Managers.Game.OnHungerChanged += RefreshUI;
        Managers.Game.OnLoveScoreChanged += RefreshUI;

        UpdateHungerUI(Managers.Game.Hunger);

        Managers.Game.OnHungerChanged += UpdateHungerUI;

        RefreshUI(0);

        StartCoroutine(CoCheckOfflineReward());
        return true;
    }

    void OnClick_Send(PointerEventData evt)
    {
        string msg = Get<TMP_InputField>((int)InputFields.Input).text;

        if (string.IsNullOrEmpty(msg)) return;

        Managers.Game.ProcessChat(msg, (reply) =>
        {
            GetTextMesh((int)Texts.OnionDebug).text = reply;
        });

        Get<TMP_InputField>((int)InputFields.Input).text = "";
    }

    void OnClick_Feed(PointerEventData evt)
    {
        Managers.Game.Hunger += 30; GetTextMesh((int)Texts.OnionDebug).text = "냠냠! 밥 맛있다냥!";

        if (Managers.Game.MyCat != null)
            Managers.Game.MyCat.ChangeState(Define.CatState.Eat);
    }

    void OnClickDebug(PointerEventData evt)
    {
        Managers.UI.ShowPopupUI<UI_Debug>("Prefabs/UI/PopUp/UI_Debug");
    }

    void OnClickShop(PointerEventData evt)
    {
        Managers.UI.ShowPopupUI<UI_Shop>("Prefabs/UI/PopUp/UI_Shop");
    }
    void RefreshUI(int val)
    {
        int hunger = Managers.Game.Hunger;
        int love = Managers.Game.LoveScore;

    }

    public void ShowBubble(string text, float duration = 3.0f)
    {
        GetTextMesh((int)Texts.TextBubble).text = text;

        GetObject((int)GameObjects.ImgBubble).SetActive(true);

        StopAllCoroutines();
        StartCoroutine(CoHideBubble(duration));
    }

    IEnumerator CoHideBubble(float duration)
    {
        yield return new WaitForSeconds(duration);

        GetObject((int)GameObjects.ImgBubble).SetActive(false);

        if (Managers.Game.MyCat != null)
            Managers.Game.MyCat.ChangeState(Define.CatState.Idle);
    }

    void UpdateHungerUI(int value)
    {
        // 0~100 값을 0.0~1.0 (Slider 범위)으로 변환
        _hungerSlider.value = value / 100.0f;
    }

    IEnumerator CoCheckOfflineReward()
    {
        yield return new WaitForSeconds(1.0f); // 씬 로딩 후 잠시 대기

        long earnedGold = Managers.Game.CalculateOfflineGold();
        if (earnedGold > 0)
        {
            string message = $"집사야! 자는 동안\n{earnedGold:N0} 골드나 벌어왔다냥!\n(효율 50% 적용)";
            ShowBubble(message, 5.0f);

            // 골드 텍스트 갱신 (UI_Main에 골드 텍스트 업데이트 로직이 필요함)
            // RefreshGoldUI(); 
        }
    }

    // 오브젝트가 파괴될 때 구독 해제 (에러 방지)
    void OnDestroy()
    {
        if (Managers.Instance != null)
            Managers.Game.OnHungerChanged -= UpdateHungerUI;
    }

}