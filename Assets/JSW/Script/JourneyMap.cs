using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 여정 지도 (1단계): 마을 문을 누르면 하얗게 밝아지며 여정 지도가 뜬다. 지금은 지도 + 돌아가기만 있다.
// 배경: 저승 조건(편성에 꼬마 사신 + 사신의 낫 + 계약서)을 채우면 여정맵, 아니면 여정맵_저승없음.
// 기획: 프로젝트 문서 '여정 지도 기획 v1'
public sealed class JourneyMap : MonoBehaviour
{
    private const int SortingOrder = 90;          // 마을 UI 위, ESC 메뉴(550)·확인 팝업(600) 아래
    private const float FadeToWhite = 0.35f;
    private const float FadeFromWhite = 0.45f;
    private const float CloseTime = 0.25f;

    // 지도 위 장소: 이름표 그림, 이름표 위치, 꼬마 사신이 걸어가 설 자리 (지도 그림 기준 0~1, 왼쪽 아래가 0,0)
    private struct Place
    {
        public string label; public Vector2 tag; public Vector2 stand; public bool afterlifeOnly;
        public Place(string label, Vector2 tag, Vector2 stand, bool afterlifeOnly = false)
        { this.label = label; this.tag = tag; this.stand = stand; this.afterlifeOnly = afterlifeOnly; }
    }
    private static readonly Place[] Places =
    {
        new Place("여정_대저택", new Vector2(0.477f, 0.935f), new Vector2(0.465f, 0.585f)),
        new Place("여정_성당",   new Vector2(0.830f, 0.915f), new Vector2(0.790f, 0.650f)),
        new Place("여정_알현실", new Vector2(0.637f, 0.720f), new Vector2(0.615f, 0.445f)),
        new Place("여정_서고",   new Vector2(0.258f, 0.700f), new Vector2(0.330f, 0.515f)),
        new Place("여정_광장",   new Vector2(0.461f, 0.553f), new Vector2(0.465f, 0.415f)),
        new Place("여정_연구소", new Vector2(0.906f, 0.625f), new Vector2(0.860f, 0.305f)),
        new Place("여정_훈련장", new Vector2(0.656f, 0.278f), new Vector2(0.640f, 0.110f)),
        new Place("여정_지하실", new Vector2(0.383f, 0.243f), new Vector2(0.365f, 0.095f)),
        new Place("여정_저승",   new Vector2(0.090f, 0.345f), new Vector2(0.110f, 0.165f), afterlifeOnly: true),
    };
    private static readonly Vector2 ReaperStart = new Vector2(0.422f, 0.278f); // 광장 아래 아치 입구
    private const float ReaperHeight = 150f;     // 캐릭터 키 (캔버스 px)
    private const float ReaperSpeed = 260f;      // 캔버스 px/초
    private const float LabelScale = 1.25f;

    private RectTransform _mapRect;
    private RectTransform _reaper;
    private Coroutine _walk;

    private static JourneyMap _instance;
    private CanvasGroup _group;
    private Image _white;
    private bool _busy;

    public static bool IsOpen => _instance != null;

