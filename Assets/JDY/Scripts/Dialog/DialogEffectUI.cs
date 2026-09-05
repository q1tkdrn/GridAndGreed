using TMPro;
using UnityEngine;
using UnityEngine.Audio;

public class DialogEffectUI : MonoBehaviour
{
    [Header("Script")]
    [SerializeField] private DialogUI dialogUI;
    [Header("UI")]
    [SerializeField] private GameObject fadePanel;
    [SerializeField] private TMP_Text centerText;
    [Header("Audio")]
    [SerializeField] private AudioMixer mixer;

    private TMP_Text dialogText;
    void Start()
    {
        if(fadePanel != null)
            fadePanel.SetActive(false);
    }
    public void MuteMusic()
    {
        mixer.SetFloat("BGMVolume", -80f);
    }
    public void ResumeMusic()
    {
        float volume = PlayerPrefs.GetFloat("BGMVolume", 1f);

        if (volume <= 0.001f)
            mixer.SetFloat("BGMVolume", -80f);
        else
            mixer.SetFloat("BGMVolume", Mathf.Log10(volume) * 20f);
    }
    public void FadeToBlack(string text)
    {
        fadePanel.SetActive(true);
        centerText.gameObject.SetActive(true);
        dialogText = dialogUI.dialogText;
        dialogUI.dialogText = centerText;
    }
    public void Clear()
    {
        fadePanel.SetActive(false);
        centerText.gameObject.SetActive(false);
        dialogUI.dialogText = dialogText;
    }
}