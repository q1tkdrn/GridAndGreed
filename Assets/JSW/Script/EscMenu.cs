using System;
using System.Collections.Generic;
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
/// - 타이틀: ESC로는 열리지 않고, '환경설정' 버튼이 설정 화면만 바로 띄운다 (OpenSettings).
/// - 그 밖의 씬(상점, 오프닝, 튜토리얼 등)에서는 열리지 않는다.
/// 붓터치 버튼 이미지는 Resources/UI/EscButton.png (9-slice). 원본 이미지가 생기면 이 파일만 교체하면 된다.
/// </summary>
public sealed class EscMenu : MonoBehaviour
{
    private const string TitleScene = "Start";
    private const string MainScene = "Main";
    private const string BattleScene = "BattleTemp";

    private enum Style { None, Village, Battle }
    private enum Page { Menu, Confirm, Settings, Credit }

    private static EscMenu _instance;

    // 예시 이미지(1920x1080) 기준 배치 값.
    private const float VillageColumnX = -240f;   // 오른쪽 끝에서의 거리(글자 중심)
    private const float VillageSpacing = 128f;
    private const float BattleColumnX = 0f;       // 화면 중앙에서의 거리 (ESC_조정.png: 가운데 정렬)
    private const float BattleSpacing = 135f;
    private const float BattleButtonWidth = 430f;
    private const float BattleButtonHeight = 105f;
    private const float BattleLongWidth = 470f;   // 긴 문구(타이틀화면으로) 버튼의 칸 너비
    private const float BattleLongScale = 1.13f;  // 긴 문구 버튼은 해골까지 통째로 키운다 (ESC_조정.png)
    private const float BattleGap = 30f;          // 버튼 사이 빈칸

    private Canvas _canvas;
    private RectTransform _root;
    private Style _style;
    private Page _page;
    private bool _isOpen;
    private bool _escapeHeld;
    private float _timeScaleBeforeOpen = 1f;
    private bool _settingsOnly;   // 타이틀에서 연 설정 화면: 메뉴로 돌아가지 않고 바로 닫는다

    // 확인 팝업(Page.Confirm)에 띄울 내용. 전투 메뉴의 재도전·타이틀화면으로·종료가 쓴다. (2026-11-05)
    private string _confirmTitle;
    private string _confirmBody;
    private string _confirmLabel;
    private Action _confirmAction;
    private const float ConfirmWidth = 860f;
    private const float ConfirmHeight = 400f;
    private const float ConfirmButtonWidth = 360f;
    private Sprite _dialogFrame;

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

        _bold = GameFonts.Bold;
        _light = GameFonts.Body;
        _brush = Resources.Load<Sprite>("UI/EscButton");
        _gear = Resources.Load<Sprite>("Images/Main/SettingButton");
        _knob = CreateCircle(64);
        var frames = Resources.LoadAll<Sprite>("대사창");   // 분필 테두리 틀 (9-slice)
        _dialogFrame = frames.Length > 0 ? frames[0] : null;
        _villageShade = CreateVillageShade();
        var sfx = Resources.Load<GameSfxSettings>("GameSfxSettings");
        if (sfx != null && sfx.output != null) _mixer = sfx.output.audioMixer;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // 옛 톱니바퀴 설정창(SettingManager)이 하던 일: 게임을 켤 때 저장된 음량을 믹서에 적용한다.
    // 믹서 값은 Awake에서 바꾸면 무시될 수 있어 Start에서 한다.
    private void Start()
    {
        foreach (var key in new[] { "MasterVolume", "BGMVolume", "SFXVolume" })
            if (_mixer != null) { float v = PlayerPrefs.GetFloat(key, 1f); _mixer.SetFloat(key, v <= 0.001f ? -80f : Mathf.Log10(v) * 20f); }
    }

    /// <summary>타이틀의 '환경설정' 버튼: 마을 ESC 설정 화면을 그대로 띄운다.</summary>
    public static void OpenSettings()
    {
        if (_instance == null) Create();
        if (_instance == null || _instance._isOpen) return;
        _instance._settingsOnly = true;
        _instance.Open(Style.Village, Page.Settings);
    }

