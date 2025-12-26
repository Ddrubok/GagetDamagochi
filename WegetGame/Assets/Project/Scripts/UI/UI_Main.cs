using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
public class UI_Main : UI_Scene
{
    enum Buttons
    {
        Feed,
        Send
    }

    enum Texts
    {
        OnionDebug,
        Status,
        VoiceDebug
    }

    enum InputFields
    {
        Input
    }

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        Bind<Button>(typeof(Buttons));
        Bind<TextMeshProUGUI>(typeof(Texts)); 
        Bind<TMP_InputField>(typeof(InputFields));
        GetButton((int)Buttons.Feed).gameObject.BindEvent(OnClick_Feed);
        GetButton((int)Buttons.Send).gameObject.BindEvent(OnClick_Send);

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
        Managers.Game.Hunger += 30; GetText((int)Texts.OnionDebug).text = "≥»≥»! π‰ ∏¿¿÷¥Ÿ≥…!";

        if (Managers.Game.MyCat != null)
            Managers.Game.MyCat.ChangeState(Define.CatState.EATING);
    }

    void RefreshUI(int val)
    {
        int hunger = Managers.Game.Hunger;
        int love = Managers.Game.LoveScore;

        GetTextMesh((int)Texts.Status).text = $"πË∞Ì«ƒ: {hunger} / »£∞®µµ: {love}";
    }
}