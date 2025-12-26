using UnityEngine;
using static Define;

public class GameScene : BaseScene
{
    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        Managers.Data.Init();
        Managers.UI.ShowSceneUI<UI_Main>("Prefabs/UI/Scene/UI_Main");

        CatController cat = FindFirstObjectByType<CatController>();
        if (cat != null)
        {
            Managers.Game.MyCat = cat;
            Debug.Log("고양이(CatController) 연결 성공!");
        }
        else
        {
            Debug.LogError("씬에 'CatController'가 붙은 고양이 오브젝트가 없습니다!");
        }

        return true;
    }

    public override void Clear()
    {
    }
}