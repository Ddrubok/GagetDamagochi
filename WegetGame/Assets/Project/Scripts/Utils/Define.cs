using UnityEngine;

public class Define
{
    // 1. 고양이 종 (외형/색깔 결정) - n가지
    public enum CatBreed
    {
        Cheese,     // 치즈태비 (노랑)
        Mackerel,   // 고등어 (회색 줄무늬)
        Black,      // 올블랙 (검정)
        Calico,     // 삼색이
        White       // 터키쉬 앙고라 (흰색)
    }

    // 2. 고양이 성격 (행동 패턴/IDLE 모션 결정) - 5가지
    public enum CatPersonality
    {
        Normal,     // 평범/기본
        Tsundere,   // 츤데레 (새침떼기)
        DogCat,     // 개냥이 (활발)
        Lazy,       // 귀차니즘 (게으름)
        Noble       // 귀족/선비 (거만)
    }

    // 3. 고양이의 현재 상태 (FSM 동작) - 모든 고양이 공통
    public enum CatState
    {
        None,       // 초기화 전

        // --- 기본 생활 ---
        Idle,       // 기본 대기 (성격에 따라 모션 다름!)
        Sleep,      // 잠자기
        Eat,        // 밥 먹기
        Walk,       // 돌아다니기

        // --- 대화/상호작용 ---
        Listening,  // 듣는 중 (귀 쫑긋)
        Thinking,   // 생각 중 (Gemini 통신 중)
        Talking,    // 대답하는 중 (입 뻥긋)

        // --- 감정 표현 ---
        Happy,      // 기쁨 (칭찬받음)
        Sad,
        Angry,      // 화남/삐짐 (혼남)
        Sick,        // 아픔 (배고픔 0일 때)
        Play,
        Sleepy
    }

    public enum EScene
    {
        Unknown,
        TitleScene,
        GameScene,
    }

    public enum EUIEvent
    {
        Click,
        PointerDown,
        PointerUp,
        Drag,
    }

    public enum ESound
    {
        Bgm,
        Effect,
        Max,
    }
}