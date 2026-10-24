using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Scene-aware Escape menu. The visual hierarchy lives in Resources/UI/PauseMenu.prefab
/// so its button and panel sprites can be replaced without changing this logic.
/// </summary>
[DisallowMultipleComponent]
public sealed class PauseMenuController : MonoBehaviour
{
    private const string PrefabPath = "UI/PauseMenu";
    private const string TitleScene = "Start";
    private const string MainScene = "Main";
    private const string BattleScene = "BattleTemp";

    private static PauseMenuController instance;

    [Header("Menu")]
    public GameObject overlay;
    public GameObject actions;
    public GameObject restartConfirmation;
    public TMP_Text heading;
    public TMP_Text destinationLabel;

    [Header("Buttons")]
    public Button continueButton;
    public Button destinationButton;
    public Button restartButton;
    public Button quitButton;
    public Button confirmRestartButton;
    public Button cancelRestartButton;

    private bool isOpen;
    private bool escapeHeld;
    private float timeScaleBeforePause = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInstance()
    {
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateMenu()
    {
        if (instance != null) return;

        var prefab = Resources.Load<PauseMenuController>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogError("ESC 메뉴 프리팹을 Resources/UI/PauseMenu에서 찾지 못했습니다.");
            return;
        }

        Instantiate(prefab);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (!HasReferences())
        {
            Debug.LogError("ESC 메뉴 프리팹의 UI 참조가 누락되었습니다.", this);
            enabled = false;
            return;
        }

        overlay.SetActive(false);
        ShowActions();

        continueButton.onClick.AddListener(Close);
        destinationButton.onClick.AddListener(GoToDestination);
        restartButton.onClick.AddListener(AskToRestart);
        quitButton.onClick.AddListener(QuitGame);
        confirmRestartButton.onClick.AddListener(RestartJourney);
        cancelRestartButton.onClick.AddListener(ShowActions);

        SceneManager.sceneLoaded += OnSceneLoaded;
        ConfigureForScene(SceneManager.GetActiveScene().name);
    }

    private bool HasReferences()
    {
        return overlay != null && actions != null && restartConfirmation != null
            && heading != null && destinationLabel != null
            && continueButton != null && destinationButton != null
            && restartButton != null && quitButton != null
            && confirmRestartButton != null && cancelRestartButton != null;
    }

    private void OnDestroy()
    {
        if (instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (isOpen) Time.timeScale = timeScaleBeforePause;
        instance = null;
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        bool escapePressed = keyboard != null && keyboard.escapeKey.isPressed;
        if (!escapePressed)
        {
            escapeHeld = false;
            return;
        }
        if (escapeHeld) return;
        escapeHeld = true;
        if (SceneManager.GetActiveScene().name == TitleScene) return;
        // 확인 팝업이 열려 있으면 ESC는 팝업을 닫는 데만 쓴다.
        if (ModalDialog.BlocksEscape) return;

        if (!isOpen) Open();
        else if (restartConfirmation.activeSelf) ShowActions();
        else Close();
    }

    public void Open()
    {
        if (isOpen || SceneManager.GetActiveScene().name == TitleScene) return;

        ConfigureForScene(SceneManager.GetActiveScene().name);
        ShowActions();
        timeScaleBeforePause = Time.timeScale;
        Time.timeScale = 0f;
        overlay.SetActive(true);
        isOpen = true;
    }

    public void Close()
    {
        if (!isOpen) return;

        overlay.SetActive(false);
        ShowActions();
        Time.timeScale = timeScaleBeforePause;
        isOpen = false;
    }

    private void ShowActions()
    {
        actions.SetActive(true);
        restartConfirmation.SetActive(false);
    }

    private void AskToRestart()
    {
        if (!CanRestartBattle()) return;
        actions.SetActive(false);
        restartConfirmation.SetActive(true);
    }

    private void ConfigureForScene(string sceneName)
    {
        bool isBattle = sceneName == BattleScene;
        bool isMain = sceneName == MainScene;

        // 보스와 싸우는 중일 때만 표시한다. (경로 선택/승리 화면/패배 후에는 다시 시작할 전투가 없다)
        restartButton.gameObject.SetActive(isBattle && RunSave.HasCurrentBattle);
        heading.text = isBattle ? "여정 일시 정지" : "잠시 멈춤";
        destinationLabel.text = isMain ? "시작 화면으로" : "메인으로 나가기";

        if (sceneName == TitleScene)
        {
            overlay.SetActive(false);
            isOpen = false;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (isOpen)
        {
            overlay.SetActive(false);
            isOpen = false;
            Time.timeScale = 1f;
        }

        ShowActions();
        ConfigureForScene(scene.name);
    }

    private void GoToDestination()
    {
        string target = SceneManager.GetActiveScene().name == MainScene ? TitleScene : MainScene;
        LeaveForScene(target);
    }

    private static bool CanRestartBattle() =>
        SceneManager.GetActiveScene().name == BattleScene && RunSave.HasCurrentBattle;

    /// <summary>
    /// 현재 보스전만 등장 장면부터 다시 시작한다. 처치한 보스, 편성, 소울은 그대로다.
    /// 씬을 다시 불러와 체크포인트에서 복원하므로 전투 도중 상태가 남지 않는다.
    /// </summary>
    private void RestartJourney()
    {
        if (!CanRestartBattle()) return;
        RunSave.RequestResume();
        LeaveForScene(BattleScene);
    }

    private void SaveBattleFormation()
    {
        if (SceneManager.GetActiveScene().name != BattleScene) return;
        var battle = FindFirstObjectByType<BattleDisplayManager>();
        if (battle != null) battle.SaveFormation();
    }

    private void LeaveForScene(string sceneName)
    {
        SaveBattleFormation();

        overlay.SetActive(false);
        isOpen = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    private void QuitGame()
    {
        SaveBattleFormation();
        PlayerPrefs.Save();
        overlay.SetActive(false);
        isOpen = false;
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
