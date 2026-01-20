using UnityEngine;
using System.Collections.Generic;
using static Define;
[CreateAssetMenu(fileName = "CatDialogueData", menuName = "Data/Dialogue")]
public class CatDialogueData : ScriptableObject
{
    public List<DialogueInfo> Dialogues = new List<DialogueInfo>();

    [System.Serializable]
    public class DialogueInfo
    {
        [TextArea] public string Text; public CatState State;
        [Range(-100, 100)] public int MinLove = -100; [Range(-100, 100)] public int MaxLove = 100;
    }

    public string GetRandomDialogue(CatState state, int currentLove)
    {
        List<string> candidates = new List<string>();

        foreach (var info in Dialogues)
        {
            if (info.State != state) continue;

            if (currentLove >= info.MinLove && currentLove <= info.MaxLove)
            {
                candidates.Add(info.Text);
            }
        }

        if (candidates.Count == 0) return null;

        return candidates[Random.Range(0, candidates.Count)];
    }
}