    public static void Open()
    {
        if (_instance != null || SceneFade.IsFading) return;
        var go = new GameObject("Journey Map", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        SceneManager.MoveGameObjectToScene(go, SceneManager.GetActiveScene());
        _instance = go.AddComponent<JourneyMap>();
        _instance.Build();
        _instance.StartCoroutine(_instance.OpenRoutine());
    }

    public static void Close()
    {
        if (_instance == null || _instance._busy) return;
        _instance.StartCoroutine(_instance.CloseRoutine());
    }

    /// <summary>편성에 꼬마 사신·사신의 낫·계약서가 모두 있으면 true (BattleDisplayManager.GetFinalBoss와 같은 조건).</summary>
    public static bool AfterlifeUnlocked
    {
        get
        {
            bool reaper = false, scythe = false, contract = false;
            for (int i = 0; i < 3; i++)
            {
                if (PlayerPrefs.GetInt("Formation_v1_Unit_" + i, 0) == 9) reaper = true;
                string item = PlayerPrefs.GetString("Formation_v1_Item_" + i, "");
                if (item == "22") scythe = true;
                if (item == "23") contract = true;
            }
            return reaper && PlayerPrefs.GetInt("Character_9", 0) == 1
                && scythe && PlayerPrefs.GetInt("Item_22", 0) > 0
                && contract && PlayerPrefs.GetInt("Item_23", 0) > 0;
        }
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    private void Build()
    {
        var canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;
        var scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        _group = GetComponent<CanvasGroup>();

        // 검은 바탕 (지도 비율이 화면과 달라도 빈틈이 보이지 않게)
        var back = NewImage("Back", transform, Color.black);
        Stretch(back.rectTransform);

        // 지도: 화면을 꽉 채우고 넘치는 위아래는 잘라낸다 (지도 5:3, 화면 16:9)
        var map = NewImage("Map", transform, Color.white);
        map.sprite = LoadSprite(AfterlifeUnlocked ? "여정맵" : "여정맵_저승없음");
        map.raycastTarget = true; // 뒤의 마을이 눌리지 않게
        var fitter = map.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = map.sprite != null ? map.sprite.rect.width / map.sprite.rect.height : 5f / 3f;
        _mapRect = map.rectTransform;

        bool afterlife = AfterlifeUnlocked;
        BuildReaper();
        foreach (var place in Places)
            if (!place.afterlifeOnly || afterlife) BuildLabel(place);

        BuildBackButton();

        // 열릴 때 화면을 덮는 흰 막
        _white = NewImage("White", transform, new Color(1f, 1f, 1f, 0f));
        Stretch(_white.rectTransform);
        _white.raycastTarget = false;
    }

    // 꼬마 사신: 발밑이 위치점. 이름표를 누르면 그쪽으로 걸어간다.
    private void BuildReaper()
    {
        var sprite = LoadSprite("여정_꼬마사신");
        if (sprite == null) return;
        var image = NewImage("Reaper", _mapRect, Color.white);
        image.sprite = sprite;
        image.raycastTarget = false;
        _reaper = image.rectTransform;
        // 그림 안에서 캐릭터가 차지하는 부분 (원본 3600x2160 기준 x 1266~2030, y 489~1666)
        const float contentHeight = 1177f / 2160f;
        float h = ReaperHeight / contentHeight;
        _reaper.sizeDelta = new Vector2(h * sprite.rect.width / sprite.rect.height, h);
        _reaper.pivot = new Vector2(1648f / 3600f, (2160f - 1666f) / 2160f);
        _reaper.anchorMin = _reaper.anchorMax = ReaperStart;
        _reaper.anchoredPosition = Vector2.zero;
    }

    private void BuildLabel(Place place)
    {
        var sprite = LoadSprite(place.label);
        if (sprite == null) return;
        var size = new Vector2(sprite.rect.width, sprite.rect.height) * LabelScale;

        var root = new GameObject(place.label, typeof(RectTransform), typeof(Button));
        var rect = (RectTransform)root.transform;
        rect.SetParent(_mapRect, false);
        rect.anchorMin = rect.anchorMax = place.tag;
        rect.sizeDelta = size;

        // 밝은 바닥에서도 보이게 뒤에 어두운 그림자
        var shadow = NewImage("Shadow", rect, new Color(0f, 0f, 0f, 0.6f));
        shadow.sprite = sprite;
        shadow.raycastTarget = false;
        Stretch(shadow.rectTransform);
        shadow.rectTransform.offsetMin = new Vector2(-14f, -10f);
        shadow.rectTransform.offsetMax = new Vector2(14f, 6f);

        var tag = NewImage("Tag", rect, Color.white);
        tag.sprite = sprite;
        Stretch(tag.rectTransform);
        var button = root.GetComponent<Button>();
        button.targetGraphic = tag;
        var stand = place.stand;
        button.onClick.AddListener(() => WalkTo(stand));
        UIHoverScale.Attach(root, 1.06f);
    }

    private void WalkTo(Vector2 stand)
    {
        if (_reaper == null || _busy) return;
        if (_walk != null) StopCoroutine(_walk);
        _walk = StartCoroutine(Walk(stand));
    }

    // 천천히 미끄러지며 걸음처럼 살짝 통통 튄다. 가는 쪽을 바라본다.
    private IEnumerator Walk(Vector2 stand)
    {
        Vector2 size = _mapRect.rect.size;
        Vector2 from = Vector2.Scale(_reaper.anchorMin, size) + _reaper.anchoredPosition;
        Vector2 to = Vector2.Scale(stand, size);
        float distance = Vector2.Distance(from, to);
        float duration = Mathf.Max(0.3f, distance / ReaperSpeed);
        var scale = _reaper.localScale;
        scale.x = Mathf.Abs(scale.x) * (to.x < from.x ? -1f : 1f);
        _reaper.localScale = scale;
        _reaper.anchorMin = _reaper.anchorMax = Vector2.zero;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            float k = t / duration;
            float ease = k * k * (3f - 2f * k);
            float hop = Mathf.Abs(Mathf.Sin(t * Mathf.PI * 3.2f)) * 9f * Mathf.Min(1f, (1f - k) * 6f);
            _reaper.anchoredPosition = Vector2.Lerp(from, to, ease) + Vector2.up * hop;
            yield return null;
        }
        _reaper.anchorMin = _reaper.anchorMax = stand;
        _reaper.anchoredPosition = Vector2.zero;
        _walk = null;
    }

    // 전투 장면 '돌아가기'(화살표 + 글자)와 같은 모양
    private void BuildBackButton()
    {
        var go = new GameObject("Back Button", typeof(RectTransform), typeof(Button));
        var rect = (RectTransform)go.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-40f, 30f);
        rect.sizeDelta = new Vector2(190f, 56f);

        var hit = NewImage("Hit", rect, new Color(0f, 0f, 0f, 0f));
        Stretch(hit.rectTransform);
        var button = go.GetComponent<Button>();
        button.targetGraphic = hit;
        button.onClick.AddListener(Close);

        var arrow = NewImage("Arrow", rect, Color.white);
        var arrows = Resources.LoadAll<Sprite>("Sprites/UI/Victory/55f41fd6ee183814");
        if (arrows.Length > 0) arrow.sprite = arrows[0];
        arrow.raycastTarget = false;
        var ar = arrow.rectTransform;
        ar.anchorMin = ar.anchorMax = new Vector2(0f, 0.5f);
        ar.pivot = new Vector2(0f, 0.5f);
        ar.sizeDelta = new Vector2(47f, 56f);
        ar.anchoredPosition = Vector2.zero;

        var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        var lr = label.rectTransform;
        lr.SetParent(rect, false);
        lr.anchorMin = new Vector2(0f, 0f); lr.anchorMax = new Vector2(1f, 1f);
        lr.offsetMin = new Vector2(60f, 0f); lr.offsetMax = Vector2.zero;
        var font = FindVillageFont();
        if (font != null) label.font = font;
        label.text = "돌아가기";
        label.fontSize = 36f;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.color = Color.white;
        label.raycastTarget = false;

        UIHoverScale.Attach(go);
    }

