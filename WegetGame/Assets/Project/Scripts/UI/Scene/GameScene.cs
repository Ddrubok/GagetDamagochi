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

        return true;
    }

    public override void Clear()
    {
    }
}