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

    private float _speechTimer = 0f;
    private float _speechInterval = 10.0f;
    [Header("Dialogue Data")]
    [SerializeField] private CatDialogueData _dialogueData;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        _animator = Util.GetOrAddComponent<Animator>(Util.FindChild(gameObject, "Animation", true));
        _spriteRenderer = _animator.GetComponent<SpriteRenderer>();

        if (Managers.Data != null)
        {
            SetCatVisual(_myCatData.Breed, _myCatData.Personality);
            Debug.Log($"[CatController] 저장된 고양이 불러오기: {_myCatData.Name} (배고픔: {_myCatData.Hunger:F1})");
        }
        else
        {
            SetCatVisual(CatBreed.Cheese, CatPersonality.Normal);
        }

        ChangeState(_currentState);

        return true;
    }

    public void ChangeState(CatState newState)
    {

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
                _stateTimer = 3.0f; PlayAnimByState(CatState.Eat);
                break;

            case CatState.Sick:
                PlayAnimByState(CatState.Sick);
                break;

            default:
                PlayAnimByState(newState);
                break;
        }

        if (_dialogueData != null)
        {
            string msg = _dialogueData.GetRandomDialogue(newState, Managers.Game.LoveScore);
            if (!string.IsNullOrEmpty(msg)) ShowBubble(msg);
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


    public override void UpdateController()
    {
        switch (_currentState)
        {
            case CatState.Idle: OnUpdateIdle(); break;
            case CatState.Walk: OnUpdateWalk(); break;
            case CatState.Sleep: OnUpdateSleep(); break;
            case CatState.Play: OnUpdatePlay(); break;
            case CatState.Eat: OnUpdateEat(); break;
            case CatState.Sick:
                DecreaseStats();
                break;
        }

        if (_currentState != CatState.Eat)
        {
            DecreaseStats();
        }
    }

    void DecreaseStats()
    {
        if (Managers.Data == null) return;

        float hungerSpeed = (_currentState == CatState.Sleep) ? 0.2f : 1.0f;
        float funSpeed = 0.5f;

        if (_myCatData.Hunger > 0)
            _myCatData.Hunger -= Time.deltaTime * hungerSpeed;

        if (_myCatData.Fun > 0)
            _myCatData.Fun -= Time.deltaTime * funSpeed;

        if (_myCatData.Hunger <= 0 && _currentState != CatState.Sick && _currentState != CatState.Eat)
        {
            Debug.Log("고양이가 너무 배고파서 쓰러졌습니다!");
            CurrentState = CatState.Sick;
        }
    }

    public void EatFood(float amount)
    {
        if (Managers.Data == null) return;

        _myCatData.Hunger += amount;
        if (_myCatData.Hunger > 100) _myCatData.Hunger = 100;

        Debug.Log($"냠냠! 현재 배고픔: {_myCatData.Hunger:F1}");

        Managers.Data.SaveGame();

        if (_currentState == CatState.Sick) CurrentState = CatState.Idle;
        else CurrentState = CatState.Eat;
    }


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

        if (_targetPosition.x < transform.position.x) _spriteRenderer.flipX = false; else _spriteRenderer.flipX = true;

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

    }

    public void ShowBubble(string message)
    {
        GameObject go = Managers.Object.SpawnEffect("UI/UI_Bubble", transform.position + new Vector3(0, 1.5f, 0), transform, 3.0f);

        if (go != null)
        {
            UI_Bubble bubble = go.GetComponent<UI_Bubble>();

            if (bubble != null)
            {
                bubble.SetText(message);
            }
        }
    }

    void OnMouseDown()
    {
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

        if (_currentState == CatState.Sleep || _currentState == CatState.Sick)
        {
            return;
        }

        PetCat();
    }

    void PetCat()
    {
        Managers.Game.ProcessChat("주인이 나를 쓰다듬어줬어. 기분이 어때?");
    }

    void TryRandomSpeech()
    {
        _speechTimer += Time.deltaTime;

        if (_speechTimer >= _speechInterval)
        {
            _speechTimer = 0f;

            // 30% 확률로 말하기
            if (Random.Range(0, 100) < 30)
            {
                if (_dialogueData != null)
                {
                    int currentLove = Managers.Game.LoveScore;
                    string msg = _dialogueData.GetRandomDialogue(_currentState, currentLove);

                    if (!string.IsNullOrEmpty(msg))
                    {
                        ShowBubble(msg);
                    }
                    else
                    {
                        // 조건에 맞는 대사가 하나도 없을 때 (예외 처리)
                        // ShowBubble("야옹?");
                    }
                }
            }
        }
    }
}