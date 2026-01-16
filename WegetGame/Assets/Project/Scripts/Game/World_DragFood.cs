using UnityEngine;

public class World_DragFood : MonoBehaviour
{
    private Vector3 _startPos;
    private bool _isDragging = false;
    private SpriteRenderer _spriteRenderer;
    private int _originalSortingOrder;

    void Awake()
    {
        Init();
    }

    public void Init()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // 1. 입력 감지 (PC는 마우스, 모바일은 터치)
        if (Input.GetMouseButtonDown(0)) // 터치 시작 (Began)
        {
            // 마우스 입력 위치 가져오기
            Vector3 screenInput = Input.mousePosition;

            // ★ 핵심 수정: 카메라(Z=-10)와 음식(Z=0) 사이의 거리 = 10
            // 이걸 안 해주면 카메라 렌즈 바로 앞(-10) 좌표를 계산해서 오차가 생길 수 있음
            screenInput.z = 10f;
            // (또는 -Camera.main.transform.position.z 로 적어도 됨)

            // 2. 화면 좌표 -> 게임 월드 좌표 변환
            Vector2 worldPos = Camera.main.ScreenToWorldPoint(screenInput);

            // 3. 해당 좌표(점)에 있는 콜라이더 찾기 (Raycast보다 훨씬 정확함!)
            Collider2D hit = Physics2D.OverlapPoint(worldPos);

            // 4. 감지된 게 '나(음식)'인지 확인
            if (hit != null && hit.gameObject == gameObject)
            {
                OnDragStart();
            }
        }
        else if (Input.GetMouseButton(0) && _isDragging) // 드래그 중 (Moved)
        {
            OnDragging();
        }
        else if (Input.GetMouseButtonUp(0) && _isDragging) // 터치 끝 (Ended)
        {
            OnDragEnd();
        }
    }

    void OnDragStart()
    {
        _isDragging = true;
        _startPos = transform.position;
        _originalSortingOrder = _spriteRenderer.sortingOrder;
        _spriteRenderer.sortingOrder = 100;
    }

    void OnDragging()
    {
        Vector3 screenPos = Input.mousePosition;

        screenPos.z = 10f;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);

        transform.position = worldPos;
    }

    void OnDragEnd()
    {
        _isDragging = false;
        _spriteRenderer.sortingOrder = _originalSortingOrder;

        Vector3 screenPos = Input.mousePosition;
        screenPos.z = 10f; // 카메라 거리 보정

        Vector2 dropPos = Camera.main.ScreenToWorldPoint(screenPos);
        Collider2D[] hits = Physics2D.OverlapPointAll(dropPos);

        bool isFed = false;

        foreach (var col in hits)
        {
            if (col.gameObject != gameObject && col.CompareTag("Player"))
            {
                CatController cat = col.GetComponent<CatController>();

                if (cat != null)
                {
                    Debug.Log("냠냠! 고양이가 밥을 먹기 시작합니다!");

                    cat.CurrentState = Define.CatState.Eat;

                    // 2. (선택사항) 배고픔 수치 회복 (매니저가 있다면)
                    Managers.Game.Hunger += 20;

                    isFed = true;
                }
                break;
            }
        }

        // 밥을 먹었으면 음식은 사라져야 함
        if (isFed)
        {
            // 방법 A: 아예 삭제 (Destroy)
            // Destroy(gameObject);

            // 방법 B: 안 보이게 꺼두기 (나중에 재활용할거면)
            // gameObject.SetActive(false);
            transform.position = _startPos;
        }
        else
        {
            // 못 먹었으면 제자리로 뿅
            transform.position = _startPos;
        }
    }
}
