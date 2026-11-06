using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 로딩 화면: 오프닝 장면 한 장(랜덤)을 어둡게 깔고, 오른쪽 아래에 "Loading..."(점 1→2→3 반복)과 로딩 바.
// LoadingScreen.Load("Item") 처럼 쓴다. 너무 빨리 끝나도 깜빡이지 않게 최소 시간만큼은 보여 준다.
public sealed class LoadingScreen : MonoBehaviour
{
    private const int SortingOrder = 29000;        // 장면 전환 페이드(30000) 바로 아래, 나머지 UI 위
    private const string OpeningPath = "Images/Opening";
    private static readonly HashSet<string> Excluded = new HashSet<string> { "11" }; // 타이틀과 같은 제외 목록
    private const float FadeIn = 0.25f;
    private const float FadeOut = 0.35f;
    private const float MinimumShow = 1.2f;        // 최소 표시 시간 (이미 불러왔어도 이만큼은 보여 준다). 2.5 → 1.2 (2026-11-05, 전체 약 1.6초)
    private const float DotInterval = 0.4f;        // 점이 하나 늘어나는 간격
    private const float BarWidth = 420f;
    private const float BarHeight = 34f;

    private static LoadingScreen _instance;
    private CanvasGroup _group;
    private RectTransform _fill;
    private TextMeshProUGUI _label;
    private RectTransform _photo;

    public static bool IsLoading => _instance != null;

    public static void Load(string sceneName)
    {
        if (_instance != null || SceneFade.IsFading) return;
        var go = new GameObject("Loading Screen", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<LoadingScreen>();
        _instance.Build();
        _instance.StartCoroutine(_instance.Run(sceneName));
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
        _group.alpha = 0f;
        _group.blocksRaycasts = true; // 로딩 중에는 아무것도 눌리지 않게

        var back = NewImage("Back", transform, Color.black);
        back.raycastTarget = true;
        Stretch(back.rectTransform);

        // 오프닝 장면 한 장 (화면을 꽉 채우고 아주 천천히 확대)
        var photo = NewImage("Photo", transform, Color.white);
        photo.sprite = PickBackground();
        photo.enabled = photo.sprite != null;
        var fitter = photo.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        if (photo.sprite != null) fitter.aspectRatio = photo.sprite.rect.width / photo.sprite.rect.height;
        _photo = photo.rectTransform;

        // 글자와 바가 잘 보이게 전체를 어둡게 (리니어 색 공간이라 수치보다 옅게 보여서 높게 잡음)
        var shade = NewImage("Shade", transform, new Color(0f, 0f, 0f, 0.72f));
        Stretch(shade.rectTransform);

        var font = GameFonts.Bold;

        // 로딩 바: 흰 테두리 + 안쪽 흰 채움
        var bar = NewRect("Bar", transform);
        bar.anchorMin = bar.anchorMax = new Vector2(1f, 0f);
        bar.pivot = new Vector2(1f, 0f);
        bar.anchoredPosition = new Vector2(-70f, 60f);
        bar.sizeDelta = new Vector2(BarWidth, BarHeight);
        var frame = NewImage("Frame", bar, new Color(0f, 0f, 0f, 0.35f));
        Stretch(frame.rectTransform);
        var outline = frame.gameObject.AddComponent<Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(3f, -3f);
        var outline2 = frame.gameObject.AddComponent<Outline>();
        outline2.effectColor = Color.white;
        outline2.effectDistance = new Vector2(-3f, 3f);
        var fill = NewImage("Fill", bar, Color.white);
        _fill = fill.rectTransform;
        _fill.anchorMin = new Vector2(0f, 0f);
        _fill.anchorMax = new Vector2(0f, 1f);
        _fill.pivot = new Vector2(0f, 0.5f);
        _fill.offsetMin = new Vector2(6f, 6f);
        _fill.offsetMax = new Vector2(6f, -6f);

        // "Loading..." : 바 왼쪽 위
        _label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        var lr = _label.rectTransform;
        lr.SetParent(bar, false);
        lr.anchorMin = lr.anchorMax = new Vector2(0f, 1f);
        lr.pivot = new Vector2(0f, 0f);
        lr.anchoredPosition = new Vector2(0f, 12f);
        lr.sizeDelta = new Vector2(300f, 50f);
        if (font != null) _label.font = font;
        _label.fontSize = 34f;
        _label.alignment = TextAlignmentOptions.BottomLeft;
        _label.textWrappingMode = TextWrappingModes.NoWrap;
        _label.color = Color.white;
        _label.raycastTarget = false;
        _label.text = "Loading.";
        SetProgress(0f);
    }

    private IEnumerator Run(string sceneName)
    {
        float start = Time.unscaledTime;
        float shown = 0f;

        // 화면을 덮는다
        for (float t = 0f; t < FadeIn; t += Time.unscaledDeltaTime)
        {
            _group.alpha = t / FadeIn;
            Animate(start);
            yield return null;
        }
        _group.alpha = 1f;

        var op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null) { Destroy(gameObject); yield break; }
        op.allowSceneActivation = false;

        // 실제 진행(0~0.9)과 최소 표시 시간 중 느린 쪽을 따라 바가 부드럽게 찬다
        while (true)
        {
            float real = Mathf.Clamp01(op.progress / 0.9f);
            float timeGate = Mathf.Clamp01((Time.unscaledTime - start) / MinimumShow);
            float target = Mathf.Min(real, timeGate);
            shown = Mathf.MoveTowards(shown, target, Time.unscaledDeltaTime * 1.8f);
            SetProgress(shown);
            Animate(start);
            if (shown >= 0.999f && real >= 1f) break;
            yield return null;
        }

        op.allowSceneActivation = true;
        while (!op.isDone) { Animate(start); yield return null; }
        yield return null; // 새 장면의 Start가 한 번 돈 뒤에 걷어낸다

        for (float t = 0f; t < FadeOut; t += Time.unscaledDeltaTime)
        {
            _group.alpha = 1f - t / FadeOut;
            Animate(start);
            yield return null;
        }
        Destroy(gameObject);
    }

    // 점 1→2→3→1… 반복, 배경은 아주 천천히 확대
    private void Animate(float start)
    {
        float elapsed = Time.unscaledTime - start;
        int dots = 1 + (int)(elapsed / DotInterval) % 3;
        _label.text = "Loading" + new string('.', dots);
        if (_photo != null) _photo.localScale = Vector3.one * (1f + elapsed * 0.02f);
    }

    private void SetProgress(float value)
    {
        float inner = BarWidth - 12f;
        _fill.sizeDelta = new Vector2(Mathf.Max(0f, inner * value), _fill.sizeDelta.y);
        _fill.gameObject.SetActive(value > 0.01f);
    }

    private static Sprite PickBackground()
    {
        var candidates = new List<Sprite>();
        foreach (var sprite in Resources.LoadAll<Sprite>(OpeningPath))
        {
            if (sprite == null || Excluded.Contains(sprite.name)) continue;
            if (!int.TryParse(sprite.name, out _)) continue; // 번호가 아닌 그림(콘티 등)은 제외
            candidates.Add(sprite);
        }
        return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        return rect;
    }

    private static Image NewImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
