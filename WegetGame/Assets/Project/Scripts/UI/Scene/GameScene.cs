using UnityEngine;
using static Define;

public class GameScene : BaseScene
{
    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        Managers.Data.Init(); 

        return true;
    }

    public override void Clear()
    {
    }
}