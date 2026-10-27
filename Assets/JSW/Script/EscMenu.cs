using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// ESC 메뉴. 예시 이미지(Resources/ESC.png, ESC2.png)를 기준으로 코드에서 화면을 만든다.
/// - 마을(Main): 오른쪽 어두운 그라데이션 + 글자 메뉴 (ESC.png)
/// - 전투(BattleTemp): 화면 전체를 어둡게 + 붓터치 해골 버튼 (ESC2.png)
/// - 그 밖의 씬(타이틀, 상점, 오프닝 등)에서는 열리지 않는다.
/// 붓터치 버튼 이미지는 Resources/UI/EscButton.png (9-slice). 원본 이미지가 생기면 이 파일만 교체하면 된다.
/// </summary>
public sealed class EscMenu : MonoBehaviour
{
    private const string TitleScene = "Start";
    private const string MainScene = "Main";
    private const string BattleScene = "BattleTemp";

    private enum Style { None, Village, Battle }
    private enum Page { Menu, ConfirmRetry, Settings }

    private static EscMenu _instance;

    // 예시 이미지(1920x1080) 기준 배치 값.
    private const float VillageColumnX = -240f;   // 오른쪽 끝에서의 거리(글자 중심)
    private const float VillageSpacing = 128f;
    private const float BattleColumnX = -80f;     // 화면 중앙에서의 거리
    private const float BattleSpacing = 135f;
    private const float BattleButtonWidth = 430f;
    private const float BattleButtonHeight = 105f;

    private Canvas _canvas;
    private RectTransform _root;
    private Style _style;
    private Page _page;
    private bool _isOpen;
    private bool _escapeHeld;
    private float _timeScaleBeforeOpen = 1f;

    private TMP_FontAsset _bold;
    private TMP_FontAsset _light;
    private Sprite _brush;
    private Sprite _gear;
    private Sprite _knob;
    private Sprite _villageShade;
    private AudioMixer _mixer;

    public static bool IsOpen => _instance != null && _instance._isOpen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => _instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (_instance != null) return;
        new GameObject("EscMenu").AddComponent<EscMenu>();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 550; // 게임 UI 위, 확인 팝업(600) 아래
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        _root = NewRect("Root", transform);
        Stretch(_root);
        _root.gameObject.SetActive(false);

        _bold = Resources.Load<TMP_FontAsset>("Fonts & Materials/BookkMyungjo_Bold SDF");
        _light = Resources.Load<TMP_FontAsset>("Fonts & Materials/BookkMyungjo_Light SDF");
        _brush = Resources.Load<Sprite>("UI/EscButton");
        _gear = Resources.Load<Sprite>("Images/Main/SettingButton");
        _knob = CreateCircle(64);
        _villageShade = CreateVillageShade();
        var sfx = Resources.Load<GameSfxSettings>("GameSfxSettings");
        if (sfx != null && sfx.output != null) _mixer = sfx.output.audioMixer;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (_instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (_isOpen) Time.timeScale = _timeScaleBeforeOpen;
        _instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!_isOpen) return;
        _isOpen = false;
        _root.gameObject.SetActive(false);
        Time.timeScale = 1f;
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        bool escape = keyboard != null && keyboard.escapeKey.isPressed;
        if (!escape) { _escapeHeld = false; return; }
        if (_escapeHeld) return;
        _escapeHeld = true;

        // 확인 팝업이 열려 있으면 ESC는 팝업을 닫는 데만 쓴다.
        if (ModalDialog.BlocksEscape) return;

        if (_isOpen)
        {
            if (_page == Page.Menu) Close();
            else ShowPage(Page.Menu);
            return;
        }

