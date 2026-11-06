using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전투 화면 왼쪽 아래 캐릭터 초상화의 정보 표시.
/// - 마우스를 올리면 초상화가 어두워지며 &lt;정보&gt; 글자가 뜨고, 초상화 위에 정보창(대사창 테두리)이 뜬다.
/// - 정보창 오른쪽 위에 피격 해골 3개 (맞은 만큼 붉게, 3번째에 쓰러짐).
/// </summary>
public sealed class UnitInfoView
{
    private const float PanelWidth = 520f;
    private const float Padding = 26f;
    private const float TitleHeight = 42f;
    private const float BodyLineSpacing = -6f;
    private const float PipSize = 30f;
    private const float PipGap = 6f;

    private readonly Image[] _portraits;
    private readonly RectTransform _canvas;
    private readonly TMP_FontAsset _font;
    private readonly GameObject[] _dims;
    private readonly Sprite _skullHit, _skullEmpty;

    private RectTransform _panel;
    private TextMeshProUGUI _title, _body;
    private Image[] _panelPips;

    public int Shown { get; private set; } = -1;

    public UnitInfoView(Image[] portraits, TMP_FontAsset font)
    {
        _portraits = portraits;
        _font = font;
        _canvas = (RectTransform)portraits[0].canvas.rootCanvas.transform;
        _skullHit = Resources.Load<Sprite>("UI/skull_hit");
        _skullEmpty = Resources.Load<Sprite>("UI/skull_empty");
        _dims = new GameObject[portraits.Length];
        for (int i = 0; i < portraits.Length; i++)
        {
            _dims[i] = CreateDim(portraits[i].transform);
        }
        CreatePanel();
    }

    // ───────── 초상화 어둡게 + <정보> ─────────

    private GameObject CreateDim(Transform portrait)
    {
        var go = new GameObject("InfoDim", typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(portrait, false);
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var image = go.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.55f);
        image.raycastTarget = false;

        var label = NewText(rect, "Label", 34f, Color.white);
        label.richText = false;
        label.text = "<정보>";
        label.alignment = TextAlignmentOptions.Center;
        Stretch(label.rectTransform);
        go.SetActive(false);
        return go;
    }

    // ───────── 해골 줄 ─────────

    private Image[] CreatePipRow(Transform parent, Vector2 anchor, Vector2 pivot, Vector2 offset, float size)
    {
        var row = new GameObject("HitPips", typeof(RectTransform));
        var rect = (RectTransform)row.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = offset;
        rect.sizeDelta = new Vector2(size * 3f + PipGap * 2f, size);
        var pips = new Image[3];
        for (int k = 0; k < 3; k++)
        {
            var go = new GameObject("Skull" + k, typeof(RectTransform), typeof(Image));
            var r = (RectTransform)go.transform;
            r.SetParent(rect, false);
            r.anchorMin = r.anchorMax = new Vector2(0f, 0.5f);
            r.pivot = new Vector2(0f, 0.5f);
            r.anchoredPosition = new Vector2(k * (size + PipGap), 0f);
            r.sizeDelta = new Vector2(size, size);
            pips[k] = go.GetComponent<Image>();
            pips[k].preserveAspect = true;
            pips[k].raycastTarget = false;
            pips[k].sprite = _skullEmpty;
        }
        row.SetActive(false);
        return pips;
    }

    /// <summary>hits: 맞은 횟수. hits &lt; 0 이면 해골 줄을 숨긴다.</summary>
    public void SetPips(int i, int hits)
    {
        if (i == Shown) Apply(_panelPips, hits);
    }

    private void Apply(Image[] pips, int hits)
    {
        bool show = hits >= 0 && _skullHit != null;
        var row = pips[0].transform.parent.gameObject;
        if (row.activeSelf != show) row.SetActive(show);
        if (!show) return;
        for (int k = 0; k < pips.Length; k++)
        {
            var sprite = k < hits ? _skullHit : _skullEmpty;
            if (pips[k].sprite != sprite) pips[k].sprite = sprite;
        }
    }

    // ───────── 정보창 ─────────

