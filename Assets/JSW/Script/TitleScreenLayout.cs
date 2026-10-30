using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 타이틀 화면 연출.
// - 배경: Resources/Images/Opening 의 컷신 중 한 장을 랜덤으로 살짝 확대해 보여준다.
//         왼쪽 위에서 시작해 오른쪽 아래로 흘러가고, 그 뒤로도 사진의 네 모서리를 천천히 돌아다닌다.
// - 사진 위에 어두운 막과 비네팅을 깔아 글자를 읽기 쉽게 한다.
// - 로고(title.png)를 가운데 위로 옮기고 뒤에 은은한 빛을 둔다.
// - 메뉴를 가운데에 세로로 정렬하고, 이미지에 박혀 있던 글자 대신 명조 텍스트를 쓴다.
//   마우스를 올렸을 때의 하이라이트 바(choice_button)는 기존 그대로다.
public sealed class TitleScreenLayout : MonoBehaviour
{
    private const string OpeningPath = "Images/Opening";
    // 오프닝 컷신 중 타이틀 배경에서 뺄 그림 (11: 종루 컷, 숫자가 아닌 이름: 콘티 스크린샷 등).
    private static readonly HashSet<string> ExcludedBackgrounds = new HashSet<string> { "11" };

    [Header("배경 흐름")]
    [SerializeField, Range(1f, 2f)] private float zoomStart = 1.15f;  // 첫 구간 시작 배율
    [SerializeField, Range(1f, 2f)] private float zoomEnd = 1.15f;    // 첫 구간 끝 배율 (이후 계속 유지)
    [SerializeField, Min(1f)] private float panDuration = 30f;       // 대각선 한 번 이동하는 시간
    [SerializeField, Min(0f)] private float fadeInDuration = 1.2f;

    [Header("어둡게")]
    [SerializeField, Range(0f, 1f)] private float dim = 0.65f;
    [SerializeField, Range(0f, 1f)] private float vignette = 0.6f;

    [Header("로고")]
    [SerializeField] private Vector2 logoCenter = new Vector2(0f, 270f);
    [SerializeField, Min(10f)] private float logoWidth = 620f;
    [SerializeField, Range(0f, 1f)] private float glowAlpha = 0.15f;

    [Header("메뉴")]
    [SerializeField] private float menuTop = -40f;
    [SerializeField] private float menuSpacing = 80f;
    [SerializeField] private Vector2 highlightSize = new Vector2(316f, 81f);
    [SerializeField] private float fontSize = 40f;

    private static readonly (string method, string label)[] MenuItems =
    {
        ("NewStartButton", "처음부터"),
        ("ContinueButton", "이어하기"),
        ("SettingButton", "환경설정"),
        ("ExitButton", "나가기"),
    };

    private RectTransform photo;
    private Image photoImage;
    private RectTransform photoParent;
    private float aspect = 16f / 9f;
    private float elapsed;
    // 사진이 화면에 맞추는 모서리 순서 (x: -1 왼쪽, +1 오른쪽 / y: +1 위, -1 아래).
    // 왼쪽 위 → 오른쪽 아래 → 오른쪽 위 → 왼쪽 아래 → 왼쪽 위 …를 반복하며 사진 전체를 돌아다닌다.
    private static readonly Vector2[] Corners =
    {
        new Vector2(-1f, 1f), new Vector2(1f, -1f), new Vector2(1f, 1f), new Vector2(-1f, -1f),
    };
    private int leg;          // 지금 Corners[leg] → Corners[leg + 1] 로 이동 중
    private float legElapsed;
    private bool firstLeg = true; // 처음 왼쪽 위 → 오른쪽 아래 구간에서만 확대 배율이 바뀐다
    private RectTransform glow;

    public static void Setup(GameObject host)
    {
        if (host == null || host.GetComponent<TitleScreenLayout>() != null) return;
        host.AddComponent<TitleScreenLayout>().Build();
    }

    private void Build()
    {
        var buttons = FindMenuButtons();
        if (!buttons.TryGetValue("NewStartButton", out var newStart))
        {
            Debug.LogWarning("[TitleScreenLayout] 처음부터 버튼을 찾지 못해 타이틀 연출을 건너뜁니다.", this);
            return;
        }
        var menuPanel = (RectTransform)newStart.transform.parent;
        var uiPanel = menuPanel.parent as RectTransform;

        BuildBackground(uiPanel);
        BuildLogo(menuPanel);
        BuildMenu(menuPanel, buttons);
    }

    // ---------- 배경 ----------

