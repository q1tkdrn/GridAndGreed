using System.Collections.Generic;
using UnityEngine;
public class ItemDialog : MonoBehaviour
{
    [Header("Script")]
    [SerializeField] private DialogUI dialogUI;
    [SerializeField] private QuestionUI questionUI;
    [SerializeField] private ItemSlot itemSlot;
    [SerializeField] private ExitButton exitButton;
    [Header("Test")]
    [SerializeField] private int currentPhase = 0;
    private List<DialogData> dialogs;
    void Awake()
    {
        currentPhase = PlayerPrefs.GetInt("currentPhase", 0);
    }
    void Start()
    {
        dialogs = DialogManager.Instance.GetDialogueGroup("아스터", DialogType.Welcome, "", currentPhase);
        dialogUI.StartDialog(dialogs, ShowQuestions);
    }
    public void ShowQuestions()
    {
        questionUI.ShowQuestions("아스터", currentPhase);
    }
    public void StartItemDialog()
    {
        dialogs = DialogManager.Instance.GetDialogueGroup("아스터", DialogType.Item, itemSlot.item.itemName, currentPhase);
        dialogUI.StartDialog(dialogs, ShowQuestions);
    }
    public void ExitDialog()
    {
        dialogs = DialogManager.Instance.GetDialogueGroup("아스터", DialogType.Exit, "", currentPhase);
        dialogUI.StartDialog(dialogs, exitButton.exitButton, false);
    }
}
