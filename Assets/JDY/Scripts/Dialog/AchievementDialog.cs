using System.Collections.Generic;
using UnityEngine;
public class AchievementDialog : MonoBehaviour
{
    public static AchievementDialog Instance;
    [Header("Script")]
    [SerializeField] private DialogUI dialogUI;
    [SerializeField] private QuestionUI questionUI;
    [SerializeField] private ExitButton exitButton;
    [Header("Test")]
    [SerializeField] private int currentPhase = 0;
    private List<DialogData> dialogs;
    private void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        dialogs = DialogManager.Instance.GetDialogueGroup("아스포델", DialogType.Welcome, "", currentPhase);
        dialogUI.StartDialog(dialogs, ShowQuestions);
    }
    public void ShowQuestions()
    {
        questionUI.ShowQuestions("아스포델", currentPhase);
    }
    public void StartAchievementDialog(AchievementData data)
    {
        string targetName;
        if (AchievementManager.Instance.IsCompleted(data.id))
        {
            targetName = data.title + "(완료 후)";
        }
        else
        {
            targetName = data.title + "(완료 전)";
        }

        dialogs = DialogManager.Instance.GetDialogueGroup("아스포델", DialogType.Achievements, targetName, currentPhase);
        dialogUI.StartDialog(dialogs, ShowQuestions);
    }
    public void ExitDialog()
    {
        dialogs = DialogManager.Instance.GetDialogueGroup("아스포델", DialogType.Exit, "", currentPhase);
        dialogUI.StartDialog(dialogs, exitButton.exitButton, false);
    }
}
