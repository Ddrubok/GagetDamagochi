using UnityEngine;
using UnityEngine.EventSystems;

public class UI_DragFood : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Vector2 _startPos;
    private RectTransform _rect;
    private CanvasGroup _canvasGroup;

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _startPos = _rect.anchoredPosition; // 원래 위치 기억
        _canvasGroup.blocksRaycasts = false; // 드래그 중에는 통과되게 (고양이 감지 위해)
        _canvasGroup.alpha = 0.6f; // 반투명 효과
    }

    public void OnDrag(PointerEventData eventData)
    {
        _rect.anchoredPosition += eventData.delta / Managers.UI.Root.transform.localScale.x;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.alpha = 1.0f;

        //고양이 위에 떨어뜨렸는지 확인 (Raycast)
        GameObject hitObject = eventData.pointerEnter;
        
        // 고양이를 맞췄다면? (Tag나 Name으로 확인)
        if (hitObject != null && hitObject.CompareTag("Player")) // 고양이 태그를 Player로 설정 필요
        {
            Debug.Log("냠냠! 밥 먹기 성공!");
            Managers.Game.Hunger += 20; // 배고픔 회복
            Managers.Game.MyCat.ChangeState(Define.CatState.Eat); // 먹는 모션
        }

        // 원래 위치로 복귀 (아이템 소모 안 되는 방식이라면)
        _rect.anchoredPosition = _startPos;
    }
}