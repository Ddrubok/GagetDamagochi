using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_Loading : UI_Scene
{
    enum Sliders { ProgressBar }
    enum Texts { LoadingText }

    private Slider _progressBar;
    private TextMeshProUGUI _loadingText;
    private float _targetProgress = 0f;

    public override bool Init()
    {
        if (base.Init() == false) return false;

        Bind<Slider>(typeof(Sliders));
        Bind<TextMeshProUGUI>(typeof(Texts));

        _progressBar = GetSlider((int)Sliders.ProgressBar);
        _loadingText = GetTextMesh((int)Texts.LoadingText);
        _progressBar.value = 0f;

        // 부드럽게 프로그레스 바가 차오르는 코루틴 실행
        StartCoroutine(CoUpdateProgressBar());

        return true;
    }

    public void SetProgress(float targetProgress, string message)
    {
        _targetProgress = targetProgress;
        if (_loadingText != null) _loadingText.text = message;
    }

    private IEnumerator CoUpdateProgressBar()
    {
        while (true)
        {
            // 현재 값에서 목표 값을 향해 부드럽게 이동 (Lerp)
            _progressBar.value = Mathf.Lerp(_progressBar.value, _targetProgress, Time.deltaTime * 2.0f);
            yield return null;
        }
    }
}