    private void BuildBackground(RectTransform uiPanel)
    {
        var background = uiPanel != null ? uiPanel.Find("Background") as RectTransform : null;
        if (background == null) return;

        var sprite = PickBackground();
        if (sprite == null) return;
        aspect = sprite.rect.width / sprite.rect.height;

        // 기존 검은 배경 위에 사진, 그 위에 어두운 막을 올린다. 배경 바깥으로 넘치는 부분은 잘라낸다.
        photoParent = background;
        if (background.GetComponent<RectMask2D>() == null) background.gameObject.AddComponent<RectMask2D>();

        photo = NewUiObject("Title Photo", background);
        photo.anchorMin = photo.anchorMax = photo.pivot = new Vector2(0.5f, 0.5f);
        photoImage = photo.gameObject.AddComponent<Image>();
        photoImage.sprite = sprite;
        photoImage.raycastTarget = false;
        photoImage.color = new Color(1f, 1f, 1f, fadeInDuration > 0f ? 0f : 1f);

        var shade = NewUiObject("Title Shade", background);
        Stretch(shade);
        var shadeImage = shade.gameObject.AddComponent<RawImage>();
        shadeImage.texture = CreateShadeTexture(dim, vignette);
        shadeImage.raycastTarget = false;

        UpdatePhoto();
    }

    private static Sprite PickBackground()
    {
        var candidates = new List<Sprite>();
        foreach (var sprite in Resources.LoadAll<Sprite>(OpeningPath))
        {
            if (sprite == null || ExcludedBackgrounds.Contains(sprite.name)) continue;
            if (!int.TryParse(sprite.name, out _)) continue; // 콘티 스크린샷처럼 번호가 아닌 그림은 제외
            candidates.Add(sprite);
        }
        if (candidates.Count == 0)
        {
            Debug.LogWarning($"[TitleScreenLayout] Resources/{OpeningPath} 에서 배경 그림을 찾지 못했습니다.");
            return null;
        }
        return candidates[Random.Range(0, candidates.Count)];
    }

    private void UpdatePhoto()
    {
        if (photo == null || photoParent == null) return;

        // 화면을 빈틈없이 덮는 크기(비율 유지)에 확대 배율을 곱한다.
        var area = photoParent.rect.size;
        if (area.x <= 0f || area.y <= 0f) return;
        var cover = area.x / area.y > aspect
            ? new Vector2(area.x, area.x / aspect)
            : new Vector2(area.y * aspect, area.y);
        // 첫 구간(왼쪽 위 → 오른쪽 아래)만 확대 배율이 바뀌고, 그 뒤로는 끝 배율을 유지한다.
        float duration = LegDuration(leg);
        float t = duration > 0f ? Mathf.Clamp01(legElapsed / duration) : 1f;
        float eased = 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI); // 부드럽게 출발하고 멈춘다
        float zoom = firstLeg ? Mathf.Lerp(zoomStart, zoomEnd, eased) : zoomEnd;
        var size = cover * zoom;
        photo.sizeDelta = size;