    /// <summary>화면 톱니바퀴(GearHud)를 눌렀을 때: ESC를 누른 것처럼 이 씬의 메뉴를 연다. 열 수 없는 씬이면 false.</summary>
    public static bool OpenFromHud()
    {
        if (_instance == null || _instance._isOpen) return false;
        if (ModalDialog.BlocksEscape) return false;
        // 톱니는 여정 지도 위에도 있다 (JourneyMap.BuildGear). 지도는 메뉴 뒤에 그대로 남는다. (2026-11-05)
        var style = StyleFor(SceneManager.GetActiveScene().name);
        if (style == Style.None) return false;
        _instance.Open(style);
        return true;
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
        // 메뉴가 열려 있으면 메뉴부터 닫는다. (여정 지도 위에서 톱니로 연 경우 지도는 남는다)
        if (_isOpen)
        {
            if (_page == Page.Credit) ShowPage(Page.Settings);
            else if (_page == Page.Menu || _settingsOnly) Close();
            else ShowPage(Page.Menu);
            return;
        }

        // 여정 지도가 떠 있으면 ESC는 지도를 닫는다. (JourneyMap)
        if (JourneyMap.IsOpen) { JourneyMap.Close(); return; }

        var style = StyleFor(SceneManager.GetActiveScene().name);
        if (style == Style.None) return;
        Open(style);
    }

    private static Style StyleFor(string sceneName) => sceneName switch
    {
        MainScene => Style.Village,
        BattleScene => Style.Battle,
        _ => Style.None,
    };

    private void Open(Style style, Page page = Page.Menu)
    {
        _style = style;
        _timeScaleBeforeOpen = Time.timeScale;
        Time.timeScale = 0f;
        _isOpen = true;
        _root.gameObject.SetActive(true);
        ShowPage(page);
    }

    public void Close()
    {
        if (!_isOpen) return;
        _isOpen = false;
        _root.gameObject.SetActive(false);
        Clear();
        _settingsOnly = false;
        Time.timeScale = _timeScaleBeforeOpen;
    }

    // ───────────────────────────── 화면 구성 ─────────────────────────────

