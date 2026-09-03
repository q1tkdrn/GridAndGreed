using System.Collections.Generic;
using UnityEngine;
public class MusicDialog : MonoBehaviour
{
    [Header("Script")]
    [SerializeField] private DialogUI dialogUI;
    [SerializeField] private QuestionUI questionUI;
    [SerializeField] private ExitButton exitButton;
    [Header("Test")]
    [SerializeField] private int currentPhase = 0;
    private List<DialogData> dialogs;
    void Start()
    {
        dialogs = DialogManager.Instance.GetDialogueGroup("¸¶¸®°ñµå", DialogType.Welcome, "", currentPhase);
        dialogUI.StartDialog(dialogs, ShowQuestions);
        currentPhase = PlayerPrefs.GetInt("currentPhase", 0);
    }
    public void ShowQuestions()
    {
        questionUI.ShowQuestions("¸¶¸®°ñµå", currentPhase);
    }
    public void StartMusicDialog(string musicName)
    {
        dialogs = DialogManager.Instance.GetDialogueGroup("¸¶¸®°ñµå", DialogType.Music, musicName, currentPhase);
        dialogUI.StartDialog(dialogs, ShowQuestions);
    }
    public void ExitDialog()
    {
        dialogs = DialogManager.Instance.GetDialogueGroup("¸¶¸®°ñµå", DialogType.Exit, "", currentPhase);
        dialogUI.StartDialog(dialogs, exitButton.exitButton, false);
    }
}
