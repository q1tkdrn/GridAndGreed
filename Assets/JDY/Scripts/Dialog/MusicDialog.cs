using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using static UnityEngine.Rendering.DebugUI;
public class MusicDialog : MonoBehaviour
{
    [Header("Script")]
    [SerializeField] private DialogUI dialogUI;
    [SerializeField] private QuestionUI questionUI;
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
        string target = PlayerPrefs.GetFloat("MasterVolume", 1f) <= 0.001f || PlayerPrefs.GetFloat("BGMVolume", 1f) <= 0.001f
            ? "(¸¶½ºÅÍ º¼·ý ¶Ç´Â BGM º¼·ýÀÌ 0ÀÏ ¶§)" : "";
        dialogs = DialogManager.Instance.GetDialogueGroup("¸¶¸®°ñµå", DialogType.Welcome, target, currentPhase);
        dialogUI.StartDialog(dialogs, ShowQuestions);
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