        // 남는 만큼 안에서 움직인다: 모서리 (x, y) 가 화면 같은 쪽 모서리에 맞도록 사진을 반대로 민다.
        var room = (size - area) * 0.5f;
        var from = Corners[leg % Corners.Length];
        var to = Corners[(leg + 1) % Corners.Length];
        var corner = Vector2.Lerp(from, to, eased);
        photo.anchoredPosition = new Vector2(-corner.x * room.x, -corner.y * room.y);
    }

    // 대각선 구간은 panDuration, 세로 구간은 거리에 비례해 짧게 (같은 속도로 보이게).
    private float LegDuration(int index)
    {
        var from = Corners[index % Corners.Length];
        var to = Corners[(index + 1) % Corners.Length];
        float diagonal = new Vector2(2f * aspect, 2f).magnitude;
        float distance = new Vector2((to.x - from.x) * aspect, to.y - from.y).magnitude;
        return panDuration * Mathf.Max(0.35f, distance / diagonal);
    }

    // 가운데는 살짝, 가장자리는 진하게 어두워지는 막.
    private static Texture2D CreateShadeTexture(float dim, float vignette)
    {
        const int w = 160, h = 90;
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            name = "Title Shade",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };
        var pixels = new Color32[w * h];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float dx = (x + 0.5f) / w * 2f - 1f;
            float dy = (y + 0.5f) / h * 2f - 1f;
            float r = Mathf.Sqrt(dx * dx * 0.85f + dy * dy);
            float edge = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1.35f, r));
            float a = 1f - (1f - dim) * (1f - vignette * edge);
            // 리니어 색 공간에서는 같은 알파라도 눈에 덜 어둡게 보이므로, 감마 기준 어둡기에 맞춘다.
            if (QualitySettings.activeColorSpace == ColorSpace.Linear) a = 1f - Mathf.Pow(1f - a, 2.2f);
            pixels[y * w + x] = new Color32(0, 0, 0, (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f));
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return texture;
    }

    // ---------- 로고 ----------

    private void BuildLogo(RectTransform menuPanel)
    {
        var title = menuPanel.Find("Title0")?.GetComponent<Image>();
        if (title == null || title.sprite == null) return;

        // title.png 는 화면 전체 크기 그림이라 로고 부분만 잘라 쓴다.
        var source = title.sprite;
        var crop = FindOpaqueBounds(source);
        var logo = Sprite.Create(source.texture, crop, new Vector2(0.5f, 0.5f), source.pixelsPerUnit, 0, SpriteMeshType.FullRect);
        logo.name = "Title Logo";

        var rect = (RectTransform)title.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(logoWidth, logoWidth * crop.height / crop.width);
        rect.anchoredPosition = logoCenter;
        title.sprite = logo;
        title.preserveAspect = true;
        title.raycastTarget = false;

        // 로고 뒤 은은한 빛.
        glow = NewUiObject("Title Glow", menuPanel);
        glow.SetSiblingIndex(title.transform.GetSiblingIndex());
        glow.anchorMin = glow.anchorMax = glow.pivot = new Vector2(0.5f, 0.5f);
        glow.anchoredPosition = logoCenter;
        glow.sizeDelta = new Vector2(logoWidth * 1.9f, logoWidth * 1.25f);
        var glowImage = glow.gameObject.AddComponent<RawImage>();
        glowImage.texture = CreateGlowTexture();
        glowImage.color = new Color(1f, 1f, 1f, glowAlpha);
        glowImage.raycastTarget = false;
    }

    // 원본 그림이 읽기 불가 설정이어도 쓸 수 있도록, title.png 의 로고 영역을 고정값으로 둔다.
    // (title.png 1920x1080 기준, 로고 불투명 영역 97,78 ~ 715,417 / 위에서부터의 좌표)
    private static Rect FindOpaqueBounds(Sprite source)
    {
        var tex = source.texture;
        float sx = tex.width / 1920f, sy = tex.height / 1080f;
        const float left = 97f, top = 78f, right = 715f, bottom = 417f, pad = 6f;
        var rect = new Rect((left - pad) * sx, (1080f - bottom - pad) * sy, (right - left + pad * 2f) * sx, (bottom - top + pad * 2f) * sy);
        rect.xMin = Mathf.Max(0f, rect.xMin);
        rect.yMin = Mathf.Max(0f, rect.yMin);
        rect.xMax = Mathf.Min(tex.width, rect.xMax);
        rect.yMax = Mathf.Min(tex.height, rect.yMax);
        return rect;
    }

    private static Texture2D CreateGlowTexture()
    {
        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Title Glow",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = (x + 0.5f) / size * 2f - 1f;
            float dy = (y + 0.5f) / size * 2f - 1f;
            float r = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
            float a = Mathf.Pow(1f - r, 2.2f);
            pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return texture;
    }

    // ---------- 메뉴 ----------

    private void BuildMenu(RectTransform menuPanel, Dictionary<string, Button> buttons)
    {
        // 글자가 박혀 있던 이미지는 텍스트로 대체한다.
        var bakedText = menuPanel.Find("Text");
        if (bakedText != null && bakedText.GetComponent<Button>() == null) bakedText.gameObject.SetActive(false);

        var font = LoadFont(menuPanel);
        int row = 0;
        foreach (var (method, text) in MenuItems)
        {
            if (!buttons.TryGetValue(method, out var button)) continue;
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = highlightSize;
            rect.anchoredPosition = new Vector2(0f, menuTop - menuSpacing * row);
            row++;

            var labelRect = NewUiObject("Label", rect);
            Stretch(labelRect);
            var label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) label.font = font;
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.95f, 0.94f, 0.91f);
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
        }
    }

    private static TMP_FontAsset LoadFont(Transform near)
    {
        var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/BookkMyungjo_Light SDF");
        if (font != null) return font;
        // 못 찾으면 같은 씬에서 이미 쓰고 있는 글꼴을 빌린다 (설정 창 텍스트 등).
        var root = near.root;
        var existing = root.GetComponentInChildren<TMP_Text>(true);
        return existing != null ? existing.font : null;
    }

    private static Dictionary<string, Button> FindMenuButtons()
    {
        var result = new Dictionary<string, Button>();
        foreach (var button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var onClick = button.onClick;
            for (int i = 0; i < onClick.GetPersistentEventCount(); i++)
            {
                var method = onClick.GetPersistentMethodName(i);
                foreach (var (name, _) in MenuItems)
                    if (method == name && !result.ContainsKey(name)) result.Add(name, button);
            }
        }
        // 설정 창의 닫기 버튼도 이름이 "Exit Button"이라 메서드 이름으로만 구분한다 (SettingExitButton ≠ ExitButton).
        return result;
    }

    // ---------- 공통 ----------

    private static RectTransform NewUiObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        go.layer = parent.gameObject.layer;
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

    private void Update()
    {
        if (photo == null) return;
        float dt = Time.unscaledDeltaTime;
        elapsed += dt;
        legElapsed += dt;
        if (legElapsed >= LegDuration(leg))
        {
            legElapsed -= LegDuration(leg);
            leg = (leg + 1) % Corners.Length;
            firstLeg = false;
        }
        UpdatePhoto();

        if (photoImage != null && photoImage.color.a < 1f)
        {
            var c = photoImage.color;
            c.a = fadeInDuration > 0f ? Mathf.Clamp01(elapsed / fadeInDuration) : 1f;
            photoImage.color = c;
        }
    }
}
