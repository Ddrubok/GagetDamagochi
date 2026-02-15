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
    private Coroutine _coDespawn;
    public void Init(Transform target)
    {
        _target = target;
        _mainCam = Camera.main;
        _rectTransform = GetComponent<RectTransform>();

        if (_rectTransform != null)
            _rectTransform.pivot = new Vector2(0.5f, 0f);
    }

    public void SetText(string message)
    {
        _text.text = message;

        if (_coDespawn != null)
        {
            StopCoroutine(_coDespawn);
        }
        _coDespawn = StartCoroutine(CoDespawn(3.0f));
    }

    IEnumerator CoDespawn(float time)
    {
        yield return new WaitForSeconds(time);

        _coDespawn = null; Managers.Object.Despawn(gameObject);
    }

    void LateUpdate()
    {
        if (_target == null) return;

        transform.position = _target.position + _offset;

        Vector3 viewportPos = _mainCam.WorldToViewportPoint(_target.position);
        float targetPivotX = 0.5f;

        if (viewportPos.x > 0.8f) targetPivotX = 1.0f;
        else if (viewportPos.x < 0.2f) targetPivotX = 0.0f;

        _rectTransform.pivot = new Vector2(targetPivotX, 0f);
    }
}