        var style = StyleFor(SceneManager.GetActiveScene().name);
        if (style == Style.None) return;
        // 마을의 톱니바퀴 설정창이 열려 있으면 ESC는 그 창을 닫는다.
        if (style == Style.Village && CloseVillageSettingsPanel()) return;
        Open(style);
    }

    private static Style StyleFor(string sceneName) => sceneName switch
    {
        MainScene => Style.Village,
        BattleScene => Style.Battle,
        _ => Style.None,
    };

    private void Open(Style style)
    {
        _style = style;
        _timeScaleBeforeOpen = Time.timeScale;
        Time.timeScale = 0f;
        _isOpen = true;
        _root.gameObject.SetActive(true);
        ShowPage(Page.Menu);
    }

    public void Close()
    {
        if (!_isOpen) return;
        _isOpen = false;
        _root.gameObject.SetActive(false);
        Clear();
        Time.timeScale = _timeScaleBeforeOpen;
        SyncVillageSettingsSliders();
    }

    // ───────────────────────────── 화면 구성 ─────────────────────────────

    private void ShowPage(Page page)
    {
        if (_page == Page.Settings && page != Page.Settings) SyncVillageSettingsSliders();
        _page = page;
        Clear();
        BuildBackdrop();
        BuildGear();
        switch (page)
        {
            case Page.Menu: BuildMenu(); break;
            case Page.ConfirmRetry: BuildConfirmRetry(); break;
            case Page.Settings: BuildSettings(); break;
        }
        EventSystem.current?.SetSelectedGameObject(null);
    }

    private void Clear()
    {
        for (int i = _root.childCount - 1; i >= 0; i--) Destroy(_root.GetChild(i).gameObject);
    }

    private void BuildBackdrop()
    {
        var backdrop = NewRect("Backdrop", _root);
        Stretch(backdrop);
        var image = backdrop.gameObject.AddComponent<Image>();
        image.raycastTarget = true; // 메뉴 뒤의 게임 화면 클릭을 막는다.
        if (_style == Style.Village)
        {
            image.sprite = _villageShade;
            image.color = Color.white;
        }
        else
        {
            image.color = new Color(0f, 0f, 0f, 0.74f);
        }
    }

    private void BuildGear()
    {
        if (_gear == null) return;
        var rect = NewRect("Gear", _root);
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(108f, 108f);
        rect.anchoredPosition = new Vector2(-88f, -75f);
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = _gear;
        image.preserveAspect = true;
        AddButton(rect.gameObject, image, () => ShowPage(_page == Page.Settings ? Page.Menu : Page.Settings), 0.85f);
    }

    private void BuildMenu()
    {
        var items = new List<(string label, Action action)> { ("계속하기", Close) };
        if (_style == Style.Battle && RunSave.HasCurrentBattle) items.Add(("재도전", () => ShowPage(Page.ConfirmRetry)));
        items.Add(("타이틀화면으로", () => LeaveTo(TitleScene)));
        items.Add(("설정", () => ShowPage(Page.Settings)));
        items.Add(("종료", Quit));

        float firstY = (items.Count - 1) * Spacing * 0.5f + MenuCenterY;
        for (int i = 0; i < items.Count; i++)
            MakeMenuButton(items[i].label, firstY - Spacing * i, items[i].action);
    }

    private void BuildConfirmRetry()
    {
        MakeLabel("보스전을 다시 시작할까요?", _bold, 50f, Color.white, new Vector2(ColumnX, 190f), 900f);
        MakeLabel("보스 등장 장면부터 다시 싸워요.\n처치한 보스와 편성은 그대로예요.", _light, 32f,
            new Color(1f, 1f, 1f, 0.8f), new Vector2(ColumnX, 95f), 900f, 100f);
        MakeMenuButton("재도전", -40f, RetryBattle);
        MakeMenuButton("돌아가기", -40f - Spacing, () => ShowPage(Page.Menu));
    }

    private void BuildSettings()
    {
        MakeLabel("설정", _bold, 56f, Color.white, new Vector2(SettingsRowX, 250f), 600f);
        MakeVolumeRow("마스터", "MasterVolume", 120f);
        MakeVolumeRow("배경음", "BGMVolume", 20f);
        MakeVolumeRow("효과음", "SFXVolume", -80f);
        MakeMenuButton("돌아가기", -230f, () => ShowPage(Page.Menu));
    }

    // 설정 화면의 슬라이더 줄 중심. 마을은 오른쪽 끝에 붙어 있어 안쪽으로 당긴다.
    private float SettingsRowX => _style == Style.Village ? ColumnX - 150f : ColumnX;

    private float Spacing => _style == Style.Village ? VillageSpacing : BattleSpacing;
    private float ColumnX => _style == Style.Village ? VillageColumnX : BattleColumnX;
    private float MenuCenterY => _style == Style.Village ? -40f : -30f;
    private Vector2 ColumnAnchor => _style == Style.Village ? new Vector2(1f, 0.5f) : new Vector2(0.5f, 0.5f);

    private void MakeMenuButton(string label, float y, Action action)
    {
        if (_style == Style.Village) MakeVillageButton(label, y, action);
        else MakeBattleButton(label, y, action);
    }

    // 마을: 글자만 있는 메뉴 (ESC.png)
    private void MakeVillageButton(string label, float y, Action action)
    {
        var rect = NewRect(label, _root);
        rect.anchorMin = rect.anchorMax = ColumnAnchor;
        rect.sizeDelta = new Vector2(420f, 96f);
        rect.anchoredPosition = new Vector2(ColumnX, y);
        var text = AddText(rect, label, _bold, 50f, Color.white);
        text.characterSpacing = SpacingFor(label);
        AddButton(rect.gameObject, text, action, 0.92f);
    }

    // 전투: 붓터치 해골 버튼 (ESC2.png)
    private void MakeBattleButton(string label, float y, Action action)
    {
        var rect = NewRect(label, _root);
        rect.anchorMin = rect.anchorMax = ColumnAnchor;
        float width = label.Length > 4 ? BattleButtonWidth + 90f : BattleButtonWidth;
        rect.sizeDelta = new Vector2(width, BattleButtonHeight);
        rect.anchoredPosition = new Vector2(ColumnX, y);
        var image = rect.gameObject.AddComponent<Image>();
        if (_brush != null)
        {
            image.sprite = _brush;
            image.type = Image.Type.Sliced;
        }
        else
        {
            image.color = new Color(0.9f, 0.9f, 0.9f, 0.95f);
        }

        var textRect = NewRect("Text", rect);
        Stretch(textRect);
        // 왼쪽 해골을 피해 글자를 놓는다.
        textRect.offsetMin = new Vector2(92f, 0f);
        textRect.offsetMax = new Vector2(-48f, 0f);
        var text = AddText(textRect, label, _bold, 46f, new Color(0.08f, 0.08f, 0.08f));
        text.characterSpacing = SpacingFor(label);
        text.raycastTarget = false;
        // 긴 문구가 해골이나 붓 끝에 닿지 않게 칸에 맞춰 줄인다.
        text.enableAutoSizing = true;
        text.fontSizeMin = 30f;
        text.fontSizeMax = 46f;
        AddButton(rect.gameObject, image, action, 0.96f);
    }

    // 예시 이미지처럼 짧은 단어는 글자 사이를 넓게 벌린다.
    private static float SpacingFor(string label) => label.Length switch
    {
        2 => 40f,
        3 => 55f,
        4 => 14f,
        _ => 0f,
    };

    private TextMeshProUGUI MakeLabel(string value, TMP_FontAsset font, float size, Color color, Vector2 pos, float width, float height = 80f)
    {
        var rect = NewRect("Label", _root);
        rect.anchorMin = rect.anchorMax = ColumnAnchor;
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = pos;
        var text = AddText(rect, value, font, size, color);
        text.raycastTarget = false;
        return text;
    }

    private void MakeVolumeRow(string label, string key, float y)
    {
        var textColor = new Color(1f, 1f, 1f, 0.9f);
        float rowX = SettingsRowX;

        var labelRect = NewRect(label, _root);
        labelRect.anchorMin = labelRect.anchorMax = ColumnAnchor;
        labelRect.sizeDelta = new Vector2(150f, 60f);
        labelRect.anchoredPosition = new Vector2(rowX - 190f, y);
        var text = AddText(labelRect, label, _light, 34f, textColor);
        text.alignment = TextAlignmentOptions.Right;
        text.raycastTarget = false;

        var slider = MakeSlider(new Vector2(rowX + 60f, y));
        slider.SetValueWithoutNotify(PlayerPrefs.GetFloat(key, 1f));
        slider.onValueChanged.AddListener(value => SetVolume(key, value));
    }

    private Slider MakeSlider(Vector2 pos)
    {
        var rect = NewRect("Slider", _root);
        rect.anchorMin = rect.anchorMax = ColumnAnchor;
        rect.sizeDelta = new Vector2(320f, 40f);
        rect.anchoredPosition = pos;

        var bar = NewRect("Bar", rect);
        bar.anchorMin = new Vector2(0f, 0.5f);
        bar.anchorMax = new Vector2(1f, 0.5f);
        bar.sizeDelta = new Vector2(0f, 6f);
        bar.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.25f);

        var fillArea = NewRect("Fill Area", rect);
        fillArea.anchorMin = new Vector2(0f, 0.5f);
        fillArea.anchorMax = new Vector2(1f, 0.5f);
        fillArea.sizeDelta = new Vector2(0f, 6f);
        var fill = NewRect("Fill", fillArea);
        Stretch(fill);
        fill.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.9f);

        var handleArea = NewRect("Handle Slide Area", rect);
        Stretch(handleArea);
        var handle = NewRect("Handle", handleArea);
        // 슬라이더가 손잡이의 세로를 부모 높이(40)에 맞춰 늘리므로 -10을 주어 30x30 원으로 만든다.
        handle.sizeDelta = new Vector2(30f, -10f);
        var handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.sprite = _knob;

        var slider = rect.gameObject.AddComponent<Slider>();
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        var nav = slider.navigation;
        nav.mode = Navigation.Mode.None;
        slider.navigation = nav;
        return slider;
    }

    // ───────────────────────────── 동작 ─────────────────────────────

    private void RetryBattle()
    {
        if (SceneManager.GetActiveScene().name != BattleScene || !RunSave.HasCurrentBattle) return;
        // 체크포인트에서 현재 보스전을 등장 장면부터 다시 시작한다. (처치한 보스, 편성, 소울 유지)
        RunSave.RequestResume();
        LeaveTo(BattleScene);
    }

    private void LeaveTo(string sceneName)
    {
        SaveBattleFormation();
        _isOpen = false;
        _root.gameObject.SetActive(false);
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    private void Quit()
    {
        SaveBattleFormation();
        PlayerPrefs.Save();
        Close();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static void SaveBattleFormation()
    {
        if (SceneManager.GetActiveScene().name != BattleScene) return;
        var battle = FindFirstObjectByType<BattleDisplayManager>();
        if (battle != null) battle.SaveFormation();
    }

    // SettingManager와 같은 방식으로 믹서에 적용하고 저장한다.
    private void SetVolume(string key, float value)
    {
        if (_mixer != null) _mixer.SetFloat(key, value <= 0.001f ? -80f : Mathf.Log10(value) * 20f);
        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();
        if (key == "MasterVolume" && value <= 0.001f && AchievementManager.Instance != null)
            AchievementManager.Instance.AddProgress("ACH-27", 1);
    }

    // 마을 톱니바퀴 설정창(StartManager.settingPanel)
    private static GameObject VillageSettingsPanel()
    {
        var start = FindFirstObjectByType<StartManager>();
        if (start == null) return null;
        var field = typeof(StartManager).GetField("settingPanel", BindingFlags.Instance | BindingFlags.NonPublic);
        return field?.GetValue(start) as GameObject;
    }

    private static bool CloseVillageSettingsPanel()
    {
        var panel = VillageSettingsPanel();
        if (panel == null || !panel.activeInHierarchy) return false;
        FindFirstObjectByType<StartManager>().SettingExitButton();
        return true;
    }

    // ESC에서 바꾼 볼륨이 마을 톱니바퀴 설정창의 슬라이더에도 보이도록 맞춘다.
    private static void SyncVillageSettingsSliders()
    {
        var settings = FindFirstObjectByType<SettingManager>(FindObjectsInactive.Include);
        if (settings == null) return;
        foreach (var (field, key) in new[] { ("masterSlider", "MasterVolume"), ("bgmSlider", "BGMVolume"), ("sfxSlider", "SFXVolume") })
        {
            var slider = typeof(SettingManager).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(settings) as Slider;
            if (slider != null) slider.SetValueWithoutNotify(PlayerPrefs.GetFloat(key, 1f));
        }
    }

    // ───────────────────────────── 공용 ─────────────────────────────

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static TextMeshProUGUI AddText(RectTransform rect, string value, TMP_FontAsset font, float size, Color color)
    {
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    private static void AddButton(GameObject target, Graphic graphic, Action action, float normalTint)
    {
        var button = target.AddComponent<Button>();
        button.targetGraphic = graphic;
        var colors = button.colors;
        colors.normalColor = new Color(normalTint, normalTint, normalTint, 1f);
        colors.highlightedColor = Color.white;
        colors.selectedColor = new Color(normalTint, normalTint, normalTint, 1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        var nav = button.navigation;
        nav.mode = Navigation.Mode.None;
        button.navigation = nav;
        button.onClick.AddListener(() => action?.Invoke());
        UIHoverScale.Attach(target);
    }

    private static Sprite CreateCircle(int size)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "EscKnob" };
        float r = size * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(r - d)));
            }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    // 왼쪽은 그대로 두고 오른쪽으로 갈수록 어두워지는 그라데이션 (ESC.png)
    private static Sprite CreateVillageShade()
    {
        const int width = 256;
        var texture = new Texture2D(width, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "EscVillageShade" };
        for (int x = 0; x < width; x++)
        {
            float t = x / (width - 1f);
            float alpha = Mathf.Lerp(0.08f, 0.94f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 0.82f, t)));
            texture.SetPixel(x, 0, new Color(0f, 0f, 0f, alpha));
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, 1), new Vector2(0.5f, 0.5f));
    }
}
