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
    public int isEnding = 0;

    private List<DialogData> dialogs;
    void Awake()
    {
        Instance = this;
        currentPhase = PlayerPrefs.GetInt("currentPhase", 0);
        isEnding = PlayerPrefs.GetInt("IsEnding", 0);
    }
    void Start()
    {
        string npc = isEnding == 2 ? "유" : "아스포델";
       
        dialogs = DialogManager.Instance.GetDialogueGroup(npc, DialogType.Welcome, "", currentPhase);
        dialogUI.StartDialog(dialogs, ShowQuestions);
    }
    public void ShowQuestions()
    {
        string npc = isEnding == 2 ? "유" : "아스포델";
        questionUI.ShowQuestions(npc, currentPhase);
    }
    public void StartAchievementDialog(AchievementData data)
    {
        string title;
        string npc;

        if (isEnding == 2)
        {
            title = (data.id == "ACH-30" || data.id == "ACH-31")? data.title : "(나머지 업적들)";
            npc = "유";
        }
        else
        {
            title = data.title;
            npc = "아스포델";
        }

        string targetName = title + (AchievementManager.Instance.IsCompleted(data.id) ? "(완료 후)" : "(완료 전)");

        dialogs = DialogManager.Instance.GetDialogueGroup(npc, DialogType.Achievements, targetName, currentPhase);
        dialogUI.StartDialog(dialogs, ShowQuestions);
    }
    public void ExitDialog()
    {
        string npc = isEnding == 2 ? "유" : "아스포델";
        dialogs = DialogManager.Instance.GetDialogueGroup(npc, DialogType.Exit, "", currentPhase);
        dialogUI.StartDialog(dialogs, exitButton.exitButton, false);
    }
}
