using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.Rendering.DebugUI;
public class StartManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject settingPanel;
    void Start()
    {
        settingPanel.SetActive(false);
    }
    public void NewStartButton()
    {
        float master = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float bgm = PlayerPrefs.GetFloat("BGMVolume", 1f);
        float sfx = PlayerPrefs.GetFloat("SFXVolume", 1f);
        PlayerPrefs.DeleteAll();
        PlayerPrefs.SetFloat("MasterVolume", master);
        PlayerPrefs.SetFloat("BGMVolume", bgm);
        PlayerPrefs.SetFloat("SFXVolume", sfx);
        PlayerPrefs.Save();
        SceneManager.LoadScene("Opening");
    }
    public void ContinueButton()
    {
        SceneManager.LoadScene("Main");
    }
    public void SettingButton()
    {
        settingPanel.SetActive(true);
    }
    public void SettingExitButton()
    {
        settingPanel.SetActive(false);
    }
    public void ExitButton()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }
}
