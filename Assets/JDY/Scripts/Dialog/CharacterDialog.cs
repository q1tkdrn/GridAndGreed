using System.Collections.Generic;
using UnityEngine;
public class CharacterDialog : MonoBehaviour
{
    [Header("Script")]
    [SerializeField] private DialogUI dialogUI;
    [SerializeField] private QuestionUI questionUI;
    [SerializeField] private CharacterSlot characterSlot;
    [SerializeField] private ExitButton exitButton;
    [Header("Test")]
    [SerializeField] private int currentPhase = 0;
    private List<DialogData> dialogs;
    void Start()
    {
        dialogs = DialogManager.Instance.GetDialogueGroup("副府", DialogType.Welcome, "", currentPhase);
        dialogUI.StartDialog(dialogs, ShowQuestions);
    }
    public void ShowQuestions()
    {
        questionUI.ShowQuestions("副府", currentPhase);
    }
    public void StartCharacterDialog()
    {
        dialogs = DialogManager.Instance.GetDialogueGroup("副府", DialogType.Character, characterSlot.character.characterName, currentPhase);
        dialogUI.StartDialog(dialogs, ShowQuestions);
    }
    public void ExitDialog()
    {
        dialogs = DialogManager.Instance.GetDialogueGroup("副府", DialogType.Exit, "", currentPhase);
        dialogUI.StartDialog(dialogs, exitButton.exitButton, false);
    }
}