    private void CreatePanel()
    {
        var go = new GameObject("UnitInfoPanel", typeof(RectTransform));
        _panel = (RectTransform)go.transform;
        _panel.SetParent(_canvas, false);
        _panel.anchorMin = _panel.anchorMax = new Vector2(0.5f, 0.5f);
        _panel.pivot = new Vector2(0.5f, 0f);
        // 전투 화면의 다른 캔버스보다 위에 그린다.
        var overlay = go.AddComponent<Canvas>();
        overlay.overrideSorting = true;
        overlay.sortingOrder = 500;

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        var fr = (RectTransform)fill.transform;
        fr.SetParent(_panel, false);
        Stretch(fr, 8f);
        var fillImage = fill.GetComponent<Image>();
        fillImage.color = Color.black;   // 완전히 검은 바탕 (편성·아이템 설명 창과 같게, 2026-11-05)
        fillImage.raycastTarget = false;

        var sprites = Resources.LoadAll<Sprite>("대사창");
        if (sprites.Length > 0)
        {
            var frame = new GameObject("Frame", typeof(RectTransform), typeof(Image));
            var r = (RectTransform)frame.transform;
            r.SetParent(_panel, false);
            Stretch(r);
            var image = frame.GetComponent<Image>();
            image.sprite = sprites[0];
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1.3f;
            image.raycastTarget = false;
        }

        _title = NewText(_panel, "Title", 34f, Color.white);
        var tr = _title.rectTransform;
        tr.anchorMin = new Vector2(0f, 1f); tr.anchorMax = new Vector2(1f, 1f);
        tr.pivot = new Vector2(0f, 1f);
        tr.anchoredPosition = new Vector2(Padding, -Padding + 6f);
        tr.sizeDelta = new Vector2(-Padding * 2f, TitleHeight);

        _panelPips = CreatePipRow(_panel, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-Padding, -Padding + 2f), PipSize);

        _body = NewText(_panel, "Body", 25f, new Color(0.9f, 0.9f, 0.92f));
        _body.richText = true;
        _body.lineSpacing = BodyLineSpacing;
        var br = _body.rectTransform;
        br.anchorMin = new Vector2(0f, 1f); br.anchorMax = new Vector2(1f, 1f);
        br.pivot = new Vector2(0f, 1f);
        br.anchoredPosition = new Vector2(Padding, -Padding - TitleHeight + 6f);
        br.sizeDelta = new Vector2(-Padding * 2f, 100f);

        go.SetActive(false);
    }

    public void Show(int i)
    {
        if (Shown >= 0 && Shown != i) Hide(Shown);
        Shown = i;
        _dims[i].SetActive(true);
        _panel.gameObject.SetActive(true);
        _panel.SetAsLastSibling();
    }

    public void Hide(int i)
    {
        if (i >= 0 && i < _dims.Length) _dims[i].SetActive(false);
        if (Shown != i) return;
        Shown = -1;
        _panel.gameObject.SetActive(false);
    }

    public void SetContent(string title, string body, int hits)
    {
        if (Shown < 0) return;
        _title.text = title;
        _body.text = body;
        Apply(_panelPips, hits);

        float innerWidth = PanelWidth - Padding * 2f;
        float bodyHeight = _body.GetPreferredValues(body, innerWidth, 0f).y;
        _body.rectTransform.sizeDelta = new Vector2(-Padding * 2f, bodyHeight);
        float height = Padding * 2f + TitleHeight - 6f + bodyHeight;
        _panel.sizeDelta = new Vector2(PanelWidth, height);

        // 마우스를 올린 초상화의 가운데 위에 띄우고, 화면 밖으로 나가지 않게 한다.
        var portrait = _portraits[Shown].rectTransform;
        var corners = new Vector3[4];
        portrait.GetWorldCorners(corners);
        Vector3 topCenter = (corners[1] + corners[2]) * 0.5f;
        Vector2 local = _canvas.InverseTransformPoint(topCenter);
        var half = _canvas.rect.size * 0.5f;
        float x = Mathf.Clamp(local.x, -half.x + PanelWidth * 0.5f + 8f, half.x - PanelWidth * 0.5f - 8f);
        float y = Mathf.Min(local.y + 12f, half.y - height - 8f);
        _panel.anchoredPosition = new Vector2(x, y);
    }

    // ───────── 공용 ─────────

    private TextMeshProUGUI NewText(Transform parent, string name, float size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        if (_font != null) text.font = _font;
        text.fontSize = size;
        text.color = color;
        text.raycastTarget = false;
        text.enableWordWrapping = true;
        text.alignment = TextAlignmentOptions.TopLeft;
        return text;
    }

    private static void Stretch(RectTransform rect, float inset = 0f)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }
}
