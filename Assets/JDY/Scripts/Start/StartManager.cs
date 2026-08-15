using UnityEngine;
using UnityEngine.SceneManagement;
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
        PlayerPrefs.DeleteAll();
        SceneManager.LoadScene("Opening");
    }
    public void ContinueButton()
    {
        //SceneManager.LoadScene("Opening");
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
