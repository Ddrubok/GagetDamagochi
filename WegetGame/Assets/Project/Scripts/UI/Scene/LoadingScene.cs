using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingScene : BaseScene
{
    private UI_Loading _uiLoading;

    public override bool Init()
    {
        if (base.Init() == false) return false;

        SceneType = Define.EScene.LoadingScene;

        _uiLoading = Managers.UI.ShowSceneUI<UI_Loading>("Prefabs/UI/Scene/UI_Loading");

        StartCoroutine(CoLoadingSequence());

        return true;
    }

    private IEnumerator CoLoadingSequence()
    {
        _uiLoading.SetProgress(0.2f, "양파 쿵야 뇌세포 깨우는 중... ");
        yield return new WaitForSeconds(1.0f);

        bool isAiReady = false;
        bool isError = false;

        Managers.Game.InitLocalAI
            (
    onSuccess: () => { isAiReady = true; },
    onError: (err) => { isError = true; Debug.LogError(err); }
);

        _uiLoading.SetProgress(0.85f, "지능(AI 모델)을 다운로드하고 있어양!\n(데이터 환경에 따라 수 분 소요) ");

        while (!isAiReady)
        {
            if (isError)
            {
                _uiLoading.SetProgress(0f, "다운로드 실패! 인터넷을 확인해주세요.");
                yield break;
            }
            yield return null;
        }

        _uiLoading.SetProgress(0.95f, "다운로드 완료! 방으로 들어가는 중... ");

        AsyncOperation asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("GameScene");
        asyncLoad.allowSceneActivation = false;
        while (!asyncLoad.isDone)
        {
            if (asyncLoad.progress >= 0.9f)
            {
                _uiLoading.SetProgress(1.0f, "준비 완료!");
                yield return new WaitForSeconds(0.5f); asyncLoad.allowSceneActivation = true;
            }
            yield return null;
        }
    }

    public override void Clear()
    {
    }
}