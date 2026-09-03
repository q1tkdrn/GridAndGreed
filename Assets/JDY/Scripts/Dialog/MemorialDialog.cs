using System.Collections.Generic;
using UnityEngine;
public class MemorialDialog : MonoBehaviour
{
    [Header("Script")]
    [SerializeField] private DialogUI dialogUI;
    [SerializeField] private QuestionUI questionUI;
    [SerializeField] private ExitButton exitButton;
    [SerializeField] private MemorialUI memorialUI;
    [Header("Test")]
    [SerializeField] private int currentPhase = 0;
    private List<DialogData> dialogs;
    void Start()
    {
        dialogs = DialogManager.Instance.GetDialogueGroup("히아신스", DialogType.Welcome, "", currentPhase);
        dialogUI.StartDialog(dialogs, ShowQuestions);
    }
    public void ShowQuestions()
    {
        questionUI.ShowQuestions("히아신스", currentPhase);
    }
    public void StartMemorialDialog(int index)
    {
        dialogs = DialogManager.Instance.GetDialogueGroup("히아신스", DialogType.Memory, "공통", currentPhase);

        dialogUI.StartDialog(dialogs, () =>{memorialUI.ShowMemorial(index, ShowQuestions);});
    }
    public void ExitDialog()
    {
        dialogs = DialogManager.Instance.GetDialogueGroup("히아신스", DialogType.Exit, "", currentPhase);
        dialogUI.StartDialog(dialogs, exitButton.exitButton, false);
    }
}
