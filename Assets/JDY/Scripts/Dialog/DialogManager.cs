using System.Collections.Generic;
using UnityEngine;

public class DialogManager : MonoBehaviour
{
    public static DialogManager Instance;

    [SerializeField]
    private DialogDatabase database;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public List<DialogData> GetDialogueGroup(string npcName, DialogType type, string target, int currentPhase)
    {
        List<DialogData> result = new List<DialogData>();

        int targetGroupId = -1;

        foreach (DialogData dialogue in database.dialogues)
        {
            if (dialogue.npcName != npcName)
                continue;

            if (dialogue.type != type)
                continue;

            if (dialogue.target != target)
                continue;

            if (currentPhase < dialogue.fromPhase)
                continue;

            if (currentPhase > dialogue.toPhase)
                continue;

            targetGroupId = dialogue.groupId;
            break;
        }

        if (targetGroupId == -1)
            return result;

        foreach (DialogData dialogue in database.dialogues)
        {
            if (dialogue.groupId == targetGroupId)
            {
                result.Add(dialogue);
            }
        }

        return result;
    }
    public List<DialogData> GetQuestions(string npcName, int phase)
    {
        List<DialogData> questions = new List<DialogData>();

        foreach (DialogData data in database.dialogues)
        {
            if (data.npcName != npcName)
                continue;

            if (data.type != DialogType.Question)
                continue;

            if (data.fromPhase > phase || data.toPhase < phase)
                continue;

            if (string.IsNullOrWhiteSpace(data.target))
                continue;

            questions.Add(data);
        }

        return questions;
    }
}