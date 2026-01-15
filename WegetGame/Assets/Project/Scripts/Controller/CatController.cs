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
    [SerializeField] private Vector2 _minRoomPos = new Vector2(-2, -2); [SerializeField] private Vector2 _maxRoomPos = new Vector2(2, 2);
    private float _stateTimer = 0f; private Vector3 _targetPosition; private SpriteRenderer _spriteRenderer;
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

        _animator = Util.GetOrAddComponent<Animator>(Util.FindChild(gameObject, "Animation", true));

        _spriteRenderer = _animator.GetComponent<SpriteRenderer>();
        SetCatVisual(CatBreed.Cheese, CatPersonality.Normal);

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
            case CatState.Idle:
                animName = GetRandomIdleAnim();
                break;

            case CatState.Play:
                animName = GetWeightedBoxAnim();
                break;

            case CatState.Listening: animName = "Idle2"; 
                break;
            case CatState.Thinking: animName = "Waiting"; 
                break;
            case CatState.Talking: animName = "Surprised"; 
                break;

            case CatState.Sleep: animName = "Sleep"; 
                break;
            case CatState.Eat: animName = "Eating"; 
                break;

            case CatState.Happy: animName = "Dance"; 
                break;
            case CatState.Angry: animName = "Sad"; 
                break;

            case CatState.Sad: animName = "Cry"; 
                break;
            case CatState.Sleepy: animName = "Sleepy"; 
                break;

            case CatState.Sick: animName = "LayDown"; 
                break;
            case CatState.None: animName = "DeadCat";
                break;

            case CatState.Walk: animName = "Walk"; 
                break;
        }

        _animator.CrossFade(animName, 0.1f);
    }

    string GetRandomIdleAnim()
    {
        return (Random.Range(0, 2) == 0) ? "Idle1" : "Idle2";
    }

    string GetWeightedBoxAnim()
    {
        float rand = Random.Range(0f, 100f);
        if (rand < 40)
        {
            return "Box1";
        }
        else if (rand < 80)
        {
            return "Box2";
        }
        else
        {
            return "Box3";
        }
    }

    public void SetCatVisual(CatBreed breed, CatPersonality personal)
    {
        string path = $"Animators/{breed}/{personal}";

        RuntimeAnimatorController newController = Resources.Load<RuntimeAnimatorController>(path);

        if (newController != null)
        {
            _animator.runtimeAnimatorController = newController;
            Debug.Log($"고양이 외형 변경 완료: {breed}");
        }
        else
        {
            Debug.LogError($"외형 파일을 찾을 수 없음: {path}");
        }
    }




    public void PlayAction(string triggerName)
    {
        if (_animator == null) return;
        _animator.SetTrigger(triggerName);
    }

    public override void UpdateController()
    {
        switch (_currentState)
        {
            case CatState.Idle:
                OnUpdateIdle();
                break;
            case CatState.Walk:
                OnUpdateWalk();
                break;
            case CatState.Sleep:
                OnUpdateSleep();
                break;
            case CatState.Play:
                OnUpdatePlay();
                break;
        }
    }

    void OnUpdateIdle()
    {
        _stateTimer -= Time.deltaTime;
        if (_stateTimer <= 0) DecideNextAction();
    }

    void OnUpdateWalk()
    {
        transform.position = Vector3.MoveTowards(transform.position, _targetPosition, _moveSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, _targetPosition) < 0.1f)
        {
            CurrentState = CatState.Idle;
        }
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

        //bool isMovingLeft = _targetPosition.x < transform.position.x;
        //if (isMovingLeft)
        //    _spriteRenderer.flipX = true;  
        //else
        //    _spriteRenderer.flipX = false;  
    }
}