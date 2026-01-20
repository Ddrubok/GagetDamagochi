using TMPro;
using UnityEngine;

public class UI_Bubble : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _text; 

    public void SetText(string message)
    {
        _text.text = message;

        // 3초 뒤에 자동으로 사라짐
        Destroy(gameObject, 3.0f);
    }

    // 말풍선이 고양이를 따라다니게 하려면 Update에 위치 고정 로직 추가 가능
    // 지금은 일단 고양이 머리 위에 고정된 위치에 소환하는 방식으로 진행합니다.
}