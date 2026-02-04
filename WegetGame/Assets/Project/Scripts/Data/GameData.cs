using System;
using System.Collections.Generic;
using static Define; // 리스트 사용을 위해 필요

[Serializable]
public class GameData
{
    // ==========================================
    // 1. 계정(집사) 정보 (유저가 가진 재산)
    // ==========================================
    public long Gold = 0;              // 재화 (캔, 골드 등)
    public int GoldAmountLevel = 1; // 수익 레벨
    public int GoldSpeedLevel = 1;  // 속도 레벨

    public string LastAppExitTime = "";

    public bool IsBgmOn = true;       // 설정: 배경음악
    public bool IsSfxOn = true;       // 설정: 효과음

    // 가지고 있는 가구 아이디 목록 (예: "bed_01", "rug_05")
    public List<string> OwnedFurnitureIds = new List<string>();

    // ==========================================
    // 2. 고양이 정보 (내 고양이의 상태)
    // ==========================================
    // 나중에 고양이를 여러 마리 키우는 업데이트를 대비해 배열이나 리스트로 확장 가능하게 분리
    public CatData MyCat = new CatData();

    public int ClickLevel = 1;       // 터치 레벨 (높을수록 돈 많이 범)
    public bool HasTranslator = false; // 번역기 아이템 보유 여부
}

[Serializable]
public class CatData
{
    // ==========================================
    // 고양이의 신상 정보
    // ==========================================
    public string Name = "나비";
    public CatBreed Breed = CatBreed.Cheese;
    public CatPersonality Personality = CatPersonality.Normal;

    // ==========================================
    // 성장 및 상태
    // ==========================================
    public int Level = 1;
    public int LoveScore = 0;     // 애정도 (레벨업 기준)
    public string EvolutionStage = "Baby"; // Baby -> Child -> Adult

    // 게이지 (부드러운 감소를 위해 float 추천)
    public float Hunger = 50.0f;
    public float Fun = 50.0f;

    // ==========================================
    // 시간 계산 (방치형 요소)
    // ==========================================
    // 켜져있지 않아도 배고픔이 줄어들게 하기 위해 필수
    public string LastExitTime;
}