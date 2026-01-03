using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class CatController : BaseController
{
    private Animator _animator;
    private CatState _currentState = CatState.Idle;

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

        _animator = Util.GetOrAddComponent<Animator>(gameObject);

        SetCatVisual(CatBreed.Cheese, CatPersonality.Normal);

        ChangeState(_currentState);

        return true;
    }

    public void ChangeState(CatState newState)
    {
        string animName = "Idle1";

        switch (newState)
        {
            case CatState.Idle:
                animName = GetRandomIdleAnim();
                break;

            case CatState.Play:
                animName = GetWeightedBoxAnim();
                break;

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
        // 1. 리소스 폴더에서 오버라이드 컨트롤러 불러오기
        // 파일 이름 규칙: "Cat_Cheese", "Cat_Black" 등
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
    }
}