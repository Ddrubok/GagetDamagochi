using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class UI_ButtonAnimation : MonoBehaviour
{
    [SerializeField] private AnimationCurve _animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float _duration = 0.1f;

    private Coroutine _currentCoroutine;
    private Vector3 _originalScale;

    private void Awake()
    {
        _originalScale = transform.localScale;
    }

    private void Start()
    {
        // 기존 바인딩 방식 유지
        gameObject.BindEvent(ButtonPointerDownAnimation, type: Define.EUIEvent.PointerDown);
        gameObject.BindEvent(ButtonPointerUpAnimation, type: Define.EUIEvent.PointerUp);
    }

    public void ButtonPointerDownAnimation(PointerEventData evt)
    {
        PlayAnimation(0.85f);
    }

    public void ButtonPointerUpAnimation(PointerEventData evt)
    {
        PlayAnimation(1f);
    }

    private void PlayAnimation(float targetScaleMultiplier)
    {
        if (_currentCoroutine != null)
            StopCoroutine(_currentCoroutine);

        _currentCoroutine = StartCoroutine(CoScaleAnimation(targetScaleMultiplier));
    }

    private IEnumerator CoScaleAnimation(float targetScaleMultiplier)
    {
        Vector3 startScale = transform.localScale;
        Vector3 endScale = _originalScale * targetScaleMultiplier;
        float time = 0f;

        while (time < _duration)
        {
            time += Time.unscaledDeltaTime;
            float t = time / _duration;
            float curveValue = _animationCurve.Evaluate(t);
            transform.localScale = Vector3.LerpUnclamped(startScale, endScale, curveValue);

            yield return null;
        }

        transform.localScale = endScale;
        _currentCoroutine = null;
    }
}