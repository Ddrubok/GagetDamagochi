using System;

[Serializable]
public class CapsuleData
{
    public string state = "IDLE";

    public int hunger = 100;
    public int monsterId = 1;

    public long lastInteractionTime;
}