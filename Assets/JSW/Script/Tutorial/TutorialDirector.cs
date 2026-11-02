using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 튜토리얼 진행자. TutorialScript의 단계를 차례로 보여 주고, 말풍선이 시킨 행동만 허락한다.
/// - 입력 막기: BattleManagerTemp가 배치·드래그·이동·심판 전에 Allow를 묻는다.
/// - 진행: 행동이 끝나면 Notify, 턴의 배치/행동 단계가 시작되면 OnPhase가 불린다.
/// - 말풍선 배경: Resources/대사창 (흰 테두리, 안쪽은 비어 있어 어두운 판을 깐다).
/// </summary>
public sealed class TutorialDirector : MonoBehaviour
{
    public static TutorialDirector Instance { get; private set; }
    public static bool Active => Instance != null;

    private const float BubbleWidth = 760f;
    private const float Gap = 24f;

    private BoardPanel _board;
    private BattleManagerTemp _battle;
    private int _index;
    private int _turn;
    private bool _place;
    private bool _showing;
    private bool _finishing;

    private RectTransform _root, _bubble, _arrow, _highlight;
    private TextMeshProUGUI _text, _hint;
    private CanvasGroup _bubbleGroup;

    private TutorialStep Current => _index < TutorialScript.Steps.Length ? TutorialScript.Steps[_index] : null;

    public static void Create()
    {
        if (Instance != null) Destroy(Instance.gameObject);
        new GameObject("TutorialDirector").AddComponent<TutorialDirector>();
    }

    private void Awake()
    {
        Instance = this;
        BuildUi();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ───────────────────────── 입력 막기 · 진행 ─────────────────────────

    /// <summary>지금 이 행동을 해도 되는가. 튜토리얼이 아니면 항상 허락한다.</summary>
    public static bool Allow(TutorialAct act, int unit, Vector2Int? cell = null)
    {
        if (!Active) return true;
        var s = Instance._showing ? Instance.Current : null;
        if (s == null) return false;
        switch (act)
        {
            case TutorialAct.Select: return (s.act == TutorialAct.Select || s.act == TutorialAct.Place) && s.unit == unit;
            case TutorialAct.Place: return s.act == TutorialAct.Place && s.unit == unit && cell == s.cell;
            case TutorialAct.Drag: return s.act == TutorialAct.Move && s.unit == unit;
            case TutorialAct.Move: return s.act == TutorialAct.Move && s.unit == unit && cell == s.cell;
            case TutorialAct.Judge: return s.act == TutorialAct.Judge && s.unit == unit;
            default: return false;
        }
    }

    /// <summary>행동이 실제로 일어난 뒤 부른다. 시킨 행동이면 다음 단계로.</summary>
    public static void Notify(TutorialAct act, int unit)
    {
        if (!Active) return;
        var s = Instance.Current;
        if (!Instance._showing || s == null || s.act != act || s.unit != unit) return;
        Instance.Advance();
    }

    /// <summary>BoardPanel.NextTurn: 배치 단계(place) 또는 행동 단계가 시작됐다.</summary>
    public static void OnPhase(int turn, bool place)
    {
        if (!Active) return;
        Instance._turn = turn;
        Instance._place = place;
        Instance.TryShow();
    }

    private void Advance()
    {
        _index++;
        _showing = false;
        TryShow();
    }

    private void TryShow()
    {
        var s = Current;
        if (_finishing) return;
        if (s == null || s.turn != _turn || s.place != _place) { Hide(); return; }
        _showing = true;
        _text.text = s.text;
        _hint.gameObject.SetActive(s.act == TutorialAct.Next);
        // 행동 단계에서는 말풍선이 클릭·드래그를 가로채지 않게 한다.
        _bubbleGroup.blocksRaycasts = s.act == TutorialAct.Next;
        _bubble.gameObject.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_bubble);
        Place();
    }

    private void Hide()
    {
        _showing = false;
        _bubble.gameObject.SetActive(false);
        _arrow.gameObject.SetActive(false);
        _highlight.gameObject.SetActive(false);
    }

