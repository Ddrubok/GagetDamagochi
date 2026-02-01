using System.Collections;
using TMPro;
using UnityEngine;

public class UI_Bubble : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private Vector3 _offset = new Vector3(0, 2.0f, 0);
    [SerializeField] private float _padding = 0.1f;

    private Transform _target;
    private Camera _mainCam;
    private RectTransform _rectTransform;
    private Coroutine _coDespawn; // 실행 중인 코루틴 저장용

    // 풀링에서 재사용될 때 호출됨 (OnEnable 등을 써도 되지만 Init으로 통일)
    public void Init(Transform target)
    {
        _target = target;
        _mainCam = Camera.main;
        _rectTransform = GetComponent<RectTransform>();

        // 피벗 초기화
        if (_rectTransform != null)
            _rectTransform.pivot = new Vector2(0.5f, 0f);
    }

    public void SetText(string message)
    {
        _text.text = message;

        // ★ 핵심: 이미 사라지는 타이머가 돌고 있다면 취소하고 다시 시작!
        if (_coDespawn != null)
        {
            StopCoroutine(_coDespawn);
        }
        _coDespawn = StartCoroutine(CoDespawn(3.0f));
    }

    IEnumerator CoDespawn(float time)
    {
        yield return new WaitForSeconds(time);

        _coDespawn = null; // 코루틴 끝남 표시
        Managers.Object.Despawn(gameObject);
    }

    void LateUpdate()
    {
        if (_target == null) return;

        // ... (기존 위치 및 피벗 계산 로직 동일) ...
        transform.position = _target.position + _offset;

        Vector3 viewportPos = _mainCam.WorldToViewportPoint(_target.position);
        float targetPivotX = 0.5f;

        if (viewportPos.x > 0.8f) targetPivotX = 1.0f;
        else if (viewportPos.x < 0.2f) targetPivotX = 0.0f;

        _rectTransform.pivot = new Vector2(targetPivotX, 0f);
    }
}