using System;

[Serializable]
public class CapsuleData
{
    // 몬스터의 현재 상태 (IDLE, SLEEP, EAT, POOP)
    // 위젯은 이 문자열을 보고 어떤 이미지를 띄울지 결정합니다.
    public string state = "IDLE";

    // 배고픔 수치 (0 ~ 100) -> 위젯 배경색이나 아이콘 변화용
    public int hunger = 100;

    // 몬스터 종류 ID (진화 단계)
    public int monsterId = 1;

    // 마지막으로 상호작용(밥주기 등)한 시간 (Unix Timestamp)
    // 앱이 꺼져 있어도 시간이 얼마나 지났는지 위젯이 계산하기 위함
    public long lastInteractionTime;
}