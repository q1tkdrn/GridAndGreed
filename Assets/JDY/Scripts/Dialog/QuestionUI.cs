using System.Collections.Generic;
using UnityEngine;

public class QuestionUI : MonoBehaviour
{
    [SerializeField] private QuestionButton[] questionButtons;
    [SerializeField] private DialogUI dialogUI;
    private string currentNpcName;
    private int currentPhase;
    
    public void ShowQuestions(string npcName, int phase)//1
    {
        currentNpcName = npcName;
        currentPhase = phase;

        List<DialogData> questions = DialogManager.Instance.GetQuestions(npcName, phase);

        for (int i = 0; i < questionButtons.Length; i++)
        {
            if (i < questions.Count)
            {
                questionButtons[i].gameObject.SetActive(true);

                questionButtons[i].SetQuestion(questions[i], this);
            }
            else
            {
                questionButtons[i].gameObject.SetActive(false);
            }
        }
    }

    public void SelectQuestion(DialogData question)//3
    {
        List<DialogData> dialogs = DialogManager.Instance.GetDialogueGroup(currentNpcName, DialogType.Question, question.target, currentPhase);

        if (dialogs.Count == 0)
        {
            Debug.LogWarning("대화 없음");
            return;
        }

        dialogUI.StartDialog(dialogs, () => ShowQuestions(currentNpcName, currentPhase));
    }
}