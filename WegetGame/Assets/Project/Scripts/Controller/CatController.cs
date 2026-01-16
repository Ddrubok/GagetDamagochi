using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class CatController : BaseController
{
    private Animator _animator;
    private CatState _currentState = CatState.Idle;

    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 2.0f;
    [SerializeField] private Vector2 _minRoomPos = new Vector2(-2, -2);
    [SerializeField] private Vector2 _maxRoomPos = new Vector2(2, 2);

    private float _stateTimer = 0f;
    private Vector3 _targetPosition;
    private SpriteRenderer _spriteRenderer;

    private CatData _myCatData => Managers.Data.CurrentData.MyCat;

    public CatState CurrentState
    {
        get { return _currentState; }
        set
        {
            if (_currentState == value) return;
            _currentState = value;
            ChangeState(_currentState);
        }
    }

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        // 애니메이터 찾기 (구조에 따라 경로 수정 필요)
        _animator = Util.GetOrAddComponent<Animator>(Util.FindChild(gameObject, "Animation", true));
        _spriteRenderer = _animator.GetComponent<SpriteRenderer>();

        // ★ [변경] 하드코딩 제거 -> 데이터 매니저에서 저장된 외형 정보 불러오기
        if (Managers.Data != null)
        {
            SetCatVisual(_myCatData.Breed, _myCatData.Personality);
            Debug.Log($"[CatController] 저장된 고양이 불러오기: {_myCatData.Name} (배고픔: {_myCatData.Hunger:F1})");
        }
        else
        {
            // 매니저가 없을 경우(테스트용) 기본값
            SetCatVisual(CatBreed.Cheese, CatPersonality.Normal);
        }

        ChangeState(_currentState);

        return true;
    }

    public void ChangeState(CatState newState)
    {
        // 상태 변경 시 애니메이션 및 타이머 설정
        switch (newState)
        {
            case CatState.Idle:
                _stateTimer = Random.Range(2.0f, 4.0f);
                PlayAnimByState(CatState.Idle);
                break;

            case CatState.Walk:
                PlayAnimByState(CatState.Walk);
                break;

            case CatState.Sleep:
                _stateTimer = Random.Range(5.0f, 10.0f);
                PlayAnimByState(CatState.Sleep);
                break;

            case CatState.Play:
                _stateTimer = 3.0f;
                PlayAnimByState(CatState.Play);
                break;

            case CatState.Eat:
                _stateTimer = 3.0f; // 3초간 냠냠
                PlayAnimByState(CatState.Eat);
                break;

            // 아플 때는 타이머 없이 계속 앓아눕게 처리
            case CatState.Sick:
                PlayAnimByState(CatState.Sick);
                break;

            default:
                PlayAnimByState(newState);
                break;
        }
    }

    void PlayAnimByState(CatState state)
    {
        string animName = "Idle1";

        switch (state)
        {
            case CatState.Idle: animName = GetRandomIdleAnim(); break;
            case CatState.Play: animName = GetWeightedBoxAnim(); break;
            case CatState.Listening: animName = "Idle2"; break;
            case CatState.Thinking: animName = "Waiting"; break;
            case CatState.Talking: animName = "Surprised"; break;
            case CatState.Sleep: animName = "Sleep"; break;
            case CatState.Eat: animName = "Eating"; break;
            case CatState.Happy: animName = "Dance"; break;
            case CatState.Angry: animName = "Sad"; break;
            case CatState.Sad: animName = "Cry"; break;
            case CatState.Sleepy: animName = "Sleepy"; break;
            case CatState.Sick: animName = "LayDown"; break;
            case CatState.None: animName = "DeadCat"; break;
            case CatState.Walk: animName = "Walk"; break;
        }

        _animator.CrossFade(animName, 0.1f);
    }

    // ... (GetRandomIdleAnim, GetWeightedBoxAnim, SetCatVisual 등 기존 함수들은 동일) ...
    string GetRandomIdleAnim() { return (Random.Range(0, 2) == 0) ? "Idle1" : "Idle2"; }
    string GetWeightedBoxAnim()
    {
        float rand = Random.Range(0f, 100f);
        if (rand < 40) return "Box1";
        else if (rand < 80) return "Box2";
        else return "Box3";
    }

    public void SetCatVisual(CatBreed breed, CatPersonality personal)
    {
        string path = $"Animators/{breed}/{personal}";
        RuntimeAnimatorController newController = Resources.Load<RuntimeAnimatorController>(path);

        if (newController != null) _animator.runtimeAnimatorController = newController;
        else Debug.LogError($"외형 파일을 찾을 수 없음: {path}");
    }

    // ==================================================================================
    // ★ [핵심] 데이터 연동 로직
    // ==================================================================================

    public override void UpdateController()
    {
        // 1. 상태별 행동 (FSM)
        switch (_currentState)
        {
            case CatState.Idle: OnUpdateIdle(); break;
            case CatState.Walk: OnUpdateWalk(); break;
            case CatState.Sleep: OnUpdateSleep(); break;
            case CatState.Play: OnUpdatePlay(); break;
            case CatState.Eat: OnUpdateEat(); break;
            case CatState.Sick:
                // 아플 때는 아무것도 안 하고 배고픔만 체크 (회복될 때까지 대기)
                DecreaseStats();
                break;
        }

        // 2. 실시간 스탯 감소 (매 프레임 호출)
        if (_currentState != CatState.Eat) // 먹을 때는 배고픔 안 깎임
        {
            DecreaseStats();
        }
    }

    // 시간 흐름에 따른 스탯 감소
    void DecreaseStats()
    {
        if (Managers.Data == null) return;

        // 배고픔 감소 속도 (기본 1.0, 자고 있으면 0.2로 천천히)
        float hungerSpeed = (_currentState == CatState.Sleep) ? 0.2f : 1.0f;
        float funSpeed = 0.5f;

        // 값 변경
        if (_myCatData.Hunger > 0)
            _myCatData.Hunger -= Time.deltaTime * hungerSpeed;

        if (_myCatData.Fun > 0)
            _myCatData.Fun -= Time.deltaTime * funSpeed;

        // ★ 상태 체크: 배가 너무 고프면 '아픔(Sick)' 상태로 강제 전환
        if (_myCatData.Hunger <= 0 && _currentState != CatState.Sick && _currentState != CatState.Eat)
        {
            Debug.Log("고양이가 너무 배고파서 쓰러졌습니다!");
            CurrentState = CatState.Sick;
        }
    }

    // ★ 밥 먹기 (World_DragFood에서 호출)
    public void EatFood(float amount)
    {
        if (Managers.Data == null) return;

        _myCatData.Hunger += amount;
        if (_myCatData.Hunger > 100) _myCatData.Hunger = 100;

        Debug.Log($"냠냠! 현재 배고픔: {_myCatData.Hunger:F1}");

        // ★ [변경 포인트 4] 저장도 Managers를 통해서
        Managers.Data.SaveGame();

        if (_currentState == CatState.Sick) CurrentState = CatState.Idle;
        else CurrentState = CatState.Eat;
    }

    // ==================================================================================
    // 기존 FSM 업데이트 함수들
    // ==================================================================================

    void OnUpdateEat()
    {
        _stateTimer -= Time.deltaTime;
        if (_stateTimer <= 0) CurrentState = CatState.Idle;
    }

    void OnUpdateIdle()
    {
        _stateTimer -= Time.deltaTime;
        if (_stateTimer <= 0) DecideNextAction();
    }

    void OnUpdateWalk()
    {
        transform.position = Vector3.MoveTowards(transform.position, _targetPosition, _moveSpeed * Time.deltaTime);

        // 방향 전환 (Walk 상태일 때 계속 바라보게)
        if (_targetPosition.x < transform.position.x) _spriteRenderer.flipX = false; // 기본이 왼쪽이면 false
        else _spriteRenderer.flipX = true;

        if (Vector3.Distance(transform.position, _targetPosition) < 0.1f)
            CurrentState = CatState.Idle;
    }

    void OnUpdateSleep()
    {
        _stateTimer -= Time.deltaTime;
        if (_stateTimer <= 0) CurrentState = CatState.Idle;
    }

    void OnUpdatePlay()
    {
        _stateTimer -= Time.deltaTime;
        if (_stateTimer <= 0) CurrentState = CatState.Idle;
    }

    void DecideNextAction()
    {
        // 아픈 상태면 행동 결정 안 함
        if (_myCatData.Hunger <= 0)
        {
            CurrentState = CatState.Sick;
            return;
        }

        int randomDice = Random.Range(0, 100);

        if (randomDice < 50)
        {
            SetRandomTargetPosition();
            CurrentState = CatState.Walk;
        }
        else if (randomDice < 70) CurrentState = CatState.Sleep;
        else if (randomDice < 80) CurrentState = CatState.Play;
        else CurrentState = CatState.Idle;
    }

    void SetRandomTargetPosition()
    {
        float x = Random.Range(_minRoomPos.x, _maxRoomPos.x);
        float y = Random.Range(_minRoomPos.y, _maxRoomPos.y);
        _targetPosition = new Vector3(x, y, 0);

        // 방향 전환 코드는 OnUpdateWalk로 이동하거나 여기서 한 번 설정 (Walk 시작 시점)
    }
}