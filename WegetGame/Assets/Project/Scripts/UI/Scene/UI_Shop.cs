using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro를 쓰신다고 가정 (UI_Debug 참고)

public class UI_Shop : UI_Popup
{
    enum Buttons
    {
        BtnClose,       // 닫기 버튼
        BtnUpgrade,     // 터치 강화 버튼
        BtnTranslator   // 번역기 구매 버튼
    }

    //enum Texts
    //{
    //    TextGold,           // 보유 골드
    //    TextLevelInfo,      // 레벨 정보 ("터치 강화 Lv.1")
    //    TextUpgradeCost,    // 강화 가격
    //    TextTranslatorCost, // 번역기 가격 ("5000 G" or "구매 완료")
    //    TextTranslatorName  // 번역기 상품명 (혹시 텍스트 바꿀 일 있으면)
    //}

    // 2. 초기화 (Start 대신 Init 사용)
    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        // 컴포넌트 바인딩 (자동 찾기)
        Bind<Button>(typeof(Buttons));
        //Bind<TextMeshProUGUI>(typeof(Texts));

        // 이벤트 연결 (람다식 활용)
        GetButton((int)Buttons.BtnClose).gameObject.BindEvent((evt) => OnClose());
        GetButton((int)Buttons.BtnUpgrade).gameObject.BindEvent((evt) => OnClickUpgrade());
        GetButton((int)Buttons.BtnTranslator).gameObject.BindEvent((evt) => OnClickTranslator());

        // 초기 UI 갱신
        RefreshUI();

        return true;
    }

    // 상점이 켜질 때마다 정보 갱신 (UI_Popup 기능에 따라 OnEnable 대신 Refresh가 필요할 수 있음)
    private void OnEnable()
    {
        // 이미 Init이 된 상태라면 갱신
        if (_init) RefreshUI();
    }

    // 3. 화면 갱신 로직
    void RefreshUI()
    {
        // 골드 표시
        //GetTextMesh((int)Texts.TextGold).text = $"{Managers.Data.CurrentData.Gold:N0} G";

        // --- 아이템 1: 터치 강화 ---
        int level = Managers.Game.ClickLevel;
        int upCost = Managers.Game.UpgradeCost;

       // GetTextMesh((int)Texts.TextLevelInfo).text = $"터치 강화 (Lv.{level})";
       // GetTextMesh((int)Texts.TextUpgradeCost).text = $"{upCost:N0} G";

        // 돈 부족하면 버튼 비활성화 (반투명 처리 등은 버튼 Transition 설정 따름)
        GetButton((int)Buttons.BtnUpgrade).interactable = (Managers.Data.CurrentData.Gold >= upCost);


        // --- 아이템 2: 번역기 ---
        if (Managers.Game.HasTranslator)
        {
            // 이미 구매함
           // GetTextMesh((int)Texts.TextTranslatorCost).text = "구매 완료";
            GetButton((int)Buttons.BtnTranslator).interactable = false;
        }
        else
        {
            int transCost = 5000;
            //GetTextMesh((int)Texts.TextTranslatorCost).text = $"{transCost:N0} G";
            GetButton((int)Buttons.BtnTranslator).interactable = (Managers.Data.CurrentData.Gold >= transCost);
        }
    }

    // 4. 기능 구현
    void OnClickUpgrade()
    {
        if (Managers.Game.TryUpgradeClick())
        {
            RefreshUI();
            // 효과음 추가 가능: Managers.Sound.Play("BuySuccess");
        }
        else
        {
            // 돈 부족 효과음 or 팝업
        }
    }

    void OnClickTranslator()
    {
        if (Managers.Game.TryBuyTranslator())
        {
            RefreshUI();
        }
    }

    void OnClose()
    {
        // UI_Popup의 닫기 기능 활용
        ClosePopupUI();
    }
}