    private void OnBubbleClicked()
    {
        if (_finishing) { Leave(); return; }
        var s = Current;
        if (_showing && s != null && s.act == TutorialAct.Next) Advance();
    }

    /// <summary>안내자를 쓰러뜨렸다. 마지막 말을 보여 주고 클릭하면 마을로.</summary>
    public void Finish()
    {
        _finishing = true;
        _showing = true;
        _text.text = TutorialScript.EndLine;
        _hint.gameObject.SetActive(true);
        _bubbleGroup.blocksRaycasts = true;
        _bubble.gameObject.SetActive(true);
        _highlight.gameObject.SetActive(false);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_bubble);
        Place();
    }

    private void Leave()
    {
        PlayerPrefs.SetInt(TutorialScript.DoneKey, 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene("Main");
    }

    // ───────────────────────── 위치 ─────────────────────────

    private void LateUpdate()
    {
        if (_bubble.gameObject.activeSelf) Place();
        if (_highlight.gameObject.activeSelf)
        {
            float a = 0.35f + 0.25f * Mathf.Sin(Time.unscaledTime * 5f);
            _highlight.GetComponent<Image>().color = new Color(1f, 0.84f, 0.3f, a);
        }
    }

    private RectTransform AnchorRect(TutorialStep s)
    {
        if (_board == null) _board = FindFirstObjectByType<BoardPanel>();
        if (_battle == null) _battle = FindFirstObjectByType<BattleManagerTemp>();
        if (s == null || _board == null || _battle == null) return null;
        switch (s.anchor)
        {
            case TutorialAnchor.Waiting: return _battle.GetWaitingRect(s.unit);
            case TutorialAnchor.Cell: return _battle.GetCellRect(s.cell);
            case TutorialAnchor.Unit: return s.unit >= 0 ? _battle.GetUnitImage(s.unit).rectTransform : null;
            default: return _board.TutorialAnchorRect(s.anchor, s.unit);
        }
    }

    // 이동·배치할 칸을 금색으로 깜빡인다.
    private RectTransform HighlightRect(TutorialStep s)
    {
        if (s == null || _battle == null) return null;
        if (s.act == TutorialAct.Place || s.act == TutorialAct.Move) return _battle.GetCellRect(s.cell);
        if (s.act == TutorialAct.Select) return _battle.GetWaitingRect(s.unit);
        return null;
    }

    private void Place()
    {
        var s = _finishing ? null : Current;
        var target = _finishing ? null : AnchorRect(s);
        bool hasTarget = target != null && target.gameObject.activeInHierarchy && TryScreenRect(target, out _);

        var hl = HighlightRect(s);
        if (hl != null && TryScreenRect(hl, out var hlRect))
        {
            _highlight.gameObject.SetActive(true);
            _highlight.anchoredPosition = hlRect.center;
            _highlight.sizeDelta = hlRect.size;
        }
        else _highlight.gameObject.SetActive(false);

        var size = _bubble.rect.size;
        var half = _root.rect.size * 0.5f;
        if (!hasTarget)
        {
            _bubble.anchoredPosition = new Vector2(0f, -half.y + size.y * 0.5f + 140f);
            _arrow.gameObject.SetActive(false);
            return;
        }
        TryScreenRect(target, out var r);
        // 기본은 대상 위. 화면 위가 모자라면 아래.
        bool above = r.yMax + Gap + size.y < half.y - 10f;
        float y = above ? r.yMax + Gap + size.y * 0.5f : r.yMin - Gap - size.y * 0.5f;
        float x = Mathf.Clamp(r.center.x, -half.x + size.x * 0.5f + 16f, half.x - size.x * 0.5f - 16f);
        y = Mathf.Clamp(y, -half.y + size.y * 0.5f + 16f, half.y - size.y * 0.5f - 16f);
        _bubble.anchoredPosition = new Vector2(x, y);

        _arrow.gameObject.SetActive(true);
        _arrow.anchoredPosition = new Vector2(Mathf.Clamp(r.center.x, x - size.x * 0.5f + 40f, x + size.x * 0.5f - 40f),
            above ? y - size.y * 0.5f - 10f : y + size.y * 0.5f + 10f);
        _arrow.localEulerAngles = new Vector3(0f, 0f, above ? 180f : 0f);
    }

    /// <summary>다른 캔버스의 RectTransform을 이 캔버스의 좌표(가운데 기준)로 바꾼다.</summary>
    private bool TryScreenRect(RectTransform target, out Rect rect)
    {
        rect = default;
        var canvas = target.GetComponentInParent<Canvas>();
        if (canvas == null) return false;
        canvas = canvas.rootCanvas;
        var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        var corners = new Vector3[4];
        target.GetWorldCorners(corners);
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
        foreach (var c in corners)
        {
            var screen = RectTransformUtility.WorldToScreenPoint(cam, c);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, null, out var local)) return false;
            min = Vector2.Min(min, local);
            max = Vector2.Max(max, local);
        }
        rect = new Rect(min, max - min);
        return true;
    }

    // ───────────────────────── 화면 만들기 ─────────────────────────

    private void BuildUi()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 520;   // 전투 UI 위, ESC 메뉴(550) 아래
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        _root = NewRect("Root", transform);
        _root.anchorMin = Vector2.zero;
        _root.anchorMax = Vector2.one;
        _root.offsetMin = _root.offsetMax = Vector2.zero;

        _highlight = NewRect("Highlight", _root);
        var hl = _highlight.gameObject.AddComponent<Image>();
        hl.raycastTarget = false;
        _highlight.gameObject.SetActive(false);

        var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/BookkMyungjo_Light SDF");

        _bubble = NewRect("Bubble", _root);
        _bubble.sizeDelta = new Vector2(BubbleWidth, 0f);
        _bubbleGroup = _bubble.gameObject.AddComponent<CanvasGroup>();
        var fit = _bubble.gameObject.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var layout = _bubble.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(56, 56, 44, 40);
        layout.spacing = 6f;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandHeight = false;

        // 어두운 바탕 (대사창 그림은 테두리만 있다)
        var back = _bubble.gameObject.AddComponent<Image>();
        back.color = new Color(0.06f, 0.06f, 0.07f, 0.9f);
        var button = _bubble.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(OnBubbleClicked);

        var frame = NewRect("Frame", _bubble);
        frame.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        frame.anchorMin = Vector2.zero;
        frame.anchorMax = Vector2.one;
        frame.offsetMin = new Vector2(-10f, -10f);
        frame.offsetMax = new Vector2(10f, 10f);
        var frameImage = frame.gameObject.AddComponent<Image>();
        var sprites = Resources.LoadAll<Sprite>("대사창");
        if (sprites.Length > 0) { frameImage.sprite = sprites[0]; frameImage.type = Image.Type.Sliced; }
        else frameImage.enabled = false;
        frameImage.raycastTarget = false;

        var name = NewText("Name", _bubble, font, 26f, new Color(1f, 0.84f, 0.48f));
        name.text = TutorialScript.BossName;
        _text = NewText("Text", _bubble, font, 32f, Color.white);
        _text.lineSpacing = 12f;
        _hint = NewText("Hint", _bubble, font, 22f, new Color(1f, 1f, 1f, 0.55f));
        _hint.text = "클릭해서 계속 ▶";
        _hint.alignment = TextAlignmentOptions.Right;

        _arrow = NewRect("Arrow", _root);
        _arrow.sizeDelta = new Vector2(36f, 22f);
        var arrow = _arrow.gameObject.AddComponent<Image>();
        arrow.sprite = TriangleSprite();
        arrow.color = Color.white;
        arrow.raycastTarget = false;

        Hide();
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    private static TextMeshProUGUI NewText(string name, Transform parent, TMP_FontAsset font, float size, Color color)
    {
        var text = NewRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.fontSize = size;
        text.color = color;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }

    // 위를 가리키는 작은 삼각형 (회전해서 아래도 가리킨다)
    private static Sprite TriangleSprite()
    {
        const int w = 36, h = 22;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float half = (float)y / (h - 1) * (w * 0.5f);
            bool inside = Mathf.Abs(x + 0.5f - w * 0.5f) <= (w * 0.5f - half);
            tex.SetPixel(x, y, inside ? Color.white : Color.clear);
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
    }
}
