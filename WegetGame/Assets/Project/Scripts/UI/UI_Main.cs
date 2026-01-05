using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
public class UI_Main : UI_Scene
{
    enum GameObjects
    {
        ImgBubble
    }
    enum Buttons
    {
        Feed,
        Send,
        BtnDebug
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
        GetButton((int)Buttons.Feed).gameObject.BindEvent(OnClick_Feed);
        GetButton((int)Buttons.Send).gameObject.BindEvent(OnClick_Send);
        GetButton((int)Buttons.BtnDebug).gameObject.BindEvent(OnClickDebug);
        Managers.Game.OnHungerChanged += RefreshUI;
        Managers.Game.OnLoveScoreChanged += RefreshUI;

        RefreshUI(0);

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
        // 팝업 띄우기
        Managers.UI.ShowPopupUI<UI_Debug>("Prefabs/UI/PopUp/UI_Debug");
    }

    void RefreshUI(int val)
    {
        int hunger = Managers.Game.Hunger;
        int love = Managers.Game.LoveScore;

        GetTextMesh((int)Texts.Status).text = $"배고픔: {hunger} / 호감도: {love}";
    }

    public void ShowBubble(string text, float duration = 3.0f)
    {
        // 1. 텍스트 설정
        GetTextMesh((int)Texts.TextBubble).text = text;

        // 2. 말풍선 켜기
        GetObject((int)GameObjects.ImgBubble).SetActive(true);

        // 3. 기존에 켜져 있던 타이머가 있다면 끄고 새로 시작 (깜빡임 방지)
        StopAllCoroutines();
        StartCoroutine(CoHideBubble(duration));
    }

    // 일정 시간 뒤에 자동으로 꺼지는 코루틴
    IEnumerator CoHideBubble(float duration)
    {
        yield return new WaitForSeconds(duration);

        // 말풍선 끄기
        GetObject((int)GameObjects.ImgBubble).SetActive(false);

        // 고양이 상태도 IDLE로 복귀 요청 (매니저가 있다면)
        if (Managers.Game.MyCat != null)
            Managers.Game.MyCat.ChangeState(Define.CatState.Idle);
    }
}