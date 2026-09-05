using System.Collections.Generic;
using UnityEngine;
public class SkinDialog : MonoBehaviour
{
    [Header("Script")]
    [SerializeField] private DialogUI dialogUI;
    [SerializeField] private QuestionUI questionUI;
    [SerializeField] private ExitButton exitButton;
    [SerializeField] private SkinSlot skinSlot;
    [Header("Test")]
    public int currentPhase = 0;
    private List<DialogData> dialogs;
    void Awake()
    {
        currentPhase = PlayerPrefs.GetInt("currentPhase", 0);
    }
    void Start()
    {
        dialogs = DialogManager.Instance.GetDialogueGroup("오키드", DialogType.Welcome, "", currentPhase);
        dialogUI.StartDialog(dialogs, ShowQuestions);
    }
    public void ShowQuestions()
    {
        questionUI.ShowQuestions("오키드", currentPhase);
    }
    public void StartSkinDialog()
    {
        dialogs = DialogManager.Instance.GetDialogueGroup("오키드", DialogType.Skin, skinSlot.skinName, currentPhase);

        dialogUI.StartDialog(dialogs, ShowQuestions);
    }
    public void ExitDialog()
    {
        dialogs = DialogManager.Instance.GetDialogueGroup("오키드", DialogType.Exit, "", currentPhase);
        dialogUI.StartDialog(dialogs, exitButton.exitButton, false);
    }
}
