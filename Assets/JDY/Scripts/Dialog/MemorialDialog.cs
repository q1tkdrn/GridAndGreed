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
    void Awake()
    {
        currentPhase = PlayerPrefs.GetInt("currentPhase", 0);
    }
    void Start()
    {
        string target = PlayerPrefs.GetFloat("MasterVolume", 1f) <= 0.001f || PlayerPrefs.GetFloat("BGMVolume", 1f) <= 0.001f
            ? "(마스터 볼륨 또는 BGM 볼륨이 0일 때)" : "";
        dialogs = DialogManager.Instance.GetDialogueGroup("히아신스", DialogType.Welcome, target, currentPhase);
        dialogUI.StartDialog(dialogs, ShowQuestions);
    }
    public void ShowQuestions()
    {
        questionUI.ShowQuestions("히아신스", currentPhase);
    }
    public void StartMemorialDialog(MemorialButton memorialButton)
    {
        dialogs = DialogManager.Instance.GetDialogueGroup("히아신스", DialogType.Memory, "공통", currentPhase);

        dialogUI.StartDialog(dialogs, () =>{memorialUI.ShowMemorial(memorialButton.memorialData, ShowQuestions);});
    }
    public void ExitDialog()
    {
        dialogs = DialogManager.Instance.GetDialogueGroup("히아신스", DialogType.Exit, "", currentPhase);
        dialogUI.StartDialog(dialogs, exitButton.exitButton, false);
    }
}
