using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject settingPanel;
    [Tooltip("비워 두면 ContinueButton()을 호출하는 버튼을 씬에서 자동으로 찾는다.")]
    [SerializeField] private Button continueButton;
    [SerializeField, Range(0f, 1f)] private float disabledContinueAlpha = 0.35f;

    void Start()
    {
        settingPanel.SetActive(false);
        // 랜덤 오프닝 배경, 가운데 로고와 메뉴 배치 (JSW/Script/TitleScreenLayout.cs)
        TitleScreenLayout.Setup(gameObject);
        RefreshContinueButton();
    }

    private void RefreshContinueButton()
    {
        if (continueButton == null) continueButton = FindButtonCalling(nameof(ContinueButton));
        if (continueButton == null) return;

        bool hasProgress = SaveData.HasProgress;
        continueButton.interactable = hasProgress;
        // 버튼 이미지의 전환 설정과 상관없이 비활성 상태가 분명히 보이도록 흐리게 만든다.
        var group = continueButton.GetComponent<CanvasGroup>();
        if (group == null) group = continueButton.gameObject.AddComponent<CanvasGroup>();
        group.alpha = hasProgress ? 1f : disabledContinueAlpha;
        // 비활성 버튼에 마우스를 올려도 커지지 않게 한다.
        var hover = continueButton.GetComponent<UIHoverScale>();
        if (hover != null) hover.enabled = hasProgress;
    }

    private static Button FindButtonCalling(string methodName)
    {
        foreach (var button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                if (button.onClick.GetPersistentMethodName(i) == methodName) return button;
        return null;
    }

    public void NewStartButton()
    {
        // 지울 데이터가 없으면 바로 시작한다.
        if (!SaveData.HasProgress)
        {
            StartNewGame();
            return;
        }

        ModalDialog.Show("새로하기",
            "처음부터 새로 시작할까요?",
            "소울, 구매 내역, 업적, 진행 중인 여정이\n모두 사라져요. 설정은 그대로 남아요.",
            null,
            new ModalDialog.Option("모두 지우고 시작", StartNewGame),
            new ModalDialog.Option("돌아가기", null));
    }

    private static void StartNewGame()
    {
        float master = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float bgm = PlayerPrefs.GetFloat("BGMVolume", 1f);
        float sfx = PlayerPrefs.GetFloat("SFXVolume", 1f);
        PlayerPrefs.DeleteAll();
        PlayerPrefs.SetFloat("MasterVolume", master);
        PlayerPrefs.SetFloat("BGMVolume", bgm);
        PlayerPrefs.SetFloat("SFXVolume", sfx);
        PlayerPrefs.Save();
        // 마을에서 타이틀로 돌아온 경우 인벤토리가 이전 소울/아이템을 기억하고 있으므로 다시 읽는다.
        if (InventoryManager.Instance != null) InventoryManager.Instance.ReloadFromSave();
        SceneManager.LoadScene("Opening");
    }

    public void ContinueButton()
    {
        if (!SaveData.HasProgress) return;
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
