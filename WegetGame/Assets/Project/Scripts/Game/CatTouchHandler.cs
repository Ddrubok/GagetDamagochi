using UnityEngine;
using UnityEngine.EventSystems;

public class CatTouchHandler : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        // 1. 이미 대화 중이거나 행동 중이면 무시 (또는 애정 표현)
        if (Managers.Game.CurrentState == Define.CatState.Thinking ||
            Managers.Game.CurrentState == Define.CatState.Talking)
        {
            return;
        }

        Debug.Log("고양이 터치! 대화 시작");

        // 2. 고양이 상태 변경 (듣는 모션)
        if (Managers.Game.MyCat != null)
        {
            Managers.Game.MyCat.ChangeState(Define.CatState.Listening);
        }

        VoiceManager voice = Managers.Instance.GetComponent<VoiceManager>();

        if (voice != null)
        {
            voice.StartListening(); 
        }
        else
        {
            Debug.LogError("VoiceManager를 찾을 수 없습니다! Managers 오브젝트에 붙여주세요.");
        }
    }
}