    private void ShowPage(Page page)
    {
        _page = page;
        Clear();
        BuildBackdrop();
        if (!_settingsOnly && page != Page.Credit) BuildGear();
        switch (page)
        {
            case Page.Menu: BuildMenu(); break;
            case Page.Confirm: BuildMenu(); BuildConfirm(); break;
            case Page.Settings: BuildSettings(); break;
            case Page.Credit: BuildCredit(); break;
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

    // 화면 톱니(GearHud)도 이 크기·자리를 그대로 쓴다. (오른쪽 위 모서리 기준, 톱니 중심)
    public const float GearSize = 108f;
    public static readonly Vector2 GearCenter = new Vector2(-88f, -75f);

    private void BuildGear()
    {
        if (_gear == null) return;
        var rect = NewRect("Gear", _root);
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(GearSize, GearSize);
        rect.anchoredPosition = GearCenter;
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = _gear;
        image.preserveAspect = true;
        // 열 때 누른 화면 톱니(GearHud)와 같은 자리라, 다시 누르면 어느 화면이든 ESC 창을 닫는다 (2026-11-05).
        AddButton(rect.gameObject, image, Close, 0.85f);
    }

    private void BuildMenu()
    {
        var items = new List<(string label, Action action)> { ("계속하기", Close) };
        bool battle = _style == Style.Battle;
        // 전투에서 나가면 진행 중인 전투가 사라지므로 한 번 더 묻는다. 마을은 잃을 것이 없어 바로 실행한다.
        if (battle && RunSave.HasCurrentBattle)
            items.Add(("재도전", () => Confirm("보스전을 다시 시작할까요?", "보스 등장 장면부터 다시 싸워요.\n처치한 보스와 편성은 그대로예요.", "재도전", RetryBattle)));
        items.Add(("타이틀화면으로", battle ? (Action)(() => Confirm("타이틀로 나갈까요?", LeaveBattleBody, "나가기", () => LeaveTo(TitleScene))) : () => LeaveTo(TitleScene)));
        items.Add(("소리설정", () => ShowPage(Page.Settings)));
        items.Add(("종료", battle ? (Action)(() => Confirm("게임을 끝낼까요?", LeaveBattleBody, "종료", Quit)) : Quit));

        if (_style == Style.Battle)
        {
            // 전투: 버튼 높이(긴 문구는 더 큼) + 빈칸으로 쌓고, 묶음 전체를 화면 가운데에 둔다. (ESC_조정.png)
            float total = -BattleGap;
            foreach (var item in items) total += BattleHeight(item.label) + BattleGap;
            float y = total * 0.5f + MenuCenterY;
            foreach (var item in items)
            {
                float height = BattleHeight(item.label);
                y -= height * 0.5f;
                MakeMenuButton(item.label, y, item.action);
                y -= height * 0.5f + BattleGap;
            }
            return;
        }

        float firstY = (items.Count - 1) * Spacing * 0.5f + MenuCenterY;
        for (int i = 0; i < items.Count; i++)
            MakeMenuButton(items[i].label, firstY - Spacing * i, items[i].action);
    }

    private static string LeaveBattleBody => RunSave.HasCurrentBattle
        ? "지금 전투는 저장되지 않아요.\n이어하면 이 보스전을 처음부터 다시 싸워요."
        : "지금 전투는 저장되지 않아요.";

    private void Confirm(string title, string body, string confirmLabel, Action action)
    {
        _confirmTitle = title;
        _confirmBody = body;
        _confirmLabel = confirmLabel;
        _confirmAction = action;
        ShowPage(Page.Confirm);
    }

    // 메뉴는 뒤에 어둡게 남기고, 그 위에 대사창 틀 팝업을 띄운다.
    // 위험한 버튼은 왼쪽, 안전한 '돌아가기'는 오른쪽. ESC도 '돌아가기'와 같다 (Update).
    private void BuildConfirm()
    {
        var dim = NewRect("ConfirmDim", _root);
        Stretch(dim);
        var dimImage = dim.gameObject.AddComponent<Image>();
        dimImage.color = new Color(0f, 0f, 0f, 0.6f);
        dimImage.raycastTarget = true;   // 뒤의 메뉴 버튼을 누르지 못하게 막는다

        var panel = NewRect("ConfirmPanel", _root);
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(ConfirmWidth, ConfirmHeight);
        var fill = NewRect("Fill", panel);
        Stretch(fill);
        fill.offsetMin = new Vector2(10f, 10f);
        fill.offsetMax = new Vector2(-10f, -10f);
        var fillImage = fill.gameObject.AddComponent<Image>();
        fillImage.color = new Color(0.04f, 0.04f, 0.04f, 1f);   // 완전히 불투명 (0.96이면 선형 색공간에서 뒤 메뉴가 비쳤다)
        fillImage.raycastTarget = true;
        if (_dialogFrame != null)
        {
            var frame = NewRect("Frame", panel);
            Stretch(frame);
            var frameImage = frame.gameObject.AddComponent<Image>();
            frameImage.sprite = _dialogFrame;
            frameImage.type = Image.Type.Sliced;
            frameImage.raycastTarget = false;
        }

        var title = NewRect("Title", panel);
        title.sizeDelta = new Vector2(ConfirmWidth - 120f, 70f);
        title.anchoredPosition = new Vector2(0f, 112f);
        AddText(title, _confirmTitle, _bold, 48f, Color.white).raycastTarget = false;

        var body = NewRect("Body", panel);
        body.sizeDelta = new Vector2(ConfirmWidth - 120f, 100f);
        body.anchoredPosition = new Vector2(0f, 22f);
        var bodyText = AddText(body, _confirmBody, _light, 30f, new Color(1f, 1f, 1f, 0.8f));
        bodyText.lineSpacing = 10f;
        bodyText.raycastTarget = false;

        float half = ConfirmButtonWidth * 0.5f + 15f;
        var action = _confirmAction;
        var yes = MakeBrushButton(panel, _confirmLabel, new Vector2(-half, -105f), ConfirmButtonWidth, () => action?.Invoke());
        var no = MakeBrushButton(panel, "돌아가기", new Vector2(half, -105f), ConfirmButtonWidth, () => ShowPage(Page.Menu));
        // 팝업 버튼은 메뉴처럼 글자 사이를 벌리지 않는다 (붓 끝에 글자가 걸리지 않게)
        foreach (var b in new[] { yes, no }) b.GetComponentInChildren<TextMeshProUGUI>().characterSpacing = 0f;
    }

    private void BuildSettings()
    {
        MakeLabel("소리설정", _bold, 56f, Color.white, new Vector2(SettingsRowX, 250f), 600f);
        MakeVolumeRow("마스터", "MasterVolume", 120f);
        MakeVolumeRow("배경음", "BGMVolume", 20f);
        MakeVolumeRow("효과음", "SFXVolume", -80f);
        MakeMenuButton("돌아가기", -230f, () => { if (_settingsOnly) Close(); else ShowPage(Page.Menu); });
        BuildCreditLink();
    }

    // 크레딧: 설정 화면 오른쪽 아래 구석의 작고 흐린 글자. 눈에 띄지 않게 숨겨 둔다. (업적 ACH-10)
    private void BuildCreditLink()
    {
        var rect = NewRect("Credit", _root);
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
        rect.sizeDelta = new Vector2(160f, 50f);
        rect.anchoredPosition = new Vector2(-110f, 45f);
        var text = AddText(rect, "Credit", _light, 26f, new Color(1f, 1f, 1f, 0.35f));
        text.characterSpacing = 6f;
        AddButton(rect.gameObject, text, () => ShowPage(Page.Credit), 0.92f);
    }

    // 옛 설정창의 크레딧과 같은 화면: 검은 바탕에 엔딩 그림(Resources/Ending)
    private void BuildCredit()
    {
        var panel = NewRect("CreditPanel", _root);
        Stretch(panel);
        var bg = panel.gameObject.AddComponent<Image>();
        bg.color = Color.black;
        var ending = Resources.Load<Texture2D>("Ending");
        if (ending != null)
        {
            var art = NewRect("Ending", panel);
            Stretch(art);
            var image = art.gameObject.AddComponent<RawImage>();
            image.texture = ending;
            image.raycastTarget = false;
            var fit = art.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = (float)ending.width / ending.height;
        }
        var back = NewRect("Back", _root);
        back.anchorMin = back.anchorMax = new Vector2(0f, 0f);
        back.sizeDelta = new Vector2(260f, 70f);
        back.anchoredPosition = new Vector2(170f, 70f);
        var label = AddText(back, "돌아가기", _bold, 38f, Color.white);
        AddButton(back.gameObject, label, () => ShowPage(Page.Settings), 0.85f);
        if (AchievementManager.Instance != null) { AchievementManager.Instance.AddProgress("ACH-10", 1); PlayerPrefs.Save(); }
    }

    // 설정 화면의 슬라이더 줄 중심. 마을은 오른쪽 끝에 붙어 있어 안쪽으로 당긴다.
    private float SettingsRowX => _style == Style.Village ? ColumnX - 150f : ColumnX;

    private float Spacing => _style == Style.Village ? VillageSpacing : BattleSpacing;
    private float ColumnX => _style == Style.Village ? VillageColumnX : BattleColumnX;
    private float MenuCenterY => _style == Style.Village ? -40f : 0f;
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
        bool isLong = IsLongLabel(label);
        var rect = MakeBrushButton(_root, label, new Vector2(ColumnX, y), isLong ? BattleLongWidth : BattleButtonWidth, action,
            isLong ? BattleLongScale : 1f);
        rect.anchorMin = rect.anchorMax = ColumnAnchor;
        rect.anchoredPosition = new Vector2(ColumnX, y);
    }

    private RectTransform MakeBrushButton(RectTransform parent, string label, Vector2 pos, float width, Action action, float scale = 1f)
    {
        var rect = NewRect(label, parent);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, BattleButtonHeight);
        rect.anchoredPosition = pos;
        // 마우스 올림 효과(UIHoverScale)가 이 크기를 기준으로 삼도록 AddButton보다 먼저 정한다.
        if (scale != 1f) rect.localScale = new Vector3(scale, scale, 1f);
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
        return rect;
    }

    private static bool IsLongLabel(string label) => label.Length > 4;
    private static float BattleHeight(string label) => BattleButtonHeight * (IsLongLabel(label) ? BattleLongScale : 1f);

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
