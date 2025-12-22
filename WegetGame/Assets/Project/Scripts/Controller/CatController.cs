using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define; 

public class CatController : BaseController
{
    private Animator _animator;
    private CatState _currentState = CatState.IDLE_SIT;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        _animator = GetComponent<Animator>();

        ChangeState(CatState.IDLE_SIT);

        return true;
    }

    public void ChangeState(CatState newState)
    {
        if (_animator == null) return;

        _currentState = newState;

        _animator.SetInteger("state", (int)newState);
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