    private IEnumerator OpenRoutine()
    {
        _busy = true;
        _group.alpha = 0f;
        // 마을이 하얗게 밝아진 뒤 지도가 드러난다 (예전 여정 전환과 같은 느낌)
        var white = _white;
        _group.alpha = 1f;
        foreach (Transform child in transform) if (child != white.transform) child.gameObject.SetActive(false);
        for (float t = 0f; t < FadeToWhite; t += Time.unscaledDeltaTime)
        {
            SetAlpha(white, Mathf.SmoothStep(0f, 1f, t / FadeToWhite));
            yield return null;
        }
        SetAlpha(white, 1f);
        foreach (Transform child in transform) child.gameObject.SetActive(true);
        for (float t = 0f; t < FadeFromWhite; t += Time.unscaledDeltaTime)
        {
            SetAlpha(white, 1f - Mathf.SmoothStep(0f, 1f, t / FadeFromWhite));
            yield return null;
        }
        SetAlpha(white, 0f);
        _busy = false;
    }

    private IEnumerator CloseRoutine()
    {
        _busy = true;
        for (float t = 0f; t < CloseTime; t += Time.unscaledDeltaTime)
        {
            _group.alpha = 1f - t / CloseTime;
            yield return null;
        }
        Destroy(gameObject);
    }

    private static Sprite LoadSprite(string name)
    {
        var sprites = Resources.LoadAll<Sprite>(name);
        return sprites.Length > 0 ? sprites[0] : null;
    }

    private static TMP_FontAsset FindVillageFont()
    {
        foreach (var text in FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
            if (text.font != null && text.gameObject.scene == SceneManager.GetActiveScene()) return text.font;
        return null;
    }

    private static Image NewImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static void SetAlpha(Graphic g, float a)
    {
        var c = g.color;
        c.a = a;
        g.color = c;
    }
}
