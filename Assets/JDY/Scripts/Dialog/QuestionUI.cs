using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuestionUI : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private QuestionButton questionButtonPrefab;
    [Header("Script")]
    [SerializeField] private DialogUI dialogUI;
    [Header("UI")]
    [SerializeField] private Transform questionContent;

    private string currentNpcName;
    private int currentPhase;
    private int lightQuestionCount = 0;
    public void ShowQuestions(string npcName, int phase)
    {
        currentNpcName = npcName;
        currentPhase = phase;
        lightQuestionCount = PlayerPrefs.GetInt("lightQuestionCount", 0);
        int buttonCount = 0;
        foreach (Transform child in questionContent)
        {
            Destroy(child.gameObject);
        }

        List<DialogData> questions = DialogManager.Instance.GetQuestions(npcName, phase);

        foreach (DialogData question in questions)
        {
            if (question.target == "(빛에 대한 이야기) 선택지를 10번 반복했을 때.")
                continue;
            if (question.target == "(빛에 대한 이야기)" && lightQuestionCount >= 11)
            {
                continue;
            }
            
            QuestionButton button = Instantiate(questionButtonPrefab, questionContent);
            button.SetQuestion(question, this);
            buttonCount++;
        }
        RectTransform contentRect = questionContent.GetComponent<RectTransform>();
        VerticalLayoutGroup layout = questionContent.GetComponent<VerticalLayoutGroup>();
        float buttonHeight = questionButtonPrefab.GetComponent<RectTransform>().rect.height;
        float totalButtonHeight = buttonHeight * buttonCount;

        float spacing = buttonCount > 0 ? (contentRect.rect.height - totalButtonHeight) / (buttonCount + 1) : 0;
        spacing = Mathf.Max(0, spacing);

        layout.padding.top = Mathf.RoundToInt(spacing);
        layout.padding.bottom = Mathf.RoundToInt(spacing);
        layout.spacing = spacing;
    }

    public void SelectQuestion(DialogData question)
    {
        lightQuestionCount = PlayerPrefs.GetInt("lightQuestionCount", 0);
        string target = question.target;
        if (target == "(빛에 대한 이야기)")
        {
            lightQuestionCount++;
            PlayerPrefs.SetInt("lightQuestionCount", lightQuestionCount);
            PlayerPrefs.Save();
            if (lightQuestionCount >= 11)
            {
                target = "(빛에 대한 이야기) 선택지를 10번 반복했을 때.";

            }
        }
        List<DialogData> dialogs =
            DialogManager.Instance.GetDialogueGroup(currentNpcName, DialogType.Question, target, currentPhase);

        if (dialogs.Count == 0)
        {
            Debug.LogWarning("대화 없음");
            return;
        }

        dialogUI.StartDialog(dialogs,() => ShowQuestions(currentNpcName, currentPhase));
    }
}