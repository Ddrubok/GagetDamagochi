using System.Collections;
using TMPro;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class UI_Bubble : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private Vector3 _offset = new Vector3(0, 2.0f, 0); // 머리 위 높이
    [SerializeField] private float _padding = 0.1f;

    private Transform _target; // 따라다닐 대상 (고양이)
    private Camera _mainCam;
    private RectTransform _rectTransform;

    bool _enabled = false;
    public void Init(Transform _tr)
    {
        if (_enabled)
            return;

        _enabled = true;

        _target = _tr;
        _mainCam = Camera.main;
        _rectTransform = GetComponent<RectTransform>();
    }

    public void SetText(string message)
    {
        _text.text = message;

        StartCoroutine(CoDespawn(3.0f));
    }


    IEnumerator CoDespawn(float time)
    {
        yield return new WaitForSeconds(time);

        Managers.Object.Despawn(gameObject);
    }
    void LateUpdate()
    {
        if (_target == null) return;

        // 1. 고양이 위치 따라가기 (기본)
        transform.position = _target.position + _offset;

        // 2. 고양이가 화면의 어디에 있는지 확인 (0:왼쪽 끝, 1:오른쪽 끝)
        Vector3 viewportPos = _mainCam.WorldToViewportPoint(_target.position);

        // 3. 스마트 피벗 변경 (핵심!)
        // 화면 오른쪽(0.8 이상)에 있으면 -> 피벗을 오른쪽(1)으로 설정 -> 말풍선이 왼쪽으로 자라남
        // 화면 왼쪽(0.2 이하)에 있으면 -> 피벗을 왼쪽(0)으로 설정 -> 말풍선이 오른쪽으로 자라남
        // 그 외(가운데) -> 피벗을 가운데(0.5)

        float targetPivotX = 0.5f; // 기본 가운데

        if (viewportPos.x > 0.8f) targetPivotX = 1.0f;      // 오른쪽 벽 근처
        else if (viewportPos.x < 0.2f) targetPivotX = 0.0f; // 왼쪽 벽 근처

        // 부드럽게 변경하고 싶다면 Lerp를 쓰지만, 글자가 흔들릴 수 있으니 바로 대입 추천
        _rectTransform.pivot = new Vector2(targetPivotX, 0f); // Y는 0(아래) 고정
    }
}